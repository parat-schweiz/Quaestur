using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Globalization;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class ApiContext
    {
        public IDatabase Database { get; private set; }
        public ApiClient Client { get; private set; }

        public ApiContext(IDatabase database, ApiClient client)
        {
            Database = database;
            Client = client;
        }

        private bool RightCovered(AccessRight requestedRight, AccessRight actualRight)
        {
            switch (requestedRight)
            {
                case AccessRight.Read:
                    switch (actualRight)
                    {
                        case AccessRight.Write:
                        case AccessRight.Read:
                            return true;
                        default:
                            return false;
                    }
                case AccessRight.Write:
                    switch (actualRight)
                    {
                        case AccessRight.Write:
                            return true;
                        case AccessRight.Read:
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        private bool GroupCovered(Group group, ApiPermission permission)
        {
            switch (permission.Subject.Value)
            {
                case SubjectAccess.SystemWide:
                case SubjectAccess.SubOrganization:
                case SubjectAccess.Organization:
                    return OrganizationCovered(group.Organization.Value, permission);
                case SubjectAccess.Group:
                    return Client.Group.Value == group;
                default:
                    return false;
            }
        }

        private bool OrganizationCovered(Organization organization, ApiPermission permission)
        {
            switch (permission.Subject.Value)
            {
                case SubjectAccess.SystemWide:
                    return true;
                case SubjectAccess.SubOrganization:
                    return (Client.Group.Value.Organization.Value == organization) ||
                        Client.Group.Value.Organization.Value.Subordinates.Contains(organization);
                case SubjectAccess.Organization:
                    return Client.Group.Value.Organization.Value == organization;
                default:
                    return false;
            }
        }

        private bool PersonCovered(Person person, ApiPermission permission)
        {
            foreach (var organization in person.Memberships
                .Select(m => m.Organization.Value)
                .Distinct())
            {
                if (OrganizationCovered(organization, permission))
                {
                    return true;
                }
            }

            foreach (var group in person.RoleAssignments
                .Select(a => a.Role.Value.Group.Value)
                .Distinct())
            {
                if (GroupCovered(group, permission))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasApiAccess(Organization organization, PartAccess part, AccessRight right)
        {
            foreach (var permission in Client.Permissions)
            {
                if (permission.Part.Value == part &&
                    RightCovered(right, permission.Right.Value) &&
                    OrganizationCovered(organization, permission))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasApiAccess(Group group, PartAccess part, AccessRight right)
        {
            foreach (var permission in Client.Permissions)
            {
                if (permission.Part.Value == part &&
                    RightCovered(right, permission.Right.Value) &&
                    GroupCovered(group, permission))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasApiAccess(Person person, PartAccess part, AccessRight right)
        {
            foreach (var permission in Client.Permissions)
            {
                if (permission.Part.Value == part &&
                    RightCovered(right, permission.Right.Value) &&
                    PersonCovered(person, permission))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class ApiResponse
    {
        private readonly IDatabase _database;
        private readonly JObject _response;

        public ApiContext Context { get; private set; }

        public ApiResponse(IDatabase database)
        {
            _database = database;
            _response = new JObject(); 
        }

        public void SetList<T>(IEnumerable<T> list, ApiContext context)
            where T : DatabaseObject
        {
            var array = new JArray();

            foreach (T obj in list)
            {
                array.Add(ConvertObject(obj, context)); 
            }

            _response.Add(new JProperty("result", array));
            SetSuccess();
        }

        public void SetObject<T>(T obj, ApiContext context)
            where T : DatabaseObject
        {
            _response.Add(new JProperty("result", ConvertObject(obj, context)));
            SetSuccess();
        }

        private JProperty Property(string name, GuidIdPrimaryKeyField field)
        {
            return new JProperty(name, field.Value.ToString());
        }

        private JProperty Property<T>(string name, EnumField<T> field)
             where T : struct, IConvertible
        {
            return new JProperty(name, field.Value.ToString());
        }

        private JProperty Property(string name, FieldDateTime field)
        {
            return new JProperty(name, field.Value.FormatIso());
        }

        private JProperty Property(string name, FieldDate field)
        {
            return new JProperty(name, field.Value.FormatIso());
        }

        private JProperty Property(string name, Field<Guid> field)
        {
            return new JProperty(name, field.Value.ToString());
        }

        private JProperty Property(string name, DecimalField field)
        {
            return new JProperty(name, field.Value.ToString());
        }

        private JProperty Property(string name, Field<long> field)
        {
            return new JProperty(name, field.Value.ToString());
        }

        private JProperty Property(string name, Field<int> field)
        {
            return new JProperty(name, field.Value.ToString());
        }

        private JProperty Property(string name, StringField field)
        {
            return new JProperty(name, field.Value);
        }

        private JProperty Property(string name, string value)
        {
            return new JProperty(name, value);
        }

        private JProperty Property(string name, MultiLanguageStringField field)
        {
            var texts = new JObject();
            texts.Add(new JProperty(Language.English.ToString().ToLowerInvariant(), field.Value[Language.English]));
            texts.Add(new JProperty(Language.German.ToString().ToLowerInvariant(), field.Value[Language.German]));
            texts.Add(new JProperty(Language.French.ToString().ToLowerInvariant(), field.Value[Language.French]));
            texts.Add(new JProperty(Language.Italian.ToString().ToLowerInvariant(), field.Value[Language.Italian]));
            return new JProperty(name, texts);
        }

        private JProperty Property(string name, string separator, params MultiLanguageStringField[] fields)
        {
            var texts = new JObject();
            texts.Add(new JProperty(Language.English.ToString().ToLowerInvariant(), Join(Language.English, separator, fields)));
            texts.Add(new JProperty(Language.German.ToString().ToLowerInvariant(), Join(Language.German, separator, fields)));
            texts.Add(new JProperty(Language.French.ToString().ToLowerInvariant(), Join(Language.French, separator, fields)));
            texts.Add(new JProperty(Language.Italian.ToString().ToLowerInvariant(), Join(Language.Italian, separator, fields)));
            return new JProperty(name, texts);
        }

        private string Join(Language language, string separator, params MultiLanguageStringField[] fields)
        {
            return string.Join(separator, fields.Select(f => f.Value[language]));
        }

        private JObject ConvertObject<T>(T obj, ApiContext context)
            where T : DatabaseObject
        {
            DatabaseObject dbObj = obj;
            var json = new JObject();
            json.Add(Property("id", dbObj.Id));
            json.Add(new JProperty("type", typeof(T).Name));

            if (dbObj is Person person)
            {
                json.Add(Property("username", person.UserName));
                json.Add(Property("language", person.Language));

                if (context.HasApiAccess(person, PartAccess.Demography, AccessRight.Read))
                {
                    json.Add(Property("lastname", person.LastName));
                    json.Add(Property("firstname", person.FirstName));
                    json.Add(Property("middlenames", person.MiddleNames));
                    json.Add(Property("fullname", person.FullName));
                    json.Add(Property("shorthand", person.ShortHand));
                    json.Add(Property("birthdate", person.BirthDate));
                }
            }
            else if (dbObj is Organization organization)
            {
                json.Add(Property("name", organization.Name));
            }
            else if (dbObj is Group group)
            {
                json.Add(Property("name", group.Name));
            }
            else if (dbObj is Points points)
            {
                json.Add(Property("ownerid", points.Owner.Value.Id));
                json.Add(Property("budgetid", points.Budget.Value.Id));
                json.Add(Property("amount", points.Amount));
                json.Add(Property("reason", points.Reason));
                json.Add(Property("url", points.Url));
                json.Add(Property("referencetype", points.ReferenceType));
                json.Add(Property("referenceid", points.ReferenceId));
            }
            else if (dbObj is PointBudget budget)
            {
                json.Add(Property("label", budget.Label));
                json.Add(Property("periodid", budget.Period.Value.Id));
                json.Add(Property("ownerid", budget.Owner.Value.Id));
                json.Add(Property("currentpoints", budget.CurrentPoints));
                json.Add(Property("share", budget.Share));
                json.Add(Property("fulllabel", "/", budget.Owner.Value.Organization.Value.Name, budget.Owner.Value.Name, budget.Label));
            }
            else if (dbObj is Credits credits)
            {
                json.Add(Property("ownerid", credits.Owner.Value.Id));
                json.Add(Property("amount", credits.Amount));
                json.Add(Property("reason", credits.Reason));
                json.Add(Property("url", credits.Url));
                json.Add(Property("referencetype", credits.ReferenceType));
                json.Add(Property("referenceid", credits.ReferenceId));
            }
            else if (dbObj is BudgetPeriod period)
            {
                json.Add(Property("startdate", period.StartDate));
                json.Add(Property("enddate", period.EndDate));
                json.Add(Property("organizationid", period.Organization.Value.Id));
            }

            return json;
        }

        public void SetErrorAuthenticationFailed()
        {
            _response.Add(new JProperty("status", "failure"));
            _response.Add(new JProperty("error", "Authentication failed"));
        }

        public void SetErrorAccessDenied()
        {
            _response.Add(new JProperty("status", "failure"));
            _response.Add(new JProperty("error", "Access denied"));
        }

        public bool HasAccess(Person person, PartAccess part, AccessRight right)
        { 
            if (Context.HasApiAccess(person, part, right))
            {
                return true; 
            }
            else
            {
                SetErrorAccessDenied();
                return false; 
            }
        }

        public bool HasAccess(Group group, PartAccess part, AccessRight right)
        {
            if (Context.HasApiAccess(group, part, right))
            {
                return true;
            }
            else
            {
                SetErrorAccessDenied();
                return false;
            }
        }

        public bool HasAccess(Organization organization, PartAccess part, AccessRight right)
        {
            if (Context.HasApiAccess(organization, part, right))
            {
                return true;
            }
            else
            {
                SetErrorAccessDenied();
                return false;
            }
        }

        public bool CheckLogin(Request request)
        {
            Context = FindLogin(request);

            if (Context == null)
            {
                SetErrorAuthenticationFailed();
                return false; 
            }
            else
            {
                return true;
            }
        }

        private ApiContext FindLogin(Request request)
        {
            string authorization = request.Headers.Authorization;

            if (!string.IsNullOrEmpty(authorization) &&
                authorization.StartsWith("QAPI2 ", StringComparison.Ordinal))
            {
                var parts = authorization.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    var client = _database.Query<ApiClient>(parts[1]);
                    if (client != null)
                    {
                        if (Global.Security.VerifyPassword(client.SecureSecret.Value, parts[2]))
                        {
                            Info("Successful API authentication for {0}", client.Name.Value[Language.English]);
                            return new ApiContext(_database, client);
                        }
                        else
                        {
                            Notice("Failed API authentication for {0}", client.Name.Value[Language.English]);
                        }
                    }
                }
            }

            return null;
        }

        public void Notice(string text, params object[] parameters)
        {
            Global.Log.Notice(text, parameters);
        }

        public void Info(string text, params object[] parameters)
        {
            Global.Log.Info(text, parameters);
        }

        public void Warning(string text, params object[] parameters)
        {
            Global.Log.Warning(text, parameters);
        }

        public bool TryParameterObjectId<T>(string idString, out T obj)
            where T : DatabaseObject, new()
        {
            if (Guid.TryParse(idString, out Guid id))
            {
                obj = _database.Query<T>(id);
                return obj != null;
            }
            else
            {
                obj = null;
                return false;
            }
        }

        public bool TryParseJson(string text, out JObject obj)
        {
            try
            {
                obj = JObject.Parse(text);
                return true;
            }
            catch
            {
                SetErrorMalformedJson();
                obj = null;
                return false;
            }
        }

        public bool TryValueString(JObject request, string key, out string value)
        {
            if (request.TryValueString(key, out value))
            {
                return true;
            }
            else
            {
                SetErrorMissingOrMalformed(key);
                return false; 
            }
        }

        public bool TryValueInt32(JObject request, string key, out int value)
        {
            if (request.TryValueInt32(key, out value))
            {
                return true;
            }
            else
            {
                SetErrorMissingOrMalformed(key);
                return false;
            }
        }

        public bool TryValueDecimal(JObject request, string key, out decimal value)
        {
            if (request.TryValueDecimal(key, out value))
            {
                return true;
            }
            else
            {
                SetErrorMissingOrMalformed(key);
                return false;
            }
        }

        public bool TryValueGuid(JObject request, string key, out Guid value)
        {
            if (request.TryValueGuid(key, out value))
            {
                return true;
            }
            else
            {
                SetErrorMissingOrMalformed(key);
                return false;
            }
        }

        public bool TryValueDateTime(JObject request, string key, out DateTime value)
        { 
            if (request.TryValueDateTime(key, out value))
            {
                return true;
            }
            else
            {
                value = new DateTime(1850, 1, 1);
                SetErrorMissingOrMalformed(key);
                return false;
            }
        }

        public bool TryValueEnum<T>(JObject request, string key, out T value)
            where T : struct
        {
            if (request.TryValueString(key, out string stringValue) &&
                Enum.TryParse(stringValue, out value))
            {
                return true;
            }
            else
            {
                value = default(T);
                SetErrorMissingOrMalformed(key);
                return false;
            }
        }

        public bool TryReadObjectField<T>(JObject request, string key, out T obj)
            where T : DatabaseObject, new()
        {
            if (request.TryValueGuid(key, out Guid id))
            {
                obj = _database.Query<T>(id);

                if (obj == null)
                {
                    SetErrorNotFound(typeof(T).Name, id);
                    return false; 
                }
                else
                {
                    return true; 
                }
            }
            else
            {
                obj = null;
                SetErrorMissingOrMalformed(key);
                return false;
            }
        }

        public void SetErrorNotFound(string type, Guid id)
        {
            _response.Add(new JProperty("status", "failure"));
            _response.Add(new JProperty("error", string.Format("Object {1} of type {0} not found", type, id)));
        }

        public void SetErrorMissingOrMalformed(string key)
        {
            _response.Add(new JProperty("status", "failure"));
            _response.Add(new JProperty("error", string.Format("Field {0} missing or malformatted", key)));
        }

        public void SetErrorMalformedJson()
        {
            _response.Add(new JProperty("status", "failure"));
            _response.Add(new JProperty("error", "Malformed JSON object"));
        }

        public void SetError(string error)
        {
            _response.Add(new JProperty("status", "failure"));
            _response.Add(new JProperty("error", error));
        }

        public void SetSuccess()
        {
            _response.Add(new JProperty("status", "success"));
        }

        public void SetSuccess(string key, object value)
        {
            _response.Add(new JProperty("status", "success"));
            _response.Add(new JProperty(key, value));
        }

        public string ToJson()
        {
            return _response.ToString();
        }
    }

    public class ApiModule : QuaesturModule
    {
        public ApiModule()
        {
            Get("/api/v2/organization/list", parameters =>
            {
                var response = CreateResponse();

                if (response.CheckLogin(Request))
                {
                    var organization = Database
                        .Query<Organization>()
                        .Where(o => response.Context.HasApiAccess(o, PartAccess.Structure, AccessRight.Read))
                        .OrderBy(o => o.GetText(Translator));
                    response.SetList(organization, response.Context);
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Get("/api/v2/group/list", parameters =>
            {
                var response = CreateResponse();

                if (response.CheckLogin(Request))
                {
                    var organization = Database
                        .Query<Group>()
                        .Where(g => response.Context.HasApiAccess(g, PartAccess.Structure, AccessRight.Read))
                        .OrderBy(g => g.Name.GetText(Translator));
                    response.SetList(organization, response.Context);
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Get("/api/v2/person/list", parameters =>
            {
                var response = CreateResponse();

                if (response.CheckLogin(Request))
                {
                    var persons = Database
                        .Query<Person>()
                        .Where(p => response.Context.HasApiAccess(p, PartAccess.Anonymous, AccessRight.Read))
                        .OrderBy(p => p.UserName.Value);
                    response.SetList(persons, response.Context);
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Get("/api/v2/pointbudget/list", parameters =>
            {
                var response = CreateResponse();

                if (response.CheckLogin(Request))
                {
                    var budgets = Database
                        .Query<PointBudget>()
                        .Where(b => b.Period.Value.StartDate.Value.Date <= DateTime.UtcNow.Date)
                        .Where(b => b.Period.Value.EndDate.Value.Date >= DateTime.UtcNow.Date)
                        .Where(b => response.Context.HasApiAccess(b.Owner.Value, PartAccess.PointBudget, AccessRight.Read))
                        .OrderBy(b => b.Label.Value.AnyValue);
                    response.SetList(budgets, response.Context);
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Get("/api/v2/budgetperiods/list", parameters =>
            {
                var response = CreateResponse();

                if (response.CheckLogin(Request))
                {
                    var periods = Database
                        .Query<BudgetPeriod>()
                        .Where(b => response.Context.HasApiAccess(b.Organization.Value, PartAccess.PointBudget, AccessRight.Read))
                        .OrderBy(b => b.ToString());
                    response.SetList(periods, response.Context);
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Post("/api/v2/points/add", parameters =>
            {
                var response = CreateResponse();

                if (response.CheckLogin(Request) &&
                    response.TryParseJson(ReadBody(), out JObject request) &&
                    response.TryReadObjectField(request, "ownerid", out Person person) &&
                    response.TryReadObjectField(request, "budgetid", out PointBudget budget) &&
                    response.TryValueInt32(request, "amount", out int amount) &&
                    response.TryValueString(request, "reason", out string reason) &&
                    response.TryValueString(request, "url", out string url) &&
                    response.TryValueDateTime(request, "moment", out DateTime moment) &&
                    response.TryValueEnum(request, "referencetype", out InteractionReferenceType referenceType) &&
                    response.TryValueGuid(request, "referenceid", out Guid referenceId) &&
                    response.HasAccess(person, PartAccess.Points, AccessRight.Write) &&
                    response.HasAccess(budget.Owner.Value.Organization.Value, PartAccess.Points, AccessRight.Write))
                {
                    var points = new Points(Guid.NewGuid());
                    points.Owner.Value = person;
                    points.Budget.Value = budget;
                    points.Amount.Value = amount;
                    points.Moment.Value = moment;
                    points.Reason.Value = reason;
                    points.Url.Value = url;
                    points.ReferenceType.Value = referenceType;
                    points.ReferenceId.Value = referenceId;

                    var credits = new Credits(Guid.NewGuid());
                    credits.Owner.Value = person;
                    credits.Amount.Value = amount;
                    credits.Moment.Value = moment;
                    credits.Reason.Value = reason;
                    credits.Url.Value = url;
                    credits.ReferenceType.Value = referenceType;
                    credits.ReferenceId.Value = referenceId;

                    var success = true;

                    if (request.TryValueGuid("impersonateid", out Guid id))
                    {
                        var impersonate = Database.Query<Person>(id);

                        if (impersonate == null)
                        {
                            response.SetErrorAccessDenied();
                            success = false;
                        }
                        else if (!HasAccess(impersonate, person, PartAccess.Points, AccessRight.Write) ||
                                 !HasAccess(impersonate, budget.Owner.Value.Organization.Value, PartAccess.Points, AccessRight.Write))
                        {
                            response.SetErrorAccessDenied();
                            success = false;
                        }
                    }

                    if (success)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            Database.Save(points);
                            Database.Save(credits);
                            Journal("API",
                                    person,
                                    "Journal.API.Points.Add",
                                    "Added points through the API",
                                    "Added points {0}.",
                                    t => points.GetText(t));
                            transaction.Commit();
                        }

                        response.SetObject(points, response.Context);
                    }
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Post("/api/v2/payment/prepare", parameters =>
            {
                PaymentTransactions.Instance.Expire();
                var response = CreateResponse();

                if (response.CheckLogin(Request) &&
                    response.TryParseJson(ReadBody(), out JObject request) &&
                    response.TryValueDecimal(request, "amount", out decimal amount) &&
                    response.TryValueString(request, "reason", out string reason) &&
                    response.TryValueString(request, "url", out string url) &&
                    response.TryValueString(request, "payreturnurl", out string payReturnUrl) &&
                    response.TryValueString(request, "cancelreturnurl", out string cancelReturnUrl))
                {
                    var transaction = new PaymentTransaction(
                        response.Context.Client.Id.Value,
                        amount,
                        reason,
                        url,
                        payReturnUrl,
                        cancelReturnUrl);
                    PaymentTransactions.Instance.Add(transaction);
                    response.SetSuccess("id", transaction.Id);
                    Global.Log.Notice(
                        "Payment transaction id {0} prepared for store {1}.",
                        transaction.Id,
                        response.Context.Client.Name.Value[Language.English]);
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Post("/api/v2/payment/commit", parameters =>
            {
                PaymentTransactions.Instance.Expire();
                var response = CreateResponse();

                if (response.CheckLogin(Request) &&
                    response.TryParseJson(ReadBody(), out JObject request) &&
                    response.TryValueGuid(request, "id", out Guid id))
                {
                    var transaction = PaymentTransactions.Instance.Get(id);

                    if ((transaction != null) &&
                        (transaction.State == PaymentTransactionState.Authorized))
                    {
                        var person = Database.Query<Person>(transaction.BuyerId);

                        if (person != null)
                        {
                            if (response.HasAccess(person, PartAccess.Credits, AccessRight.Write))
                            {
                                using (var dbTransaction = Database.BeginTransaction())
                                {
                                    var creditsBalance = Database
                                    .Query<Credits>(DC.Equal("ownerid", person.Id.Value))
                                    .Sum(c => c.Amount.Value);
                                    var settings = Database
                                        .Query<SystemWideSettings>()
                                        .Single();
                                    var split = new PaymentTransactionSplit(settings, transaction, creditsBalance);
                                    var translator = new Translator(Translation, person.Language.Value);
                                    var reason = translator.Get(
                                        "API.Payment.Reason",
                                        "Reason used by API for store purchase.",
                                        "Payment for {0}: {1}",
                                        response.Context.Client.Name.Value[translator.Language],
                                        transaction.Reason);

                                    var credits = new Credits(transaction.Id);
                                    credits.Owner.Value = person;
                                    credits.Amount.Value = (-split.PayableCredits);
                                    credits.Reason.Value = reason;
                                    credits.Url.Value = transaction.Url;
                                    credits.Moment.Value = transaction.Moment;
                                    credits.ReferenceType.Value = InteractionReferenceType.StorePayment;
                                    credits.ReferenceId.Value = transaction.ShopId;
                                    Database.Save(credits);

                                    if (split.PayableCurrency > 0M)
                                    {
                                        var prepayment = new Prepayment(Guid.NewGuid());
                                        prepayment.Person.Value = person;
                                        prepayment.Amount.Value = (-split.PayableCurrency);
                                        prepayment.Reason.Value = reason;
                                        prepayment.Moment.Value = transaction.Moment;
                                        prepayment.Url.Value = transaction.Url;
                                        prepayment.Reference.Value = transaction.Id.ToString();
                                        prepayment.ReferenceType.Value = PrepaymentType.StorePayment;
                                        Database.Save(prepayment);
                                    }

                                    Journal("API",
                                            person,
                                            "Journal.API.Payment.Committed",
                                            "Payment committed through the API",
                                            "Payment committed for store {0}.",
                                            t => response.Context.Client.Name.Value[t.Language]);
                                    Global.Log.Notice(
                                        "Payment transaction committed for user {0} by store {1}.",
                                        person.GetText(new Translator(Translation, Language.English)),
                                        response.Context.Client.Name.Value[Language.English]);
                                    dbTransaction.Commit();
                                    response.SetSuccess();
                                    PaymentTransactions.Instance.Remove(transaction.Id);
                                }
                            }
                            else
                            {
                                Global.Log.Notice(
                                    "Credits payment transaction for user {0} by store {1} failed because access was denied.",
                                    person.GetText(new Translator(Translation, Language.English)),
                                    response.Context.Client.Name.Value[Language.English]);
                            }
                        }
                        else
                        {
                            Global.Log.Notice(
                                "Credits payment transaction by store {0} failed because the buyer was not found.",
                                response.Context.Client.Name.Value[Language.English]);
                            response.SetError("Buyer not found.");
                        }
                    }
                    else
                    {
                        Global.Log.Info(
                            "Credits payment transaction by store {0} failed because the transaction was not found.",
                            response.Context.Client.Name.Value[Language.English]);
                        response.SetError("Transaction not found.");
                    }
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
        }

        private ApiResponse CreateResponse()
        {
            return new ApiResponse(Database);
        }
    }
}

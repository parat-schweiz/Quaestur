using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
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
        private readonly JObject _response;

        public ApiResponse()
        {
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

        private JProperty Property(string name, Field<Guid> field)
        {
            return new JProperty(name, field.Value.ToString());
        }

        private JProperty Property(string name, StringField field)
        {
            return new JProperty(name, field.Value);
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

        private JObject ConvertObject<T>(T obj, ApiContext context)
            where T : DatabaseObject
        {
            DatabaseObject dbObj = obj;
            var json = new JObject();
            json.Add(Property("id", dbObj.Id));

            if (dbObj is Person person)
            {
                json.Add(Property("username", person.UserName));
                return json;
            }
            else if (dbObj is Organization organization)
            {
                json.Add(Property("name", organization.Name));
                return json;
            }
            else if (dbObj is Group group)
            {
                json.Add(Property("name", group.Name));
                return json;
            }
            else
            {
                throw new ArgumentException(); 
            }
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

        public void SetSuccess()
        {
            _response.Add(new JProperty("status", "success"));
        }

        public string ToJson()
        {
            return _response.ToString();
        }
    }

    public class ApiModule : QuaesturModule
    {
        private ApiContext _context;

        public ApiModule()
        {
            Get("/api/v2/organization/list", parameters =>
            {
                var response = new ApiResponse();

                if (CheckLogin())
                {
                    var organization = Database
                        .Query<Organization>()
                        .Where(o => _context.HasApiAccess(o, PartAccess.Structure, AccessRight.Read))
                        .OrderBy(o => o.GetText(Translator));
                    response.SetList(organization, _context);
                }
                else
                {
                    response.SetErrorAuthenticationFailed();
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Get("/api/v2/group/list", parameters =>
            {
                var response = new ApiResponse();

                if (CheckLogin())
                {
                    var organization = Database
                        .Query<Group>()
                        .Where(g => _context.HasApiAccess(g, PartAccess.Structure, AccessRight.Read))
                        .OrderBy(g => g.Name.GetText(Translator));
                    response.SetList(organization, _context);
                }
                else
                {
                    response.SetErrorAuthenticationFailed();
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
            Get("/api/v2/person/list", parameters =>
            {
                var response = new ApiResponse();

                if (CheckLogin())
                {
                    var persons = Database
                        .Query<Person>()
                        .Where(p => _context.HasApiAccess(p, PartAccess.Anonymous, AccessRight.Read))
                        .OrderBy(p => p.UserName.Value);
                    response.SetList(persons, _context);
                }
                else
                {
                    response.SetErrorAuthenticationFailed();
                }

                return Response.AsText(response.ToJson(), "application/json");
            });
        }

        private bool CheckLogin()
        {
            _context = FindLogin();
            return _context != null;
        }

        private ApiContext FindLogin()
        {
            string authorization = Request.Headers.Authorization;

            if (!string.IsNullOrEmpty(authorization) &&
                authorization.StartsWith("QAPI2 ", StringComparison.Ordinal))
            {
                var parts = authorization.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 3)
                {
                    var client = Database.Query<ApiClient>(parts[1]);
                    if (client != null)
                    {
                        if (Global.Security.VerifyPassword(client.SecureSecret.Value, parts[2]))
                        {
                            Notice("Successful API authentication for {0}", client.Name.Value[Language.English]);
                            return new ApiContext(Database, client);
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
    }
}

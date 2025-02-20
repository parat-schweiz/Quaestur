using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Nancy;
using Nancy.ModelBinding;
using SiteLibrary;
using BaseLibrary;
using MimeKit;
using System.Text;
using Nancy.Responses.Negotiation;
using Nancy.Helpers;
using System.Globalization;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;

namespace Quaestur
{
    public class SubscribeViewModel
    {
        public string Id;
        public string Title;
        public string Text;
        public string Email;
        public string Language;
        public string Request;
        public string PhraseFieldEmail;
        public string PhraseButtonSubscribe;
        public string PhraseThrottleWait;

        public SubscribeViewModel()
        { 
        }

        public SubscribeViewModel(Translator translator, IDatabase database, Subscription subscription, string request)
        {
            PhraseFieldEmail = translator.Get("Subscription.Subscribe.Field.Email", "Email field on the subscribe page", "E-Mail").EscapeHtml();
            PhraseButtonSubscribe = translator.Get("Subscription.Subscribe.Button.Subscribe", "Subscribe button on the subscribe page", "Subscribe").EscapeHtml();
            PhraseThrottleWait = translator.Get("Subscription.Subscribe.Throttle.Wait", "Info text on the subscribe page when waiting for the anti-spam throttle", "Please wait for the anti-spam check...").EscapeHtml();
            var page = subscription.SubscribePrePages.Value(database, translator.Language);
            Id = subscription.Id.Value.ToString();
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
            Language = translator.Language.Locale();
            Request = request;
        }
    }

    public class ConfirmViewModel
    {
        public string Title;
        public string Text;

        public ConfirmViewModel()
        {
        }

        public ConfirmViewModel(Translator translator, IDatabase database, Subscription subscription)
        {
            var page = subscription.ConfirmMailPages.Value(database, translator.Language);
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
        }
    }

    public class SubscribedViewModel
    {
        public string Id;
        public string Title;
        public string Text;

        public SubscribedViewModel()
        {
        }

        public SubscribedViewModel(Translator translator, IDatabase database, Subscription subscription)
        {
            var page = subscription.SubscribePostPages.Value(database, translator.Language);
            Id = subscription.Id.Value.ToString();
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
        }
    }

    public class JoinedViewModel
    {
        public string Id;
        public string Title;
        public string Text;

        public JoinedViewModel()
        {
        }

        public JoinedViewModel(Translator translator, IDatabase database, Subscription subscription, Person person)
        {
            var page = subscription.JoinPostPages.Value(database, translator.Language);
            Id = subscription.Id.Value.ToString();
            var templator = new Templator(new PersonContentProvider(database, translator, person));
            Text = templator.Apply(page.HtmlText.Value);
            Title = page.Subject.Value;
        }
    }

    public class UnsubscribeViewModel
    {
        public string PhraseButtonUnsubscribe;
        public string LinkUrl;
        public string Title;
        public string Text;

        public UnsubscribeViewModel()
        {
        }

        public UnsubscribeViewModel(Translator translator, IDatabase database, Subscription subscription, string linkUrl)
        {
            PhraseButtonUnsubscribe = translator.Get("Subscription.Unsubscribe.Button.Unsubscribe", "Unsubscribe button on the unsubscribe page", "Unsubscribe").EscapeHtml();
            var page = subscription.UnsubscribePrePages.Value(database, translator.Language);
            LinkUrl = linkUrl;
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
        }
    }

    public class UnsubscribedViewModel
    {
        public string Id;
        public string Title;
        public string Text;

        public UnsubscribedViewModel()
        {
        }

        public UnsubscribedViewModel(Translator translator, IDatabase database, Subscription subscription)
        {
            var page = subscription.UnsubscribePostPages.Value(database, translator.Language);
            Id = subscription.Id.Value.ToString();
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
        }
    }

    public class JoinForm : Form<Person>
    {
        private ServiceAddress GetMailAddress(Person person, bool create)
        {
            var address = person.PrimaryAddress(ServiceType.EMail);
            if ((address == null) && create)
            {
                address = new ServiceAddress(Guid.NewGuid());
                address.Person.Value = person;
                address.Precedence.Value = 0;
                address.Service.Value = ServiceType.EMail;
                address.Category.Value = AddressCategory.Home;
            }
            return address;
        }

        private PostalAddress GetPostalAddress(Person person, bool create)
        {
            var address = person.PrimaryPostalAddress;
            if ((address == null) && create)
            {
                address = new PostalAddress(Guid.NewGuid());
                address.Person.Value = person;
                address.Precedence.Value = 0;
            }
            return address;
        }

        public override void ClearUpdated()
        {
            throw new NotImplementedException();
        }

        public override void Save()
        {
            throw new NotImplementedException();
        }

        public JoinForm(QuaesturModule module, MailTemplate template, string saveUrl, Person person, string mailAddress)
            : base(module, "JoinForm", template.Subject, saveUrl, template.HtmlText.GetText(module.Translator))
        {
            if (person != null)
            {
                LoadValues(person);
            }
            else if (!string.IsNullOrEmpty(mailAddress))
            {
            }
        }

        public override string Template => "View/Form/nobling_form.sshtml";
    }

    public class JoinViewModel
    {
        public string PhraseFieldUserName;
        public string PhraseFieldFirstName;
        public string PhraseFieldMiddleNames;
        public string PhraseFieldLastName;
        public string PhraseFieldBirthDate;
        public string PhraseFieldStreet;
        public string PhraseFieldPlace;
        public string PhraseFieldPostalCode;
        public string PhraseFieldMailAddress;
        public string PhraseFieldCountry;
        public string PhraseFieldState;
        public string PhraseButtonJoin;
        public string LinkUrl;
        public string SubscriptionId;
        public string PersonId;
        public string Title;
        public string Text;
        public string UserName;
        public string UserNameFeedback;
        public string UserNameValid;
        public string FirstName;
        public string FirstNameFeedback;
        public string FirstNameValid;
        public string MiddleNames;
        public string MiddleNamesFeedback;
        public string MiddleNamesValid;
        public string LastName;
        public string LastNameFeedback;
        public string LastNameValid;
        public string BirthDate;
        public string BirthDateFeedback;
        public string BirthDateValid;
        public string StartDate;
        public string EndDate;
        public string Street;
        public string StreetFeedback;
        public string StreetValid;
        public string Place;
        public string PlaceFeedback;
        public string PlaceValid;
        public string PostalCode;
        public string PostalCodeFeedback;
        public string PostalCodeValid;
        public string MailAddress;
        public string MailAddressFeedback;
        public string MailAddressValid;
        public string Country;
        public string CountryFeedback;
        public string CountryValid;
        public string State;
        public string StateFeedback;
        public string StateValid;
        public List<NamedIdViewModel> States;
        public List<NamedIdViewModel> Countries;
        public string Language;

        public JoinViewModel()
        {
        }

        public JoinViewModel(Translator translator, IDatabase database)
        {
            LoadPhrases(translator);
        }

        private void LoadPhrases(Translator translator)
        {
            PhraseFieldUserName = translator.Get("Subscription.Join.Field.UserName", "User name field on the join page", "User name").EscapeHtml();
            PhraseFieldFirstName = translator.Get("Subscription.Join.Field.FirstName", "First name field on the join page", "First name").EscapeHtml();
            PhraseFieldMiddleNames = translator.Get("Subscription.Join.Field.dMiddleNames", "Middle names field on the join page", "Middle names").EscapeHtml();
            PhraseFieldLastName = translator.Get("Subscription.Join.Field.LastName", "Last name field on the join page", "Last name").EscapeHtml();
            PhraseFieldBirthDate = translator.Get("Subscription.Join.Field.BirthDate", "Birth date field on the join page", "Birth date").EscapeHtml();
            PhraseFieldStreet = translator.Get("Subscription.Join.Field.Street", "Street field on the join page", "Street").EscapeHtml();
            PhraseFieldPlace = translator.Get("Subscription.Join.Field.Place", "Place field on the join page", "Place").EscapeHtml();
            PhraseFieldPostalCode = translator.Get("Subscription.Join.Field.PostalCode", "Postal code field on the join page", "Postal code").EscapeHtml();
            PhraseFieldMailAddress = translator.Get("Subscription.Join.Field.MailAddress", "Mail address field on the join page", "E-Mail").EscapeHtml();
            PhraseFieldCountry = translator.Get("Subscription.Join.Field.Country", "Country field on the join page", "Country").EscapeHtml();
            PhraseFieldState = translator.Get("Subscription.Join.Field.State", "State field on the join page", "State").EscapeHtml();
            PhraseButtonJoin = translator.Get("Subscription.Join.Button.Join", "Join button on the join page", "Join").EscapeHtml();
            StartDate = "-41975d";
            EndDate = "-1825d";
            Language = translator.Language.Locale();
        }

        public JoinViewModel(Translator translator, IDatabase database, Subscription subscription, string linkUrl, string mailAddress)
            : this(translator, database)
        {
            var page = subscription.JoinPrePages.Value(database, translator.Language);
            SubscriptionId = subscription.Id.Value.ToString();
            PersonId = "new";
            LinkUrl = linkUrl;
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
            MailAddress = mailAddress;
            States = database.Query<State>()
                .OrderBy(s => s.Name.Value[translator.Language])
                .Select(s => new NamedIdViewModel(translator, s, false))
                .ToList();
            States.Add(new NamedIdViewModel(translator.Get("Subscription.Join.Field.State.None", "No selection in the select state field of the join page", "None"), false, true));
            Countries = database.Query<Country>()
                .OrderBy(c => c.Name.Value[translator.Language])
                .Select(c => new NamedIdViewModel(translator, c, false))
                .ToList();
        }

        public JoinViewModel(Translator translator, IDatabase database, Subscription subscription, string linkUrl, Person person)
            : this(translator, database)
        {
            var page = subscription.JoinPrePages.Value(database, translator.Language);
            SubscriptionId = subscription.Id.Value.ToString();
            PersonId = person.Id.Value.ToString();
            LinkUrl = linkUrl;
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
            UserName = person.UserName.Value;
            FirstName = person.FirstName.Value;
            MiddleNames = person.MiddleNames.Value;
            LastName = person.LastName.Value;
            BirthDate = person.BirthDate.Value.Year < 1900 ? string.Empty : person.BirthDate.Value.FormatSwissDateDay();
            MailAddress = person.PrimaryMailAddress;
            if (person.PrimaryPostalAddress != null)
            {
                Street = person.PrimaryPostalAddress.Street.Value;
                Place = person.PrimaryPostalAddress.Place.Value;
                PostalCode = person.PrimaryPostalAddress.PostalCode.Value;
                States = database.Query<State>()
                    .OrderBy(s => s.Name.Value[translator.Language])
                    .Select(s => new NamedIdViewModel(translator, s, person.PrimaryPostalAddress.State.Value == s))
                    .ToList();
                States.Add(new NamedIdViewModel(translator.Get("Subscription.Join.Field.State.None", "No selection in the select state field of the join page", "None"), false, false));
                Countries = database.Query<Country>()
                    .OrderBy(c => c.Name.Value[translator.Language])
                    .Select(c => new NamedIdViewModel(translator, c, person.PrimaryPostalAddress.Country.Value == c))
                    .ToList();
            }
            else
            {
                States = database.Query<State>()
                    .OrderBy(s => s.Name.Value[translator.Language])
                    .Select(s => new NamedIdViewModel(translator, s, false))
                    .ToList();
                States.Add(new NamedIdViewModel(translator.Get("Subscription.Join.Field.State.None", "No selection in the select state field of the join page", "None"), false, true));
                Countries = database.Query<Country>()
                    .OrderBy(c => c.Name.Value[translator.Language])
                    .Select(c => new NamedIdViewModel(translator, c, false))
                    .ToList();
            }
        }

        public void Reload(Translator translator, IDatabase database, Subscription subscription, Person person)
        {
            LoadPhrases(translator);
            var page = subscription.SubscribePrePages.Value(database, translator.Language);
            SubscriptionId = subscription.Id.Value.ToString();
            PersonId = person.Id.Value.ToString();
            Text = page.HtmlText.Value;
            Title = page.Subject.Value;
            States = database.Query<State>()
                .OrderBy(s => s.Name.Value[translator.Language])
                .Select(s => new NamedIdViewModel(translator, s, s.Id.ToString() == State))
                .ToList();
            States.Add(new NamedIdViewModel(translator.Get("Subscription.Join.Field.State.None", "No selection in the select state field of the join page", "None"), false, string.IsNullOrEmpty(State)));
            Countries = database.Query<Country>()
                .OrderBy(c => c.Name.Value[translator.Language])
                .Select(c => new NamedIdViewModel(translator, c, c.Id.ToString() == Country))
                .ToList();
        }
    }

    public class LinkContentProvider : IContentProvider
    {
        private readonly string _linkUrl;

        public LinkContentProvider(string linkUrl)
        {
            _linkUrl = linkUrl;
        }

        public string Prefix => "Link";

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "Link.Url":
                    return _linkUrl;
                default:
                    return string.Empty;
            }
        }
    }

    public class SubscriptionModule : QuaesturModule
    {
        private bool SendMail(Subscription subscription, MailboxAddress to, Templator templator, MailTemplate template, Translator translator)
        {
            var from = new MailboxAddress(
            subscription.SenderGroup.Value.MailName.Value[translator.Language],
            subscription.SenderGroup.Value.MailAddress.Value[translator.Language]);
            var senderKey = string.IsNullOrEmpty(subscription.SenderGroup.Value.GpgKeyId.Value) ? null :
                new GpgPrivateKeyInfo(
                subscription.SenderGroup.Value.GpgKeyId.Value,
                subscription.SenderGroup.Value.GpgKeyPassphrase.Value);
            var content = new Multipart("mixed");
            var htmlText = templator.Apply(template.HtmlText.Value);
            var plainText = templator.Apply(template.PlainText.Value);
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainText };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            var htmlPart = new TextPart("html") { Text = htmlText };
            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(htmlPart);
            content.Add(alternative);

            try
            {
                Global.MailCounter.Used();
                Global.Mail.Send(from, to, senderKey, null, template.Subject.Value, content);
                return true;
            }
            catch (Exception exception)
            {
                Global.Log.Error(exception.ToString());
                return false;
            }
        }

        private bool SendSubscribeMail(Subscription subscription, MailTemplate template, Translator translator, string mailAddress, string action, long number)
        {
            var mail = EncodeAddress(mailAddress);
            var expiry = DateTime.UtcNow.AddDays(10).Ticks.ToString();
            var link = CreateLink(action,
                                  translator.Language.Locale(),
                                  subscription.Id.Value.ToString(),
                                  mail,
                                  expiry);
            var to = new MailboxAddress(mailAddress, mailAddress);
            var templator = new Templator(new LinkContentProvider(link));
            if (SendMail(subscription, to, templator, template, translator))
            {
                Global.SubscribeThrottle.Sent(mailAddress, number);
                Global.Log.Info("Sent {0} mail to {1}", action, mailAddress);
                return true;
            }
            return false;
        }

        public static string CreateJoinLink(IDatabase database, Person person)
        {
            var subscription = database.Query<Subscription>()
                .SingleOrDefault(s => person.ActiveMemberships.Any(m => m.Type.Value == s.Membership.Value));
            if (subscription != null)
            {
                return CreateLink("join",
                                  person.Language.Value.Locale(),
                                  subscription.Id.Value.ToString(),
                                  person.Id.Value.ToString(),
                                  DateTime.UtcNow.AddDays(30).Ticks.ToString());
            }
            else
            {
                return string.Empty;
            }
        }

        public static string CreateUnsubscribeLink(IDatabase database, Person person)
        {
            var subscription = database.Query<Subscription>()
                .SingleOrDefault(s => person.ActiveMemberships.Any(m => m.Type.Value == s.Membership.Value));
            if (subscription != null)
            {
                return CreateLink("unsubscribe",
                                  person.Language.Value.Locale(),
                                  subscription.Id.Value.ToString(),
                                  person.Id.Value.ToString(),
                                  DateTime.UtcNow.AddDays(30).Ticks.ToString());
            }
            else
            {
                return string.Empty;
            }
        }

        private static string CreateLink(string action, params string[] arguments)
        {
            return string.Format("{0}/{1}/{2}/{3}",
                Global.Config.WebSiteAddress,
                action,
                string.Join("/", arguments),
                CreateAuth(action, arguments));
        }

        private static bool VerifyAuth(string authValue, string action, params string[] arguments)
        {
            return authValue == CreateAuth(action, arguments);
        }

        private static string CreateAuth(string action, params string[] arguments)
        {
            using (var serializer = new Serializer())
            {
                serializer.WritePrefixed("SUBSCRIPTION");
                serializer.WritePrefixed(action);
                foreach (var arg in arguments)
                {
                    serializer.WritePrefixed(arg);
                }
                using (var hmac = new HMACSHA256())
                {
                    hmac.Key = Global.Config.LinkKey;
                    var authValue = hmac.ComputeHash(serializer.Data).Part(0, 32).ToHexString();
                    return authValue;
                }
            }
        }

        private static bool CheckExpiry(string expiryString)
        { 
            if (long.TryParse(expiryString, out long ticks))
            {
                var expiry = new DateTime(ticks);
                return expiry >= DateTime.UtcNow;
            }
            else
            {
                return false;
            }
        }

        private string EncodeAddress(string address)
        {
            return HttpUtility.UrlEncode(address);
        }

        private bool TryParseAddress(string encoded, out string address)
        {
            try
            {
                address = HttpUtility.UrlDecode(encoded);
                return true;
            }
            catch
            {
                address = null;
                return false;
            }
        }

        private Negotiator Expired(string linkUrl)
        {
            return View["View/info.sshtml",
                new InfoViewModel(Database, Translator,
                    Translator.Get("Subscription.Expired.Title", "Expired title on the subscribe page", "Link expired").EscapeHtml(),
                    Translator.Get("Subscription.Expired.Text", "Expired text on the subscribe page", "This link has expired.").EscapeHtml(),
                    Translator.Get("Subscription.Expired.Link", "Expired back link on the subscribe page", "Back").EscapeHtml(),
                    linkUrl)];
        }

        private bool Subscribe(Subscription subscription, string mailAddress)
        {
            using (var transaction = Database.BeginTransaction())
            {
                foreach (var address in Database
                    .Query<ServiceAddress>(DC.Equal("service", (int)ServiceType.EMail)
                    .And(DC.Equal("address", mailAddress))))
                {
                    var person = address.Person.Value;
                    if (person.ActiveMemberships.Any(m => m.Type.Value != subscription.Membership.Value))
                    {
                        Journal("Subscribe Page", person, "Subscription.Subscribe.Journal.Error", "Error journal event on the subscribe page", "Tried to subscribe despite other memberships");
                        transaction.Commit();
                        Global.Mail.SendAdmin(
                            "Subscribe error caused by memberships",
                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                        return false;
                    }
                    else
                    {
                        if (person.Deleted.Value)
                        {
                            person.Deleted.Value = false;
                            Database.Save(person);
                        }
                        if (!person.ActiveMemberships.Any(m => m.Type.Value == subscription.Membership.Value))
                        {
                            var membership = new Membership(Guid.NewGuid());
                            membership.Organization.Value = subscription.Membership.Value.Organization.Value;
                            membership.Type.Value = subscription.Membership.Value;
                            membership.Person.Value = person;
                            membership.StartDate.Value = DateTime.UtcNow.Date;
                            membership.HasVotingRight.Value = false;
                            Database.Save(membership);
                        }
                        if (!person.TagAssignments.Any(ta => ta.Tag.Value == subscription.Tag.Value))
                        {
                            var tagAssignment = new TagAssignment(Guid.NewGuid());
                            tagAssignment.Tag.Value = subscription.Tag.Value;
                            tagAssignment.Person.Value = person;
                            Database.Save(tagAssignment);
                        }
                        Journal("Subscribe Page", person, "Subscription.Subscribe.Journal.Resubscribed", "Resubscribe journal event on the subscribe page", "Resubscribed");
                        transaction.Commit();
                        Global.Mail.SendAdmin(
                            "New subscription",
                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                        return true;
                    }
                }

                {
                    var person = new Person(Guid.NewGuid());
                    person.UserName.Value = mailAddress.Split(new string[] { "@" }, StringSplitOptions.None).First();
                    person.FirstName.Value = string.Empty;
                    person.MiddleNames.Value = string.Empty;
                    person.LastName.Value = string.Empty;
                    person.UserStatus.Value = UserStatus.Locked;
                    person.Language.Value = Translator.Language;
                    person.Deleted.Value = false;
                    Database.Save(person);

                    var serviceAddress = new ServiceAddress(Guid.NewGuid());
                    serviceAddress.Person.Value = person;
                    serviceAddress.Service.Value = ServiceType.EMail;
                    serviceAddress.Category.Value = AddressCategory.Home;
                    serviceAddress.Address.Value = mailAddress;
                    Database.Save(serviceAddress);

                    var membership = new Membership(Guid.NewGuid());
                    membership.Organization.Value = subscription.Membership.Value.Organization.Value;
                    membership.Type.Value = subscription.Membership.Value;
                    membership.Person.Value = person;
                    membership.StartDate.Value = DateTime.UtcNow.Date;
                    membership.HasVotingRight.Value = false;
                    Database.Save(membership);

                    var tagAssignment = new TagAssignment(Guid.NewGuid());
                    tagAssignment.Tag.Value = subscription.Tag.Value;
                    tagAssignment.Person.Value = person;
                    Database.Save(tagAssignment);

                    Journal("Subscribe Page", person, "Subscription.Subscribe.Journal.Create", "Create journal event on the subscribe page", "Was created by subscribing");
                    transaction.Commit();
                    Global.Mail.SendAdmin(
                        "New subscription",
                        Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                    return true;
                }
            }
        }

        public SubscriptionModule()
        {
            Get("/subscribe/{lang}/{sid}", parameters =>
            {
                Console.WriteLine("/subscribe/lang/sid");
                CurrentLanguage = ConvertLocale(parameters.lang);
                string subscriptionIdString = parameters.sid;
                var subscription = Database.Query<Subscription>(subscriptionIdString);

                if (subscription != null)
                {
                    Console.WriteLine("  view");
                    return View["View/subscribe.sshtml",
                        new SubscribeViewModel(Translator, Database, subscription, "subscribe")];
                }


                Console.WriteLine("  denied");
                return AccessDenied();
            });
            Post("/subscribe/{lang}/{sid}", parameters =>
            {
                CurrentLanguage = ConvertLocale(parameters.lang);
                string subscriptionIdString = parameters.sid;
                return CheckSendSubscribeMail(subscriptionIdString, false);
            });
            Get("/subscribed/{lang}/{sid}/{address}/{expiry}/{auth}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;
                string addressString = parameters.address;
                string expiryString = parameters.expiry;
                string authValue = parameters.auth;

                if (VerifyAuth(authValue, "subscribed", languageString, subscriptionIdString, EncodeAddress(addressString), expiryString))
                {
                    if (!CheckExpiry(expiryString))
                    {
                        return Expired(string.Format("/subscribe/{0}/{1}", CurrentLanguage.Locale(), subscriptionIdString));
                    }

                    CurrentLanguage = ConvertLocale(parameters.lang);
                    var subscription = Database.Query<Subscription>(subscriptionIdString);

                    if ((subscription != null) &&
                        TryParseAddress(addressString, out string address))
                    {
                        Subscribe(subscription, address);
                        return View["View/subscribed.sshtml",
                            new SubscribedViewModel(Translator, Database, subscription)];
                    }
                }

                return AccessDenied();
            });
            Get("/unsubscribe/{lang}/{sid}/{pid}/{expiry}/{auth}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;
                string personIdString = parameters.pid;
                string expiryString = parameters.expiry;
                string authValue = parameters.auth;

                if (VerifyAuth(authValue, "unsubscribe", languageString, subscriptionIdString, personIdString, expiryString))
                {
                    if (!CheckExpiry(expiryString))
                    {
                        return Expired("/");
                    }

                    CurrentLanguage = ConvertLocale(parameters.lang);
                    var subscription = Database.Query<Subscription>(subscriptionIdString);
                    var person = Database.Query<Person>(personIdString);

                    if ((subscription != null) &&
                        (person != null))
                    {
                        var linkUrl = CreateLink("unsubscribe",
                                                 languageString,
                                                 subscriptionIdString,
                                                 personIdString,
                                                 DateTime.UtcNow.AddDays(10).Ticks.ToString());
                        return View["View/unsubscribe.sshtml",
                            new UnsubscribeViewModel(Translator, Database, subscription, linkUrl)];
                    }
                }

                return AccessDenied();
            });
            Post("/unsubscribe/{lang}/{sid}/{pid}/{expiry}/{auth}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;
                string personIdString = parameters.pid;
                string expiryString = parameters.expiry;
                string authValue = parameters.auth;

                if (VerifyAuth(authValue, "unsubscribe", languageString, subscriptionIdString, personIdString, expiryString))
                {
                    if (!CheckExpiry(expiryString))
                    {
                        return Expired("/");
                    }

                    CurrentLanguage = ConvertLocale(parameters.lang);
                    using (var transaction = Database.BeginTransaction())
                    {
                        var subscription = Database.Query<Subscription>(subscriptionIdString);
                        var person = Database.Query<Person>(personIdString);

                        if ((subscription != null) &&
                            (person != null))
                        {
                            person.Deleted.Value = true;
                            Journal("Unsubscribe Page", person, "Subscription.Subscribe.Journal.Unsubscribe", "Unsubscribe journal event on the subscribe page", "Unsubscribed");
                            transaction.Commit();
                            Global.Mail.SendAdmin(
                                "Unsubscription",
                                Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                            return View["View/unsubscribed.sshtml",
                                new UnsubscribedViewModel(Translator, Database, subscription)];
                        }
                        else
                        {
                            transaction.Rollback();
                        }
                    }
                }

                return AccessDenied();
            });
            Get("/join/{lang}/{sid}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;

                CurrentLanguage = ConvertLocale(parameters.lang);
                var subscription = Database.Query<Subscription>(subscriptionIdString);

                if (subscription != null)
                {
                    var saveUrl = string.Format("/join/{0}/{1}",
                                                CurrentLanguage.Locale(),
                                                subscription.Id.Value);
                    var page = subscription.SubscribePrePages.Value(Database, CurrentLanguage);
                    var joinForm = new JoinForm(this, page, saveUrl, null, null);
                    return joinForm.Render();

                    //return View["View/subscribe.sshtml",
                    //    new SubscribeViewModel(Translator, Database, subscription, "join")];
                }

                return AccessDenied();
            });
            Post("/join/{lang}/{sid}", parameters =>
            {
                string subscriptionIdString = parameters.sid;
                CurrentLanguage = ConvertLocale(parameters.lang);
                return CheckSendSubscribeMail(subscriptionIdString, true);
            });
            Get("/join/{lang}/{sid}/{pid}/{expiry}/{auth}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;
                string personIdString = parameters.pid;
                string expiryString = parameters.expiry;
                string authValue = parameters.auth;

                if (VerifyAuth(authValue, "join", languageString, subscriptionIdString, personIdString, expiryString))
                {
                    if (!CheckExpiry(expiryString))
                    {
                        return Expired("/");
                    }

                    CurrentLanguage = ConvertLocale(parameters.lang);
                    var subscription = Database.Query<Subscription>(subscriptionIdString);
                    var person = Database.Query<Person>(personIdString);

                    if (subscription != null)
                    {
                        if (person != null)
                        {
                            var saveUrl = CreateLink("join",
                                                     languageString,
                                                     subscriptionIdString,
                                                     personIdString,
                                                     DateTime.UtcNow.AddDays(10).Ticks.ToString());
                            var page = subscription.SubscribePrePages.Value(Database, CurrentLanguage);
                            var joinForm = new JoinForm(this, page, saveUrl, person, null);
                            return joinForm.Render();
                            //return View["View/join.sshtml",
                            //    new JoinViewModel(Translator, Database, subscription, linkUrl, person)];
                        }
                        else if (TryParseAddress(personIdString, out string mailAddress))
                        {
                            var saveUrl = CreateLink("join",
                                                     languageString,
                                                     subscriptionIdString,
                                                     personIdString,
                                                     DateTime.UtcNow.AddDays(10).Ticks.ToString());
                            var page = subscription.SubscribePrePages.Value(Database, CurrentLanguage);
                            var joinForm = new JoinForm(this, page, saveUrl, null, mailAddress);
                            return joinForm.Render();
                            //return View["View/join.sshtml",
                            //    new JoinViewModel(Translator, Database, subscription, linkUrl, mailAddress)];
                        }
                    }
                }

                return AccessDenied();
            });
            base.Post("/join/{lang}/{sid}/{pid}/{expiry}/{auth}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;
                string personIdString = parameters.pid;
                string expiryString = parameters.expiry;
                string authValue = parameters.auth;

                if (VerifyAuth(authValue, "join", languageString, subscriptionIdString, personIdString, expiryString))
                {
                    if (!CheckExpiry(expiryString))
                    {
                        return Expired("/");
                    }

                    CurrentLanguage = ConvertLocale(parameters.lang);
                    var subscription = Database.Query<Subscription>(subscriptionIdString);
                    using (var transaction = Database.BeginTransaction())
                    {
                        var person = Database.Query<Person>(personIdString);

                        if (subscription != null)
                        {
                            var model = this.Bind<JoinViewModel>();
                            var required = Translator.Get("Subscription.Join.Validation.Missing", "Missing value on the join page", "This field is required").EscapeHtml();
                            var invalid = Translator.Get("Subscription.Join.Validation.Invalid", "Invalid value on the join page", "Value is not valid").EscapeHtml();
                            var valid = true;
                            DateTime birthDate = new DateTime(1850, 1, 1);

                            valid &= ValidateNotEmpty(required, model.UserName, ref model.UserNameFeedback, ref model.UserNameValid);
                            valid &= ValidateNotEmpty(required, model.FirstName, ref model.FirstNameFeedback, ref model.FirstNameValid);
                            valid &= ValidateNotEmpty(required, model.LastName, ref model.LastNameFeedback, ref model.LastNameValid);
                            valid &= ValidateNotEmpty(required, model.BirthDate, ref model.BirthDateFeedback, ref model.BirthDateValid);
                            if (ValidateNotEmpty(required, model.BirthDate, ref model.BirthDateFeedback, ref model.BirthDateValid))
                            {
                                if (model.BirthDate.TryParseDate(out DateTime value))
                                {
                                    if ((value >= DateTime.UtcNow.Date.AddYears(-115) &&
                                        (value <= DateTime.UtcNow.Date.AddYears(-5))))
                                    {
                                        birthDate = value;
                                    }
                                    else
                                    {
                                        model.BirthDateFeedback = required;
                                        model.BirthDateValid = "is-invalid";
                                        valid = false;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("BirthDate not valid");
                                    model.BirthDateFeedback = required;
                                    model.BirthDateValid = "is-invalid";
                                    valid = false;
                                }
                            }
                            else
                            {
                                valid = false;
                            }
                            valid &= ValidateNotEmpty(required, model.Street, ref model.StreetFeedback, ref model.StreetValid);
                            valid &= ValidateNotEmpty(required, model.Place, ref model.PlaceFeedback, ref model.PlaceValid);
                            valid &= ValidateNotEmpty(required, model.PostalCode, ref model.PostalCodeFeedback, ref model.PostalCodeValid);
                            valid &= ValidateNotEmpty(required, model.Country, ref model.CountryFeedback, ref model.CountryValid);

                            if (valid)
                            {
                                if (person != null)
                                {
                                    if (!person.ActiveMemberships.Any(m => m.Type.Value != subscription.Membership.Value))
                                    {
                                        UpdatePersonNames(person, model, birthDate);
                                        AddOrUpdatePostalAddress(person, model);
                                        Journal("Join Page", person, "Subscription.Join.Journal.Join", "Join journal event on the join page", "Requested to join");
                                        transaction.Commit();
                                        Global.Mail.Send(
                                            subscription.SenderGroup.Value.MailAddress.Value.AnyValue,
                                            "New member joined",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        Global.Mail.SendAdmin(
                                            "New member joined",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        return View["View/joined.sshtml",
                                            new JoinedViewModel(Translator, Database, subscription, person)];
                                    }
                                    else
                                    {
                                        Journal("Join Page", person, "Subscription.Join.Journal.Error", "Error journal event on the join page", "Tried to join despite other memberships");
                                        transaction.Commit();
                                        Global.Mail.SendAdmin(
                                            "Join error caused by memberships",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        return JoinError(subscription);
                                    }
                                }
                                else if (TryParseAddress(personIdString, out string mailAddress))
                                {
                                    var oldAddress = Database
                                        .Query<ServiceAddress>(DC.Equal("service", (int)ServiceType.EMail)
                                        .And(DC.Equal("address", mailAddress)))
                                        .FirstOrDefault();

                                    if (oldAddress == null)
                                    {
                                        person = new Person(Guid.NewGuid());
                                        UpdatePersonNames(person, model, birthDate);
                                        AddPostalAddress(person, model);
                                        AddMailAddress(person, mailAddress);
                                        Journal("Join Page", person, "Subscription.Join.Journal.Create", "Create journal event on the join page", "Was created by joining");
                                        Journal("Join Page", person, "Subscription.Join.Journal.Join", "Join journal event on the join page", "Requested to join");
                                        transaction.Commit();
                                        Global.Mail.Send(
                                            subscription.SenderGroup.Value.MailAddress.Value.AnyValue,
                                            "New member joined",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        Global.Mail.SendAdmin(
                                            "New member joined",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        return View["View/joined.sshtml",
                                            new JoinedViewModel(Translator, Database, subscription, person)];
                                    }
                                    else if (!person.ActiveMemberships.Any(m => m.Type.Value != subscription.Membership.Value))
                                    {
                                        person = oldAddress.Person.Value;
                                        UpdatePersonNames(person, model, birthDate);
                                        AddOrUpdatePostalAddress(person, model);
                                        Journal("Join Page", person, "Subscription.Join.Journal.Join", "Join journal event on the join page", "Requested to join");
                                        transaction.Commit();
                                        Global.Mail.Send(
                                            subscription.SenderGroup.Value.MailAddress.Value.AnyValue,
                                            "New member joined",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        Global.Mail.SendAdmin(
                                            "New member joined",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        return View["View/joined.sshtml",
                                            new JoinedViewModel(Translator, Database, subscription, person)];
                                    }
                                    else
                                    {
                                        person = oldAddress.Person.Value;
                                        Journal("Join Page", person, "Subscription.Join.Journal.Error", "Error journal event on the join page", "Tried to join despite other memberships");
                                        transaction.Commit();
                                        Global.Mail.SendAdmin(
                                            "Join error caused by memberships",
                                            Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        return JoinError(subscription);
                                    }
                                }
                                else
                                {
                                    transaction.Rollback();
                                    return JoinError(subscription);
                                }
                            }
                            else
                            {
                                transaction.Rollback();
                                model.Reload(Translator, Database, subscription, person);
                                return View["View/join.sshtml", model];
                            }
                        }

                        transaction.Rollback();
                    }
                }

                return AccessDenied();
            });
            Get("/confirm/{lang}/{sid}", parameters =>
            {
                CurrentLanguage = ConvertLocale(parameters.lang);
                string subscriptionIdString = parameters.sid;
                var subscription = Database.Query<Subscription>(subscriptionIdString);

                if (subscription != null)
                {
                    return View["View/confirm.sshtml",
                        new ConfirmViewModel(Translator, Database, subscription)];
                }

                return AccessDenied();
            });
            Post("/throttle", parameters =>
            {
                Console.WriteLine("throttle");
                var bodyString = ReadBody();

                if (TryParseJson(bodyString, out JObject request))
                {
                    Console.WriteLine(" json ok");
                    Console.WriteLine(request.ToString());
                    if (request.TryValueString("mailAddress", out string mailAddress) &&
                        request.TryValueInt32("counter", out int counter) &&
                        request.TryValueHexBytes("prefix", out byte[] prefix) &&
                        request.TryValueInt32("bitlength", out int bitLength) &&
                        request.TryValueHexBytes("target", out byte[] target) &&
                        request.TryValueHexBytes("encryptedMiddle", out byte[] encryptedMiddle) &&
                        request.TryValueInt32("number", out int number) &&
                        request.TryValueString("authValue", out string authValue) &&
                        request.TryValueHexBytes("solvedMiddle", out byte[] solvedMiddle))
                    {
                        Console.WriteLine(" next fields ok");
                        if (VerifyAuth(authValue,
                                       "throttle",
                                       mailAddress,
                                       counter.ToString(),
                                       prefix.ToHexString(),
                                       bitLength.ToString(),
                                       target.ToHexString(),
                                       encryptedMiddle.ToHexString(),
                                       number.ToString()))
                        {
                            Console.WriteLine(" auth ok");
                            if (TryDecryptThrottle(encryptedMiddle, out byte[] decryptedMiddle))
                            {
                                Console.WriteLine(" decrypt ok");
                                if (decryptedMiddle.AreEqual(solvedMiddle))
                                {
                                    Console.WriteLine(" middle ok");
                                    if (counter > 1)
                                    {
                                        Console.WriteLine(" next problem");
                                        var response = new JObject();
                                        AddProblem(mailAddress, bitLength, counter - 1, number, response);
                                        Console.WriteLine(response.ToString());
                                        return response.ToString();
                                    }
                                    else
                                    {
                                        Console.WriteLine(" throttled ticket");
                                        var newAuthValue = CreateAuth("throttled",
                                                                      mailAddress,
                                                                      number.ToString());
                                        var response = new JObject();
                                        response.Add(new JProperty("mailAddress", mailAddress));
                                        response.Add(new JProperty("number", number));
                                        response.Add(new JProperty("authValue", newAuthValue));
                                        Console.WriteLine(response.ToString());
                                        return response.ToString();
                                    }
                                }
                            }
                        }
                    }
                    else if (request.TryValueString("mailAddress", out string newMailAddress))
                    {
                        Console.WriteLine(" new fields ok");
                        if (!string.IsNullOrEmpty(newMailAddress) &&
                            UserController.ValidateMailAddress(newMailAddress))
                        {
                            Console.WriteLine(" mail address ok");
                            Global.SubscribeThrottle.Request(newMailAddress, out int newBitLength, out long newNumber);
                            var newCounter = 1;
                            while (newBitLength > 12)
                            {
                                newCounter *= 2;
                                newBitLength--;
                            }
                            var response = new JObject();
                            AddProblem(newMailAddress, newBitLength, newCounter, newNumber, response);
                            Console.WriteLine(response.ToString());
                            return response.ToString();
                        }
                        else
                        {
                            var response = new JObject();
                            AddMessage(response, EmailRequiredMessage());
                            Console.WriteLine(response.ToString());
                            return response.ToString();
                        }
                    }
                }

                {
                    var response = new JObject();
                    AddMessage(response, AnitSpamCheckFailedMessage());
                    Console.WriteLine(response.ToString());
                    return response.ToString();
                }
            });
            Get("/denied", parameters =>
            {
                return View["View/info.sshtml", new AccessDeniedViewModel(Database, Translator)];
            });
        }

        private void AddMessage(JObject response, string message)
        {
            response.Add(new JProperty("message", message));
        }

        private void AddProblem(string mailAddress, int bitlength, int counter, long number, JObject response)
        {
            var prefix = Rng.Get(32);
            var postfix = Rng.Get(bitlength / 8 + (((bitlength % 8) > 0) ? 1 : 0));
            if ((bitlength % 8) > 0)
            {
                postfix[0] = (byte)(postfix[0] & ((1 << (bitlength % 8)) - 1));
            }
            Console.WriteLine("postfix " + postfix.ToHexString());
            var start = prefix.Concat(postfix);
            var middle = start.HashSha256();
            var target = middle.HashSha256();
            var encryptedMiddle = BaseLibrary.Aes.Encrypt(middle, Global.Config.ThrottleEncryptionKey);

            var prefixString = prefix.ToHexString();
            var targetString = target.ToHexString();
            var encryptedMiddleString = encryptedMiddle.ToHexString();
            var authValue = CreateAuth("throttle",
                                       mailAddress,
                                       counter.ToString(),
                                       prefixString,
                                       bitlength.ToString(),
                                       targetString,
                                       encryptedMiddleString,
                                       number.ToString());

            response.Add(new JProperty("mailAddress", mailAddress));
            response.Add(new JProperty("counter", counter));
            response.Add(new JProperty("prefix", prefixString));
            response.Add(new JProperty("bitlength", bitlength));
            response.Add(new JProperty("target", targetString));
            response.Add(new JProperty("encryptedMiddle", encryptedMiddleString));
            response.Add(new JProperty("number", number));
            response.Add(new JProperty("authValue", authValue));
        }

        private object CheckSendSubscribeMail(string subscriptionIdString, bool preJoin)
        {
            Console.WriteLine("CheckSendSubscribeMail " + (preJoin ? "prejoin" : "subscribe"));
            var subscription = Database.Query<Subscription>(subscriptionIdString);

            if (subscription != null)
            {
                Console.WriteLine(" subscription ok");
                var requestString = ReadBody();

                if (TryParseJson(requestString, out JObject request))
                {
                    Console.WriteLine(" json ok");
                    Console.WriteLine(request.ToString());
                    if (request.TryValueString("mailAddress", out string mailAddress) &&
                        request.TryValueInt32("number", out int number) &&
                        request.TryValueString("authValue", out string authValue))
                    {
                        Console.WriteLine(" fields ok");
                        if (VerifyAuth(authValue,
                                       "throttled",
                                       mailAddress,
                                       number.ToString()))
                        {
                            Console.WriteLine(" auth ok");
                            if (Global.SubscribeThrottle.Check(mailAddress, number))
                            {
                                Console.WriteLine(" check ok");
                                var templateField = preJoin ? subscription.JoinPrePages : subscription.SubscribeMails;
                                var template = templateField.Value(Database, Translator.Language);
                                var linkAction = preJoin ? "join" : "subscribed";
                                if (SendSubscribeMail(subscription, template, Translator, mailAddress, linkAction, number))
                                {
                                    Console.WriteLine(" send ok");
                                    var response = new JObject();
                                    var redirectUrl = string.Format("/confirm/{0}/{1}", CurrentLanguage.Locale(), subscription.Id.Value.ToString());
                                    response.Add(new JProperty("redirect", redirectUrl));
                                    Console.WriteLine(response.ToString());
                                    return response.ToString();
                                }
                            }
                        }
                    }
                }
            }

            {
                var response = new JObject();
                response.Add(new JProperty("message", AnitSpamCheckFailedMessage()));
                Console.WriteLine(response.ToString());
                return response.ToString();
            }
        }

        private string AnitSpamCheckFailedMessage()
        {
            return Translate("Subscription.Validation.Check.Failed", "Anti-spam check failed message on invalid mail address on subscribe or join page", "The anti-spam check failed.");
        }

        private string EmailRequiredMessage()
        {
            return Translate("Subscription.Validation.Email.Required", "Required message on invalid mail address on subscribe or join page", "A vaild mail address is required.");
        }

        private bool TryParseJson(string data, out JObject obj)
        { 
            try
            {
                obj = JObject.Parse(data);
                return true;
            }
            catch
            {
                obj = null;
                return false;
            }
        }

        private bool ValidateNotEmpty(string required, string value, ref string feedback, ref string valid)
        {
            if (string.IsNullOrEmpty(value))
            {
                feedback = required;
                valid = "is-invalid";
                return false;
            }
            else
            {
                feedback = string.Empty;
                valid = string.Empty;
                return true;
            }
        }

        private Negotiator JoinError(Subscription subscription)
        {
            return View["View/info.sshtml",
                new InfoViewModel(Database, Translator,
                    Translator.Get("Subscription.Join.Error.Title", "Error title on the join page", "Error").EscapeHtml(),
                    Translator.Get("Subscription.Join.Error.Text", "Error text on the join page", "Unfortunatly an error has occurred.").EscapeHtml(),
                    Translator.Get("Subscription.Join.Error.Link", "Error back link on the join page", "Back").EscapeHtml(),
                    string.Format("/join/{0}/{1}", Translator.Language.Locale(), subscription.Id))];
        }

        private void AddOrUpdatePostalAddress(Person person, JoinViewModel model)
        {
            if (person.PrimaryPostalAddress == null)
            {
                AddPostalAddress(person, model);
            }
            else
            {
                UpdatePostalAdress(person, model);
            }
        }

        private void UpdatePersonNames(Person person, JoinViewModel model, DateTime birthDate)
        {
            person.UserName.Value = model.UserName ?? string.Empty;
            person.FirstName.Value = model.FirstName ?? string.Empty;
            person.MiddleNames.Value = model.MiddleNames ?? string.Empty;
            person.LastName.Value = model.LastName ?? string.Empty;
            person.BirthDate.Value = birthDate;
            Database.Save(person);
        }

        private void AddMailAddress(Person person, string mailAddress)
        {
            var serverAddress = new ServiceAddress(Guid.NewGuid());
            serverAddress.Person.Value = person;
            serverAddress.Service.Value = ServiceType.EMail;
            serverAddress.Category.Value = AddressCategory.Home;
            serverAddress.Address.Value = mailAddress;
            Database.Save(serverAddress);
        }

        private void AddPostalAddress(Person person, JoinViewModel model)
        {
            var postalAddress = new PostalAddress(Guid.NewGuid());
            postalAddress.Person.Value = person;
            postalAddress.Street.Value = model.Street ?? string.Empty;
            postalAddress.Place.Value = model.Place ?? string.Empty;
            postalAddress.PostalCode.Value = model.PostalCode ?? string.Empty;
            postalAddress.PostOfficeBox.Value = string.Empty;
            postalAddress.CareOf.Value = string.Empty;
            postalAddress.State.Value = Database.Query<State>(model.State);
            postalAddress.Country.Value = Database.Query<Country>(model.Country);
            Database.Save(postalAddress);
        }

        private void UpdatePostalAdress(Person person, JoinViewModel model)
        {
            var postalAddress = person.PrimaryPostalAddress;
            postalAddress.Street.Value = model.Street ?? string.Empty;
            postalAddress.Place.Value = model.Place ?? string.Empty;
            postalAddress.PostalCode.Value = model.PostalCode ?? string.Empty;
            postalAddress.PostOfficeBox.Value = string.Empty;
            postalAddress.CareOf.Value = string.Empty;
            postalAddress.State.Value = Database.Query<State>(model.State);
            postalAddress.Country.Value = Database.Query<Country>(model.Country);
            Database.Save(postalAddress);
        }

        private bool TryDecryptThrottle(byte[] cipherText, out byte[] plainText)
        {
            try
            {
                plainText = BaseLibrary.Aes.Decrypt(cipherText, Global.Config.ThrottleEncryptionKey);
                return true;
            }
            catch
            {
                plainText = null;
                return false;
            }
        }
    }
}

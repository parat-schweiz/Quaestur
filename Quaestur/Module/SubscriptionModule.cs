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
    public class NobilingViewModel
    {
        public string Id;
        public string Title;
        public string Text;
        public string Header;
        public string Footer;
        public string Language;
        public string PhraseButtonOk;

        private static string TemplateText(PageTemplate template, Templator templator)
        { 
            var preText = template.HtmlText?.Value ?? string.Empty;
            return templator == null ? preText : templator.Apply(preText);
        }

        public NobilingViewModel()
        { 
        }

        public NobilingViewModel(Translator translator, IDatabase database, Subscription subscription, string phraseButtonOk, PageTemplate page, Templator templator = null)
        {
            PhraseButtonOk = phraseButtonOk;
            Id = subscription.Id.Value.ToString();
            Title = page.Title.Value;
            Text = TemplateText(page, templator);
            Header = TemplateText(subscription.PageHeaders.Value(database, translator.Language), templator);
            Footer = TemplateText(subscription.PageFooters.Value(database, translator.Language), templator);
            Language = translator.Language.Locale();
        }
    }

    public class VerifyMailViewModel : NobilingViewModel
    {
        public string Email;
        public string Request;
        public string PhraseFieldEmail;
        public string PhraseThrottleWait;

        public VerifyMailViewModel()
        {
        }

        public VerifyMailViewModel(Translator translator, IDatabase database, Subscription subscription, string phraseButtonOk, PageTemplate page, string request)
            : base(translator, database, subscription, phraseButtonOk, page, null)
        {
            PhraseFieldEmail = translator.Get("Subscription.Subscribe.Field.Email", "Email field on the subscribe page", "E-Mail").EscapeHtml();
            PhraseThrottleWait = translator.Get("Subscription.Subscribe.Throttle.Wait", "Info text on the subscribe page when waiting for the anti-spam throttle", "Please wait for the anti-spam check...").EscapeHtml();
            Request = request;
        }
    }

    public class SubscribeViewModel : VerifyMailViewModel
    {
        public SubscribeViewModel()
        { 
        }

        public SubscribeViewModel(Translator translator, IDatabase database, Subscription subscription)
            : base(translator, database, subscription,
                   translator.Get("Subscription.Subscribe.Button.Subscribe", "Subscribe button on the subscribe page", "Subscribe").EscapeHtml(),
                   subscription.SubscribePrePages.Value(database, translator.Language),
                   "subscribe")
        {
        }
    }

    public class PreJoinViewModel : VerifyMailViewModel
    {
        public PreJoinViewModel()
        {
        }

        public PreJoinViewModel(Translator translator, IDatabase database, Subscription subscription)
            : base(translator, database, subscription,
                   translator.Get("Subscription.Subscribe.Button.Verify", "Verify button on the subscribe page", "Verify").EscapeHtml(),
                   subscription.JoinPrePages.Value(database, translator.Language),
                   "join")
        {
        }
    }

    public class ConfirmViewModel : NobilingViewModel
    {
        public ConfirmViewModel()
        {
        }

        public ConfirmViewModel(Translator translator, IDatabase database, Subscription subscription)
         : base(translator, database, subscription,
                string.Empty,
                subscription.ConfirmMailPages.Value(database, translator.Language))
        {
        }
    }

    public class SubscribedViewModel : NobilingViewModel
    {
        public SubscribedViewModel()
        {
        }

        public SubscribedViewModel(Translator translator, IDatabase database, Subscription subscription)
         : base(translator, database, subscription, string.Empty,
                subscription.SubscribePostPages.Value(database, translator.Language))
        { 
        }
    }

    public class JoinedViewModel : NobilingViewModel
    {
        public JoinedViewModel()
        {
        }

        public JoinedViewModel(Translator translator, IDatabase database, Subscription subscription, Person person)
         : base(translator, database, subscription, string.Empty,
               subscription.JoinPostPages.Value(database, translator.Language),
               new Templator(new PersonContentProvider(database, translator, person)))
        {
        }
    }

    public class UpdateViewModel : NobilingViewModel
    {
        public UpdateViewModel()
        {
        }

        public UpdateViewModel(Translator translator, IDatabase database, Subscription subscription, Person person)
         : base(translator, database, subscription, string.Empty,
               subscription.UpdatePages.Value(database, translator.Language),
               new Templator(new PersonContentProvider(database, translator, person)))
        {
        }
    }

    public class UnsubscribeViewModel : NobilingViewModel
    {
        public string LinkUrl;

        public UnsubscribeViewModel()
        {
        }

        public UnsubscribeViewModel(Translator translator, IDatabase database, Subscription subscription, string linkUrl)
         : base(translator, database, subscription,
                translator.Get("Subscription.Unsubscribe.Button.Unsubscribe", "Unsubscribe button on the unsubscribe page", "Unsubscribe").EscapeHtml(),
                subscription.UnsubscribePrePages.Value(database, translator.Language))
        {
            LinkUrl = linkUrl;
        }
    }

    public class UnsubscribedViewModel : NobilingViewModel
    {
        public UnsubscribedViewModel()
        {
        }

        public UnsubscribedViewModel(Translator translator, IDatabase database, Subscription subscription)
         : base(translator, database, subscription, string.Empty,
                subscription.UnsubscribePostPages.Value(database, translator.Language))
        {
        }
    }

    public class JoinForm : Form<Person>
    {
        public string Header;
        public string Footer;

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

        private string CountryOrder(Country country)
        {
            if (country.Code.Value.ToLowerInvariant() == "ch")
            {
                return "a_" + country.Name.Value;
            }
            else
            {
                return "b_" + country.Name.Value;
            }
        }

        public JoinForm(QuaesturModule module, Subscription subscription, PageTemplate template, string saveUrl, Person person, string mailAddress)
            : base(module, "JoinForm", template.Title.Value, saveUrl,
                   module.Translator.Get("Subscription.Join.Button.Join", "Join button on the join page", "Join").EscapeHtml(),
                   template.HtmlText.GetText(module.Translator))
        {
            Header = subscription.PageHeaders.Value(module.Database, module.CurrentLanguage).HtmlText?.Value ?? string.Empty;
            Footer = subscription.PageFooters.Value(module.Database, module.CurrentLanguage).HtmlText?.Value ?? string.Empty;

            if (person != null)
            {
                Add(new ReadOnlyWidget<Person, string>(
                    this, "MailAddress", 12,
                    module.Translator.Get("Subscription.Join.Field.MailAddress", "Mail address field on the join page", "E-Mail").EscapeHtml(),
                    p => p.PrimaryMailAddress));
            }
            else if (!string.IsNullOrEmpty(mailAddress))
            {
                Add(new ReadOnlyWidget<Person, string>(
                    this, "MailAddress", 12,
                    module.Translator.Get("Subscription.Join.Field.MailAddress", "Mail address field on the join page", "E-Mail").EscapeHtml(),
                    mailAddress));
            }

            var userNameTaken = module.Translator.Get("Subscription.Join.Field.UserName.Invalid.Duplicate", "Duplicate user name on the join page", "This username is already assigned to another user.").EscapeHtml();
            Add(new StringWidget<Person>(
                    this, "UserName", 12, true,
                    module.Translator.Get("Subscription.Join.Field.UserName", "User name field on the join page", "Username").EscapeHtml(),
                    p => p.UserName,
                    value => {
                        if (module.Database.Query<Person>(DC.Equal("username", value)).Any())
                        {
                            return userNameTaken;
                        }
                        else
                        {
                            return null;
                        }
                    }));
            Add(new StringWidget<Person>(
                    this, "FirstName", 12, true,
                    module.Translator.Get("Subscription.Join.Field.FirstName", "First name field on the join page", "First name").EscapeHtml(),
                    p => p.FirstName));
            Add(new StringWidget<Person>(
                    this, "MiddleNames", 12, false,
                    module.Translator.Get("Subscription.Join.Field.dMiddleNames", "Middle names field on the join page", "Middle names").EscapeHtml(),
                    p => p.MiddleNames));
            Add(new StringWidget<Person>(
                    this, "LastName", 12, true,
                    module.Translator.Get("Subscription.Join.Field.LastName", "Last name field on the join page", "Last name").EscapeHtml(),
                    p => p.LastName));
            Add(new DateWidget<Person>(
                    this, "BirthDate", 6, true,
                    module.Translator.Get("Subscription.Join.Field.BirthDate", "Birth date field on the join page", "Birth date").EscapeHtml(),
                    p => p.BirthDate,
                    null,
                    DateTime.UtcNow.Date.AddYears(-120),
                    DateTime.UtcNow.Date.AddYears(-12),
                    module.CurrentLanguage));
            Add(new SubWidget<Person, PostalAddress>(
                    new StringWidget<PostalAddress>(
                    this, "Street", 12, true,
                    module.Translator.Get("Subscription.Join.Field.Street", "Street field on the join page", "Street").EscapeHtml(),
                    s => s.Street),
                    GetPostalAddress));
            Add(new SubWidget<Person, PostalAddress>(
                    new StringWidget<PostalAddress>(
                    this, "PostalCode", 3, true,
                    module.Translator.Get("Subscription.Join.Field.PostalCode", "Postal code field on the join page", "Postal code").EscapeHtml(),
                    s => s.PostalCode),
                    GetPostalAddress));
            Add(new SubWidget<Person, PostalAddress>(
                    new StringWidget<PostalAddress>(
                    this, "Place", 9, true,
                    module.Translator.Get("Subscription.Join.Field.Place", "Place field on the join page", "Place").EscapeHtml(),
                    s => s.Place),
                    GetPostalAddress));
            Add(new SubWidget<Person, PostalAddress>(
                    new SelectIdWidget<PostalAddress, Country>(
                    this, "Country", 6, true,
                    module.Translator.Get("Subscription.Join.Field.Country", "Country field on the join page", "Country").EscapeHtml(),
                    s => s.Country,
                    c => new NamedIdViewModel(module.Translator, c, false),
                    null,
                    null,
                    null,
                    CountryOrder),
                    GetPostalAddress));
            Add(new SubWidget<Person, PostalAddress>(
                    new SelectIdWidget<PostalAddress, State>(
                    this, "State", 6, false,
                    module.Translator.Get("Subscription.Join.Field.State", "State field on the join page", "State").EscapeHtml(),
                    s => s.State,
                    s => new NamedIdViewModel(module.Translator, s, false),
                    () => new NamedIdViewModel(module.Translator.Get("Subscription.Join.Field.State.None", "No selection in the select state field of the join page", "None"), false, true)),
                    GetPostalAddress));

            if (person != null)
            {
                LoadValues(person);
            }
        }

        public override string Template => "View/Form/nobling_form.sshtml";
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
                .FirstOrDefault(s => person.ActiveMemberships.Any(m => m.Type.Value == s.Membership.Value));
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
                .FirstOrDefault(s => person.ActiveMemberships.Any(m => m.Type.Value == s.Membership.Value));
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
                        Global.SubscribeThrottle.Subscribed(mailAddress);
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
                    Global.SubscribeThrottle.Subscribed(mailAddress);
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
                CurrentLanguage = ConvertLocale(parameters.lang);
                string subscriptionIdString = parameters.sid;
                var subscription = Database.Query<Subscription>(subscriptionIdString);

                if (subscription != null)
                {
                    return View["View/subscribe.sshtml",
                        new SubscribeViewModel(Translator, Database, subscription)];
                }

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
                        return View["View/nobling_textonly.sshtml",
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
                            Global.SubscribeThrottle.Unsubscribed(person.PrimaryMailAddress);
                            Global.Mail.SendAdmin(
                                "Unsubscription",
                                Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                            return View["View/nobling_textonly.sshtml",
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
                    return View["View/subscribe.sshtml",
                        new PreJoinViewModel(Translator, Database, subscription)];
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

                if (VerifyAuth(authValue, "join", languageString, subscriptionIdString, EncodeAddress(personIdString), expiryString))
                {
                    if (!CheckExpiry(expiryString))
                    {
                        return Expired("/");
                    }

                    CurrentLanguage = ConvertLocale(parameters.lang);
                    var subscription = Database.Query<Subscription>(subscriptionIdString);
                    if (subscription != null)
                    {
                        var person = Database.Query<Person>(personIdString);
                        if (person != null)
                        {
                            var saveUrl = CreateLink("join",
                                                     languageString,
                                                     subscriptionIdString,
                                                     personIdString,
                                                     DateTime.UtcNow.AddDays(10).Ticks.ToString());
                            var page = subscription.JoinPages.Value(Database, CurrentLanguage);
                            var joinForm = new JoinForm(this, subscription, page, saveUrl, person, null);
                            return joinForm.Render();
                        }
                        else if (TryParseAddress(personIdString, out string mailAddress))
                        {
                            var saveUrl = CreateLink("join",
                                                     languageString,
                                                     subscriptionIdString,
                                                     personIdString,
                                                     DateTime.UtcNow.AddDays(10).Ticks.ToString());
                            var page = subscription.JoinPages.Value(Database, CurrentLanguage);
                            var joinForm = new JoinForm(this, subscription, page, saveUrl, null, mailAddress);
                            return joinForm.Render();
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
                var status = CreateStatus();

                if (VerifyAuth(authValue, "join", languageString, subscriptionIdString, personIdString, expiryString))
                {
                    if (CheckExpiry(expiryString))
                    {
                        var bodyString = ReadBody();

                        if (TryParseJson(bodyString, out JObject request))
                        {
                            CurrentLanguage = ConvertLocale(parameters.lang);
                            using (var transaction = Database.BeginTransaction())
                            {
                                var subscription = Database.Query<Subscription>(subscriptionIdString);
                                if (subscription != null)
                                {
                                    var page = subscription.JoinPages.Value(Database, CurrentLanguage);
                                    var person = Database.Query<Person>(personIdString);
                                    if (person != null)
                                    {
                                        if (!person.ActiveMemberships.Any(m => m.Type.Value != subscription.Membership.Value))
                                        {
                                            var joinForm = new JoinForm(this, subscription, page, null, person, null);
                                            joinForm.SaveValues(status, request, person);
                                            if (status.IsSuccess)
                                            {
                                                person.Language.Value = CurrentLanguage;
                                                joinForm.SaveObjects();
                                                Journal("Join Page", person, "Subscription.Join.Journal.Join", "Join journal event on the join page", "Requested to join");
                                                transaction.Commit();
                                                Global.Mail.Send(
                                                    subscription.SenderGroup.Value.MailAddress.Value.AnyValue,
                                                    "New member joined",
                                                    Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                                Global.Mail.SendAdmin(
                                                    "New member joined",
                                                    Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                                var expiry = DateTime.UtcNow.AddDays(10).Ticks.ToString();
                                                status.Redirect = CreateLink(
                                                    "joined",
                                                    CurrentLanguage.Locale(),
                                                    subscription.Id.Value.ToString(),
                                                    person.Id.Value.ToString(),
                                                    expiry);
                                            }
                                        }
                                        else
                                        {
                                            status.SetErrorAccessDenied();
                                            Global.Mail.SendAdmin(
                                                "Join error caused by memberships",
                                                Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
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
                                            var joinForm = new JoinForm(this, subscription, page, null, person, null);
                                            joinForm.SaveValues(status, request, person);
                                            if (status.IsSuccess)
                                            {
                                                person.Language.Value = CurrentLanguage;
                                                joinForm.SaveObjects();
                                                var newAddress = new ServiceAddress(Guid.NewGuid());
                                                newAddress.Person.Value = person;
                                                newAddress.Precedence.Value = 0;
                                                newAddress.Service.Value = ServiceType.EMail;
                                                newAddress.Category.Value = AddressCategory.Home;
                                                newAddress.Address.Value = mailAddress;
                                                Database.Save(newAddress);
                                                Journal("Join Page", person, "Subscription.Join.Journal.Create", "Create journal event on the join page", "Was created by joining");
                                                transaction.Commit();
                                                Global.Mail.Send(
                                                    subscription.SenderGroup.Value.MailAddress.Value.AnyValue,
                                                    "New member joined",
                                                    Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                                Global.Mail.SendAdmin(
                                                    "New member joined",
                                                    Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                                var expiry = DateTime.UtcNow.AddDays(10).Ticks.ToString();
                                                status.Redirect = CreateLink(
                                                    "joined",
                                                    CurrentLanguage.Locale(),
                                                    subscription.Id.Value.ToString(),
                                                    person.Id.Value.ToString(),
                                                    expiry);
                                            }
                                        }
                                        else if (!oldAddress.Person.Value.ActiveMemberships.Any(m => m.Type.Value != subscription.Membership.Value))
                                        {
                                            person = oldAddress.Person.Value;
                                            var joinForm = new JoinForm(this, subscription, page, null, person, null);
                                            joinForm.SaveValues(status, request, person);
                                            if (status.IsSuccess)
                                            {
                                                person.Language.Value = CurrentLanguage;
                                                joinForm.SaveObjects();
                                                Journal("Join Page", person, "Subscription.Join.Journal.Join", "Join journal event on the join page", "Requested to join");
                                                transaction.Commit();
                                                Global.Mail.Send(
                                                    subscription.SenderGroup.Value.MailAddress.Value.AnyValue,
                                                    "New member joined",
                                                    Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                                Global.Mail.SendAdmin(
                                                    "New member joined",
                                                    Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                                var expiry = DateTime.UtcNow.AddDays(10).Ticks.ToString();
                                                status.Redirect = CreateLink(
                                                    "joined",
                                                    CurrentLanguage.Locale(),
                                                    subscription.Id.Value.ToString(),
                                                    person.Id.Value.ToString(),
                                                    expiry);
                                            }
                                        }
                                        else
                                        {
                                            status.SetErrorAccessDenied();
                                            person = oldAddress.Person.Value;
                                            Global.Mail.SendAdmin(
                                                "Join error caused by memberships",
                                                Global.Config.WebSiteAddress + "/person/detail/" + person.Id.Value.ToString());
                                        }
                                    }
                                    else
                                    {
                                        status.SetErrorInvalidData();
                                    }
                                }
                                else
                                {
                                    status.SetErrorNotFound();
                                }

                                if (!status.IsSuccess)
                                {
                                    transaction.Rollback();
                                }
                            }
                        }
                        else
                        {
                            status.SetErrorInvalidData();
                        }
                    }
                    else
                    {
                        status.SetErrorAccessDenied();
                    }
                }
                else
                {
                    status.SetErrorAccessDenied();
                }

                return status.CreateJsonData();
            });
            Get("/confirm/{lang}/{sid}", parameters =>
            {
                CurrentLanguage = ConvertLocale(parameters.lang);
                string subscriptionIdString = parameters.sid;
                var subscription = Database.Query<Subscription>(subscriptionIdString);

                if (subscription != null)
                {
                    return View["View/nobling_textonly.sshtml",
                        new ConfirmViewModel(Translator, Database, subscription)];
                }

                return AccessDenied();
            });
            Post("/throttle", parameters =>
            {
                var bodyString = ReadBody();

                if (TryParseJson(bodyString, out JObject request))
                {
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
                            if (TryDecryptThrottle(encryptedMiddle, out byte[] decryptedMiddle))
                            {
                                if (decryptedMiddle.AreEqual(solvedMiddle))
                                {
                                    if (counter > 1)
                                    {
                                        var response = new JObject();
                                        AddProblem(mailAddress, bitLength, counter - 1, number, response);
                                        return response.ToString();
                                    }
                                    else
                                    {
                                        var newAuthValue = CreateAuth("throttled",
                                                                      mailAddress,
                                                                      number.ToString());
                                        var response = new JObject();
                                        response.Add(new JProperty("mailAddress", mailAddress));
                                        response.Add(new JProperty("number", number));
                                        response.Add(new JProperty("authValue", newAuthValue));
                                        return response.ToString();
                                    }
                                }
                            }
                        }
                    }
                    else if (request.TryValueString("mailAddress", out string newMailAddress))
                    {
                        if (!string.IsNullOrEmpty(newMailAddress) &&
                            UserController.ValidateMailAddress(newMailAddress))
                        {
                            Global.SubscribeThrottle.Request(newMailAddress, out int newBitLength, out long newNumber);
                            var newCounter = 1;
                            while (newBitLength > 12)
                            {
                                newCounter *= 2;
                                newBitLength--;
                            }
                            var response = new JObject();
                            AddProblem(newMailAddress, newBitLength, newCounter, newNumber, response);
                            return response.ToString();
                        }
                        else
                        {
                            var response = new JObject();
                            AddMessage(response, EmailRequiredMessage());
                            return response.ToString();
                        }
                    }
                }

                {
                    var response = new JObject();
                    AddMessage(response, AnitSpamCheckFailedMessage());
                    return response.ToString();
                }
            });
            Get("/denied", parameters =>
            {
                return View["View/info.sshtml", new AccessDeniedViewModel(Database, Translator)];
            });
            Get("/joined/{lang}/{sid}/{pid}/{expiry}/{auth}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;
                string personIdString = parameters.pid;
                string expiryString = parameters.expiry;
                string authValue = parameters.auth;
                var status = CreateStatus();

                if (VerifyAuth(authValue, "joined", languageString, subscriptionIdString, personIdString, expiryString))
                {
                    if (CheckExpiry(expiryString))
                    {
                        CurrentLanguage = ConvertLocale(languageString) ?? Language.German;
                        var subscription = Database.Query<Subscription>(subscriptionIdString);
                        var person = Database.Query<Person>(personIdString);

                        if ((subscription != null) && (person != null))
                        {
                            return View["View/nobling_textonly.sshtml", new JoinedViewModel(Translator, Database, subscription, person)];
                        }
                    }
                }

                return AccessDenied();
            });
            Get("/update/{lang}/{sid}/{pid}/{expiry}/{auth}", parameters =>
            {
                string languageString = parameters.lang;
                string subscriptionIdString = parameters.sid;
                string personIdString = parameters.pid;
                string expiryString = parameters.expiry;
                string authValue = parameters.auth;
                var status = CreateStatus();

                if (VerifyAuth(authValue, "update", languageString, subscriptionIdString, personIdString, expiryString))
                {
                    if (CheckExpiry(expiryString))
                    {
                        CurrentLanguage = ConvertLocale(languageString) ?? Language.German;
                        var subscription = Database.Query<Subscription>(subscriptionIdString);
                        var person = Database.Query<Person>(personIdString);

                        if ((subscription != null) && (person != null))
                        {
                            return View["View/nobling_textonly.sshtml", new UpdateViewModel(Translator, Database, subscription, person)];
                        }
                    }
                }

                return AccessDenied();
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
            var subscription = Database.Query<Subscription>(subscriptionIdString);

            if (subscription != null)
            {
                var requestString = ReadBody();

                if (TryParseJson(requestString, out JObject request))
                {
                    if (request.TryValueString("mailAddress", out string mailAddress) &&
                        request.TryValueInt32("number", out int number) &&
                        request.TryValueString("authValue", out string authValue))
                    {
                        if (VerifyAuth(authValue,
                                       "throttled",
                                       mailAddress,
                                       number.ToString()))
                        {
                            if (Global.SubscribeThrottle.Check(mailAddress, number))
                            {
                                var templateField = preJoin ? subscription.JoinConfirmMails : subscription.SubscribeMails;
                                var template = templateField.Value(Database, Translator.Language);
                                var linkAction = preJoin ? "join" : "subscribed";
                                if (SendSubscribeMail(subscription, template, Translator, mailAddress, linkAction, number))
                                {
                                    var response = new JObject();
                                    var redirectUrl = string.Format("/confirm/{0}/{1}", CurrentLanguage.Locale(), subscription.Id.Value.ToString());
                                    response.Add(new JProperty("redirect", redirectUrl));
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

        private void AddMailAddress(Person person, string mailAddress)
        {
            var serverAddress = new ServiceAddress(Guid.NewGuid());
            serverAddress.Person.Value = person;
            serverAddress.Service.Value = ServiceType.EMail;
            serverAddress.Category.Value = AddressCategory.Home;
            serverAddress.Address.Value = mailAddress;
            Database.Save(serverAddress);
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

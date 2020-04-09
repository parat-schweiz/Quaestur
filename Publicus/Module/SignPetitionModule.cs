using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using BaseLibrary;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Publicus
{
    public class LastActivity
    {
        private static readonly object _staticLock = new object();
        private static LastActivity _instance;

        public static LastActivity Instance
        {
            get
            {
                lock (_staticLock)
                {
                    if (_instance == null)
                    {
                        _instance = new LastActivity();
                    }

                    return _instance;
                }
            }
        }

        private class PetitionEntry
        {
            public object Lock { get; private set; }
            public long Index { get; set; }
            public long Counter { get; set; }
            public Queue<MultiLanguageString> Queue { get; private set; }

            public PetitionEntry()
            {
                Lock = new object();
                Index = 0;
                Counter = 0;
                Queue = new Queue<MultiLanguageString>(); 
            }
        }

        private readonly object _lock = new object();
        private Dictionary<Guid, PetitionEntry> _entries;

        public LastActivity()
        {
            _entries = new Dictionary<Guid, PetitionEntry>();
        }

        private PetitionEntry GetEntry(IDatabase database, Petition petition)
        {
            PetitionEntry entry = null;

            lock (_lock)
            {
                if (!_entries.ContainsKey(petition.Id.Value))
                {
                    _entries.Add(petition.Id.Value, new PetitionEntry());
                }

                entry = _entries[petition.Id.Value];
            }

            lock (entry.Lock)
            {
                if (entry.Index < 1)
                {
                    var translation = new Translation(database);
                    var signatories = database
                        .Query<TagAssignment>(DC.Equal("tagid", petition.PetitionTag.Value.Id.Value))
                        .ToList();
                    entry.Counter = signatories.Count;
                    var top3 = signatories
                        .OrderByDescending(ta => ta.Contact.Value.Subscriptions
                            .Where(s => s.IsActive && s.Feed.Value == petition.Group.Value.Feed.Value)
                            .MaxOrDefault(s => s.StartDate.Value, new DateTime(1850, 1, 1)))
                        .Take(3)
                        .Reverse()
                        .ToList();

                    foreach (var tagAssignment in top3)
                    {
                        var contact = tagAssignment.Contact.Value;
                        var showPublicly = contact.TagAssignments
                            .Any(ta => ta.Tag.Value == petition.ShowPubliclyTag.Value);
                        var postalAddress = contact.PostalAddresses.FirstOrDefault();
                        var place = postalAddress == null ? string.Empty : postalAddress.Place.Value;
                        var text = CreateText(translation, contact.FullName, place, showPublicly);
                        entry.Queue.Enqueue(text);
                    }

                    entry.Index = 1;
                } 
            }

            return entry;
        }

        private long MaxCounter(long counter)
        {
            long max = 10;
            while (counter >= max / 10 * 9)
            {
                max *= 10; 
            }
            return max;
        }

        public Tuple<long, string> GetCurrent(IDatabase database, Petition petition, Translator translator, long index)
        {
            var start = DateTime.UtcNow;
            var entry = GetEntry(database, petition);

            while (true)
            {
                lock (entry.Lock)
                {
                    if (entry.Index > index)
                    {
                        var text = string.Join("<br/>" + Environment.NewLine,
                            entry.Queue.Reverse().Select(e => e[translator.Language]));
                        var maxCounter = MaxCounter(entry.Counter);
                        text += "<br/>" + Environment.NewLine + "<div style=\"height: 10px;\"/>" + Environment.NewLine +
                            string.Format("<div class=\"progress bg-white\" style=\"height: 30px;\"><div class=\"progress-bar\" role=\"progressbar\" style=\"width: {0}%;\" aria-valuenow=\"{1}\" aria-valuemin=\"0\" aria-valuemax=\"{2}\">{3}</div></div>",
                            (int)Math.Floor(100d / (double)maxCounter * (double)entry.Counter),
                            entry.Counter,
                            maxCounter, 
                            translator.Get(
                            "Petition.Action.LastActivity.TotalSignatures",
                            "Total number of signatures last activity on the petition action page",
                            "{0} signatures",
                            entry.Counter).EscapeHtml());
                        /*
                        text += "<br/>" + Environment.NewLine + translator.Get(
                            "Petition.Action.LastActivity.Counter",
                            "Counter last activity on the petition action page",
                            "{0} other people have signed this petition",
                            entry.Counter).EscapeHtml();
                            */
                        return new Tuple<long, string>(
                            entry.Index,
                            text);
                    }

                    if (DateTime.UtcNow.Subtract(start).TotalSeconds > 2d)
                    {
                        return new Tuple<long, string>(index, string.Empty);
                    }
                }

                System.Threading.Thread.Sleep(100);
            }
        }

        public void Add(IDatabase database, Petition petition, Translation translation, string name, string place, bool anonymize)
        {
            var text = CreateText(translation, name, place, anonymize);

            var entry = GetEntry(database, petition);

            lock (entry.Lock)
            {
                entry.Counter++;
                entry.Index++;
                entry.Queue.Enqueue(text);

                if (entry.Queue.Count > 3)
                {
                    entry.Queue.Dequeue();
                }
            }
        }

        private static MultiLanguageString CreateText(Translation translation, string name, string place, bool showPublicly)
        {
            MultiLanguageString text = new MultiLanguageString();

            if (showPublicly)
            {
                foreach (var language in LanguageExtensions.All)
                {
                    var translator = new Translator(translation, language);
                    text[language] = translator.Get("" +
                        "Petition.Action.LastActivity.ShowName",
                        "Show name last activity on the petition action page",
                        "{0} from {1} just signed this petition",
                        name,
                        place).EscapeHtml();
                }
            }
            else
            {
                foreach (var language in LanguageExtensions.All)
                {
                    var translator = new Translator(translation, language);
                    text[language] = translator.Get("" +
                        "Petition.Action.LastActivity.Anonymized",
                        "Anonymized last activity on the petition action page",
                        "Someone from {0} just signed this petition",
                        place).EscapeHtml();
                }
            }

            return text;
        }
    }

    public class PetitionPageViewModel : PetitionActionBaseViewModel
    {
        public string Text;

        public PetitionPageViewModel(IDatabase database, Translator translator, Petition petition, MultiLanguageString text)
            : base(database, translator, petition)
        {
            Text = text[translator.Language];
        }
    }

    public class PetitionActionBaseViewModel
    {
        public string Id;
        public string Title;
        public string Language;
        public string WebAddress;
        public string PhrasePagePetition;
        public string PhrasePagePrivacy;
        public string PhrasePageFaq;
        public string PhrasePageImprint;

        public PetitionActionBaseViewModel()
        {
        }

        public PetitionActionBaseViewModel(IDatabase database, Translator translator, Petition petition)
        {
            PhrasePagePetition = translator.Get("Petition.Action.Page.Petition", "Page 'Petition' on the petition action page", "Petition").EscapeHtml();
            PhrasePagePrivacy = translator.Get("Petition.Action.Page.Privacy", "Page 'Privacy statement' on the petition action page", "Privacy statement").EscapeHtml();
            PhrasePageFaq = translator.Get("Petition.Action.Page.Faq", "Page 'FAQ' on the petition action page", "FAQ").EscapeHtml();
            PhrasePageImprint = translator.Get("Petition.Action.Page.Imprint", "Page 'Imprint' on the petition action page", "Imprint").EscapeHtml();

            Id = petition.Id.Value.ToString();
            Title = petition.Label.Value[translator.Language].EscapeHtml();
            WebAddress = petition.WebAddress.Value[translator.Language];

            switch (translator.Language)
            {
                case SiteLibrary.Language.French:
                    Language = "fr";
                    break;
                case SiteLibrary.Language.Italian:
                    Language = "it";
                    break;
                case SiteLibrary.Language.English:
                    Language = "en";
                    break;
                case SiteLibrary.Language.German:
                default:
                    Language = "de";
                    break;
            }
        }
    }

    public class PetitionActionViewModel : PetitionActionBaseViewModel
    {
        public string Text;

        public PetitionActionViewModel()
            : base()
        { 
        }

        public PetitionActionViewModel(IDatabase database, Translator translator, Petition petition)
            : base(database, translator, petition)
        {
            Text = petition.Text.Value[translator.Language];
        }
    }

    public class PetitionSignViewModel : PetitionActionViewModel
    {
        public string AlertType;
        public string PhraseFieldMail;
        public string PhraseInfo;
        public string PhraseButtonNext;

        public PetitionSignViewModel(IDatabase database, Translator translator, Petition petition)
            : base(database, translator, petition)
        {
            AlertType = "primary";
            PhraseFieldMail = translator.Get("Petition.Action.Field.Mail", "Field 'Mail' on the petition action page", "E-Mail").EscapeHtml();
            PhraseInfo = translator.Get("Petition.Action.Sign.Info", "Info text on the petition action page", "Please enter your e-mail address to sign the petition.").EscapeHtml();
            PhraseButtonNext = translator.Get("Petition.Action.Button.Next", "Button 'Next' on the petition action page", "Next").EscapeHtml();
        }
    }

    public class PetitionMailPostViewModel
    {
        public string Mail;
    }

    public class PetitionInfoViewModel : PetitionActionViewModel
    {
        public string AlertType;
        public string PhraseInfo;
        public bool PhraseShare;
        public string PhraseShareInfo;
        public string PhraseShareText;

        public PetitionInfoViewModel(IDatabase database, Translator translator, Petition petition, string info, string alertType, bool share)
            : base(database, translator, petition)
        {
            AlertType = alertType;
            PhraseInfo = info;
            PhraseShare = share;
            PhraseShareInfo = translator.Get(
                "Petition.Action.Info.Share",
                "Share information on the petition action page",
                "Please share our petition on social media").EscapeHtml();
            PhraseShareText = Nancy.Helpers.HttpUtility
                .UrlEncode(petition.ShareText.Value[translator.Language]);
        }
    }

    public class PetitionConfirmViewModel : PetitionActionViewModel
    {
        public string FirstName;
        public string LastName;
        public string Place;
        public string PostalCode;
        public bool SpecialNewsletter;
        public bool GeneralNewsletter;
        public bool ShowPublicly;
        public string EncodedMailAddress;
        public string Code;
        public string PhraseFieldFirstName;
        public string PhraseFieldLastName;
        public string PhraseFieldPlace;
        public string PhraseFieldPostalCode;
        public string PhraseFieldSpecialNewsletter;
        public string PhraseFieldGeneralNewsletter;
        public string PhraseFieldShowPublicly;
        public string AlertType;
        public string PhraseInfo;
        public string PhraseButtonConfirm;

        public PetitionConfirmViewModel()
        { 
        }

        public PetitionConfirmViewModel(
            IDatabase database, 
            Translator translator, 
            Petition petition, 
            string encodedMailAddress, 
            string code)
            : base(database, translator, petition)
        {
            AlertType = "primary";
            EncodedMailAddress = encodedMailAddress;
            Code = code;
            PhraseFieldFirstName = translator.Get("Petition.Action.Field.FirstName", "Field 'First name' on the petition action page", "First name").EscapeHtml();
            PhraseFieldLastName = translator.Get("Petition.Action.Field.LastName", "Field 'Last name' on the petition action page", "Last name").EscapeHtml();
            PhraseFieldPlace = translator.Get("Petition.Action.Field.Place", "Field 'Place' on the petition action page", "Place").EscapeHtml();
            PhraseFieldPostalCode = translator.Get("Petition.Action.Field.PostalCode", "Field 'Postal code' on the petition action page", "Postal code").EscapeHtml();
            PhraseFieldSpecialNewsletter = translator.Get("Petition.Action.Field.SpecialNewsletter", "Field 'Special newsletter' on the petition action page", "Keep me informed about further development with this petition").EscapeHtml();
            PhraseFieldGeneralNewsletter = translator.Get("Petition.Action.Field.GeneralNewsletter", "Field 'General newsletter' on the petition action page", "Keep me informed about other petitions and activities").EscapeHtml();
            PhraseFieldShowPublicly = translator.Get("Petition.Action.Field.ShowPublicly", "Field 'Show publicly' on the petition action page", "Show my name publicly").EscapeHtml();
            PhraseInfo = translator.Get("Petition.Action.Confirm.Info", "Info text on the petition confirm action page", "Please enter your name and location to sign the petition.").EscapeHtml();
            PhraseButtonConfirm = translator.Get("Petition.Action.Button.Confirm", "Button 'Confirm' on the petition action page", "Sign").EscapeHtml();
        }
    }

    public class PetitionMailContentProvider : IContentProvider
    {
        public string ConfirmationLink { get; private set; }

        public PetitionMailContentProvider(Translator translator, Petition petition, string mailAddress)
        {
            ConfirmationLink = CreateLink(translator, petition, mailAddress);
        }

        public string Prefix
        {
            get { return "Petition"; }
        }

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "Petition.ConfirmationLink":
                    return ConfirmationLink;
                default:
                    throw new NotSupportedException();
            }
        }

        public static string CreateCode(Petition petition, string mailAddress)
        {
            using (var serializer = new Serializer())
            {
                serializer.Write(petition.Id.Value);
                serializer.WritePrefixed(mailAddress);

                using (var hmac = new HMACSHA256(petition.EmailKey.Value))
                {
                    return hmac.ComputeHash(serializer.Data).ToHexString();
                }
            }
        }

        public static string CreateLink(Translator translator, Petition petition, string mailAddress)
        {
            return string.Format("{0}/confirm/{1}/{2}#inputrow",
                petition.WebAddress.Value[translator.Language],
                Convert.ToBase64String(Encoding.UTF8.GetBytes(mailAddress)),
                CreateCode(petition, mailAddress));
        }

        public static string ValidateLink(Petition petition, string encodedMailAddress, string code)
        {
            var mailAddress = Encoding.UTF8.GetString(Convert.FromBase64String(encodedMailAddress));

            if (CreateCode(petition, mailAddress) == code)
            {
                return mailAddress;
            }
            else
            {
                return null; 
            }
        }
    }

    public class SignPetitionModule : PublicusModule
    {
        private Translator CreateTranslator(string langString)
        {
            switch (langString)
            {
                case "fr":
                    return new Translator(Translation, Language.French);
                case "it":
                    return new Translator(Translation, Language.Italian);
                case "en":
                    return new Translator(Translation, Language.English);
                case "de":
                default:
                    return new Translator(Translation, Language.German);
            }
        }

        private bool SendMail(IDatabase database, Translator translator, Petition petition, string mailAddress)
        {
            var mailMessage = CreateMail(database, translator, petition, mailAddress);

            try
            {
                Global.Mail.Send(mailMessage);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static MimeMessage CreateMail(IDatabase database, Translator translator, Petition petition, string mailAddress)
        {
            var group = petition.Group.Value;
            var from = new MailboxAddress(
                group.MailName.Value[translator.Language],
                group.MailAddress.Value[translator.Language]);
            var to = new MailboxAddress(mailAddress);
            var senderKey = string.IsNullOrEmpty(group.GpgKeyId.Value) ? null :
                new GpgPrivateKeyInfo(
                group.GpgKeyId.Value,
                group.GpgKeyPassphrase.Value);
            var mailTemplate = petition.GetConfirmationMail(database, translator.Language);
            var templator = new Templator(new PetitionMailContentProvider(translator, petition, mailAddress));
            var subject = templator.Apply(mailTemplate.Subject.Value);
            var htmlText = templator.Apply(mailTemplate.HtmlText.Value);
            var plainText = templator.Apply(mailTemplate.PlainText.Value);
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainText };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            var htmlPart = new TextPart("html") { Text = htmlText };
            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(htmlPart);
            return Global.Mail.Create(from, to, senderKey, null, subject, alternative);
        }

        public void UpdateTagAssignment(Contact contact, Tag tag, bool value)
        {
            var present = contact.TagAssignments.Any(ta => ta.Tag.Value == tag);

            if (present && !value)
            {
                foreach (var t in contact.TagAssignments.Where(ta => ta.Tag.Value == tag).ToList())
                {
                    t.Delete(Database);
                }
            }
            else if (!present && value)
            {
                var t = new TagAssignment(Guid.NewGuid());
                t.Contact.Value = contact;
                t.Tag.Value = tag;
                Database.Save(t);
            }
        }

        public SignPetitionModule()
        {
            Get("/petition/{id}/{lang}", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);

                if (petition != null)
                {
                    return View["View/petition_sign.sshtml", new PetitionSignViewModel(Database, translator, petition)];
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Petition.Sign.Error.Unknown.Title", "Error title when petition not known", "Not found"),
                        Translate("Petition.Sign.Error.Unknown.Text", "Error text when petition not known", "The requested petition could not be found."),
                        Translate("Petition.Sign.Error.Unknown.BackLink", "Back link when petition not known", "Back"),
                        "/")];
                }
            });
            Post("/petition/{id}/{lang}/mail", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);
                var status = CreateStatus(translator);
                var model = JsonConvert.DeserializeObject<PetitionMailPostViewModel>(ReadBody());

                if (status.ObjectNotNull(petition))
                {
                    if (!string.IsNullOrEmpty(model.Mail))
                    {
                        const string mailPattern = @"^[a-zA-Z0-9\+\.\-_]+@([a-zA-Z0-9\-]{1,63}\.)*[a-zA-Z0-9\-]{2,63}\.[a-zA-Z0-9\-]{2,63}$";

                        if (Regex.IsMatch(model.Mail, mailPattern))
                        {
                            if (!SendMail(Database, translator, petition, model.Mail))
                            {
                                status.SetValidationError(
                                    "Mail",
                                    "Petition.Sign.Validation.Mail.Failed",
                                    "Sending mail error at petition sign validation",
                                    "Could not send e-mail to that address");
                            }
                        }
                        else
                        {
                            status.SetValidationError(
                                "Mail",
                                "Petition.Sign.Validation.Mail.Invalid",
                                "Invalid mail at petition sign validation",
                                "Invalid e-mail address");
                        }
                    }
                    else
                    {
                        status.SetValidationError(
                            "Mail",
                            "Petition.Sign.Validation.Mail.Empty",
                            "Empty mail at petition sign validation",
                            "Please fill");
                    }
                }

                return status.CreateJsonData();
            });
            Get("/petition/{id}/{lang}/mail", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);

                if (petition != null)
                {
                    return View["View/petition_info.sshtml",
                        new PetitionInfoViewModel(Database, translator, petition,
                        translator.Get("Petition.Action.Mail.Info", "Info text on the petition action page", "You will get a mail with a confirmation link shortly. Please check your inbox.").EscapeHtml(),
                        "primary", false)];
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Petition.Sign.Error.Unknown.Title", "Error title when petition not known", "Not found"),
                        Translate("Petition.Sign.Error.Unknown.Text", "Error text when petition not known", "The requested petition could not be found."),
                        Translate("Petition.Sign.Error.Unknown.BackLink", "Back link when petition not known", "Back"),
                        "/")];
                }
            });
            Get("/petition/{id}/{lang}/confirm/{mail}/{code}", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);
                string encodedMailAddress = parameters.mail;
                string code = parameters.code;

                if (petition != null)
                {
                    var mailAddress = PetitionMailContentProvider.ValidateLink(petition, encodedMailAddress, code);

                    if (!string.IsNullOrEmpty(mailAddress))
                    {
                        return View["View/petition_confirm.sshtml",
                            new PetitionConfirmViewModel(Database, translator, petition, encodedMailAddress, code)];
                    }
                    else
                    {
                        return View["View/info.sshtml", new InfoViewModel(Translator,
                            Translate("Petition.Sign.Error.Invalid.Title", "Error title when confirmation link is not valid", "Invalid link"),
                            Translate("Petition.Sign.Error.Invalid.Text", "Error text when confirmation link is not valid", "The confirmation link is not valid."),
                            Translate("Petition.Sign.Error.Invalid.BackLink", "Back link when confirmation link is not valid", "Back"),
                            "/")];
                    }
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Petition.Sign.Error.Unknown.Title", "Error title when petition not known", "Not found"),
                        Translate("Petition.Sign.Error.Unknown.Text", "Error text when petition not known", "The requested petition could not be found."),
                        Translate("Petition.Sign.Error.Unknown.BackLink", "Back link when petition not known", "Back"),
                        "/")];
                }
            });
            Post("/petition/{id}/{lang}/confirm/{mail}/{code}", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);
                string encodedMailAddress = parameters.mail;
                string code = parameters.code;
                var status = CreateStatus(translator);
                var model = JsonConvert.DeserializeObject<PetitionConfirmViewModel>(ReadBody());

                if (status.ObjectNotNull(petition))
                {
                    var mailAddress = PetitionMailContentProvider.ValidateLink(petition, encodedMailAddress, code);

                    if (mailAddress != null)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            var oldAddress = Database.Query<ServiceAddress>(
                                DC.Equal("address", mailAddress)
                                .And(DC.Equal("service", (int)ServiceType.EMail)))
                                .FirstOrDefault();

                            Contact contact = null;

                            if (oldAddress == null)
                            {
                                contact = new Contact(Guid.NewGuid());
                                contact.Language.Value = translator.Language;
                                contact.ExpiryDate.Value = model.GeneralNewsletter ?
                                    DateTime.UtcNow.AddYears(3) : DateTime.UtcNow.AddYears(1);
                                status.AssignStringRequired("FirstName", contact.FirstName, model.FirstName);
                                status.AssignStringRequired("LastName", contact.LastName, model.LastName);
                                Database.Save(contact);

                                var subscription = new Subscription(Guid.NewGuid());
                                subscription.Contact.Value = contact;
                                subscription.Feed.Value = petition.Group.Value.Feed.Value;
                                subscription.StartDate.Value = DateTime.UtcNow;
                                Database.Save(subscription);

                                var postalAddress = new PostalAddress(Guid.NewGuid());
                                postalAddress.Contact.Value = contact;
                                postalAddress.Country.Value = Database
                                    .Query<Country>()
                                    .FirstOrDefault(c => c.Name.Value[Language.German] == "Schweiz");
                                status.AssignStringRequired("Place", postalAddress.Place, model.Place);
                                status.AssignStringRequired("PostalCode", postalAddress.PostalCode, model.PostalCode);
                                Database.Save(postalAddress);

                                var serviceAddress = new ServiceAddress(Guid.NewGuid());
                                serviceAddress.Contact.Value = contact;
                                serviceAddress.Service.Value = ServiceType.EMail;
                                serviceAddress.Address.Value = mailAddress;
                                Database.Save(serviceAddress);

                                Journal(contact.FullName,
                                        contact,
                                        "Petition.Sign.Journal.Created",
                                        "Journal entry when user is created by signing a petition",
                                        "Joined by signing petition {0}",
                                        l => petition.Label.Value[l.Language]);
                            }
                            else
                            {
                                contact = oldAddress.Contact.Value;
                                if (contact.ExpiryDate.Value.HasValue)
                                {
                                    var newExpiryDate = model.GeneralNewsletter ?
                                        DateTime.UtcNow.AddYears(3) : DateTime.UtcNow.AddYears(1);
                                    if (contact.ExpiryDate.Value.Value < newExpiryDate)
                                    {
                                        contact.ExpiryDate.Value = newExpiryDate;
                                    }
                                }
                                status.AssignStringRequired("FirstName", contact.FirstName, model.FirstName);
                                status.AssignStringRequired("LastName", contact.LastName, model.LastName);
                                Database.Save(contact);

                                var postalAddress = contact.PostalAddresses
                                    .FirstOrDefault();
                                if (postalAddress != null)
                                {
                                    status.AssignStringRequired("Place", postalAddress.Place, model.Place);
                                    status.AssignStringRequired("PostalCode", postalAddress.PostalCode, model.PostalCode);
                                }
                                else
                                {
                                    postalAddress = new PostalAddress(Guid.NewGuid());
                                    postalAddress.Contact.Value = contact;
                                    status.AssignStringRequired("Place", postalAddress.Place, model.Place);
                                    status.AssignStringRequired("PostalCode", postalAddress.PostalCode, model.PostalCode);
                                    Database.Save(postalAddress);
                                }
                                Database.Save(postalAddress);

                                var subscription = contact.Subscriptions
                                    .FirstOrDefault(s => s.Feed.Value == petition.Group.Value.Feed.Value);
                                if (subscription == null)
                                {
                                    subscription = new Subscription(Guid.NewGuid());
                                    subscription.Contact.Value = contact;
                                    subscription.Feed.Value = petition.Group.Value.Feed.Value;
                                    subscription.StartDate.Value = DateTime.UtcNow;
                                    Database.Save(subscription);
                                }

                                Journal(contact.FullName,
                                        contact,
                                        "Petition.Sign.Journal.Signed",
                                        "Journal entry when user signed a petition",
                                        "Signed petition {0}",
                                        l => petition.Label.Value[l.Language]);
                            }

                            if (status.IsSuccess)
                            {
                                UpdateTagAssignment(contact, petition.PetitionTag, true);
                                UpdateTagAssignment(contact, petition.SpecialNewsletterTag, model.SpecialNewsletter);
                                UpdateTagAssignment(contact, petition.GeneralNewsletterTag, model.GeneralNewsletter);
                                UpdateTagAssignment(contact, petition.ShowPubliclyTag, model.ShowPublicly);
                            }

                            if (status.IsSuccess)
                            {
                                transaction.Commit();

                                LastActivity.Instance.Add(Database, petition, Translation, contact.FullName, model.Place, model.ShowPublicly);
                            }
                        }
                    }
                    else
                    {
                        status.SetError("Petition.Sign.Error.Invalid.Text", "Error text when confirmation link is not valid", "The confirmation link is not valid.");
                    }
                }

                return status.CreateJsonData();
            });
            Get("/petition/{id}/{lang}/thanks", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);

                if (petition != null)
                {
                    return View["View/petition_info.sshtml",
                        new PetitionInfoViewModel(Database, translator, petition,
                        translator.Get("Petition.Action.Thanks.Info", "Info text on the petition action thanks page", "Thank you for joining our petition!").EscapeHtml(),
                        "success", true)];
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Petition.Sign.Error.Unknown.Title", "Error title when petition not known", "Not found"),
                        Translate("Petition.Sign.Error.Unknown.Text", "Error text when petition not known", "The requested petition could not be found."),
                        Translate("Petition.Sign.Error.Unknown.BackLink", "Back link when petition not known", "Back"),
                        "/")];
                }
            });
            Get("/petition/{id}/{lang}/lastactivity/{index}", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);
                string indexString = parameters.index;

                if (petition != null && long.TryParse(indexString, out long index))
                {
                    var current = LastActivity.Instance.GetCurrent(Database, petition, translator, index);
                    var json = new JObject(
                        new JProperty("index", current.Item1),
                        new JProperty("text", current.Item2));
                    return json.ToString();
                }
                else
                {
                    var json = new JObject(
                        new JProperty("index", 0),
                        new JProperty("text", string.Empty));
                    return json.ToString();
                }
            });
            Get("/petition/{id}/{lang}/privacy", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);

                if (petition != null)
                {
                    return View["View/petition_page.sshtml",
                        new PetitionPageViewModel(Database, translator, petition, petition.Privacy.Value)];
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Petition.Sign.Error.Unknown.Title", "Error title when petition not known", "Not found"),
                        Translate("Petition.Sign.Error.Unknown.Text", "Error text when petition not known", "The requested petition could not be found."),
                        Translate("Petition.Sign.Error.Unknown.BackLink", "Back link when petition not known", "Back"),
                        "/")];
                }
            });
            Get("/petition/{id}/{lang}/faq", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);

                if (petition != null)
                {
                    return View["View/petition_page.sshtml",
                        new PetitionPageViewModel(Database, translator, petition, petition.Faq.Value)];
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Petition.Sign.Error.Unknown.Title", "Error title when petition not known", "Not found"),
                        Translate("Petition.Sign.Error.Unknown.Text", "Error text when petition not known", "The requested petition could not be found."),
                        Translate("Petition.Sign.Error.Unknown.BackLink", "Back link when petition not known", "Back"),
                        "/")];
                }
            });
            Get("/petition/{id}/{lang}/imprint", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);
                string langString = parameters.lang;
                var translator = CreateTranslator(langString);

                if (petition != null)
                {
                    return View["View/petition_page.sshtml",
                        new PetitionPageViewModel(Database, translator, petition, petition.Imprint.Value)];
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Petition.Sign.Error.Unknown.Title", "Error title when petition not known", "Not found"),
                        Translate("Petition.Sign.Error.Unknown.Text", "Error text when petition not known", "The requested petition could not be found."),
                        Translate("Petition.Sign.Error.Unknown.BackLink", "Back link when petition not known", "Back"),
                        "/")];
                }
            });
        }
    }
}

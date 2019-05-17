using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Publicus
{
    public class MailingSendingViewModel
    {
        public string Name;
        public string Address;
        public string Status;

        public MailingSendingViewModel(Translator translator, Sending sending)
        {
            Name = sending.Address.Value.Contact.Value.ShortHand;
            Address = sending.Address.Value.Address.Value;

            var sentDate =
                sending.SentDate.Value.HasValue ?
                sending.SentDate.Value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") :
                string.Empty;

            switch (sending.Status.Value)
            {
                case SendingStatus.Created:
                    Status = translator.Get("Mailing.Sending.Field.Status.Sending", "Sending value in the sending status field the scheduled mailing page", "Sending").EscapeHtml();
                    break;
                case SendingStatus.Sent:
                    Status = translator.Get("Mailing.Sending.Field.Status.Sent", "Sent value in the sending status field the scheduled mailing page", "Sent at {0}", sentDate).EscapeHtml();
                    break;
                case SendingStatus.Failed:
                    Status = translator.Get("Mailing.Sending.Field.Status.Failed", "Failed value in the sending status field the scheduled mailing page", "Failed at {0} width message '{1}'", sentDate, sending.FailureMessage.Value ?? string.Empty).EscapeHtml();
                    break;
                default:
                    throw new NotSupportedException(); 
            } 
        }
    }

    public class MailingScheduledViewModel : MasterViewModel
    {
        public string PhraseFieldTitle;
        public string PhraseFieldRecipients;
        public string PhraseFieldSubject;
        public string PhraseFieldStatus;
        public string PhraseButtonAbort;
        public string PhraseButtonBack;
        public string PhraseFieldSendingName;
        public string PhraseFieldSendingAddress;
        public string PhraseFieldSendingStatus;

        public string Id;
        public string Recipients;
        public string Subject;
        public string Status;
        public bool Cancelable;

        public List<MailingSendingViewModel> Sendings;

        private MailingScheduledViewModel(Translator translator, Session session)
            : base(translator,
                   translator.Get("Mailing.Scheduled.Title", "Title of the scheduled mailing page", "Scheduled mailing"),
                   session)
        {
            PhraseFieldTitle = translator.Get("Mailing.Scheduled.Field.Title", "Title field the scheduled mailing page", "Title").EscapeHtml();
            PhraseFieldRecipients = translator.Get("Mailing.Scheduled.Field.Recipients", "Recipients field the scheduled mailing page", "Recipients").EscapeHtml();
            PhraseFieldSubject = translator.Get("Mailing.Scheduled.Field.Subject", "Subject field the scheduled mailing page", "Subject").EscapeHtml();
            PhraseFieldStatus = translator.Get("Mailing.Scheduled.Field.Status", "Status field the scheduled mailing page", "Status").EscapeHtml();
            PhraseButtonAbort = translator.Get("Mailing.Scheduled.Button.Abort", "Abort button the scheduled mailing page", "Abort").EscapeHtml();
            PhraseButtonBack = translator.Get("Mailing.Scheduled.Button.Back", "Back button the scheduled mailing page", "Back").EscapeHtml();
            PhraseFieldSendingName = translator.Get("Mailing.Sending.Field.Name", "Sending name field the scheduled mailing page", "Name").EscapeHtml();
            PhraseFieldSendingAddress = translator.Get("Mailing.Sending.Field.Address", "Sending address field the scheduled mailing page", "Address").EscapeHtml();
            PhraseFieldSendingStatus = translator.Get("Mailing.Sending.Field.Status", "Sending status field the scheduled mailing page", "Status").EscapeHtml();
        }

        public MailingScheduledViewModel(Translator translator, IDatabase db, Session session, Mailing mailing)
            : this(translator, session)
        {
            Id = mailing.Id.Value.ToString();
            Title = mailing.Title.Value.EscapeHtml();
            Recipients = mailing.RecipientFeed.Value.Name.Value[translator.Language];

            if (mailing.RecipientTag.Value != null)
            {
                Recipients = " / " + mailing.RecipientTag.Value.Name.Value[translator.Language];
            }

            if (mailing.RecipientLanguage.Value.HasValue)
            {
                Recipients = " / " + mailing.RecipientLanguage.Value.Value.Translate(translator);
            }

            var number = db
                .Query<Contact>()
                .Count(p => p.ActiveSubscriptions.Any(m => m.Feed == mailing.RecipientFeed.Value) &&
                      (mailing.RecipientTag.Value == null || p.TagAssignments.Any(t => t.Tag == mailing.RecipientTag.Value)));

            Recipients +=  " / " + translator.Get("Mailing.Scheduled.Fields.Recipients.Contacts", "Contacts in the recipients field on the scheduled mailing page", "circa {0} contacts", number);

            Subject = mailing.Subject.Value.EscapeHtml();

            var sendingDate =
                mailing.SendingDate.Value.HasValue ?
                mailing.SendingDate.Value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") :
                string.Empty;
            var sentDate =
                mailing.SentDate.Value.HasValue ?
                mailing.SentDate.Value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm") :
                string.Empty;

            switch (mailing.Status.Value)
            {
                case MailingStatus.Scheduled:
                    Status = translator.Get("Mailing.Scheduled.Field.Status.Scheduled", "Scheduled status in the status field the scheduled mailing page", "Scheduled for {0}", sendingDate).EscapeHtml();
                    break;
                case MailingStatus.Sending:
                    Status = translator.Get("Mailing.Scheduled.Field.Status.Sending", "Sending status in the status field the scheduled mailing page", "Sending since {0}", sendingDate).EscapeHtml();
                    break;
                case MailingStatus.Sent:
                    Status = translator.Get("Mailing.Scheduled.Field.Status.Sent", "Sent status in the status field the scheduled mailing page", "Sent at {0}", sentDate).EscapeHtml();
                    break;
                case MailingStatus.Canceled:
                    Status = translator.Get("Mailing.Scheduled.Field.Status.Canceled", "Canceled status in the status field the scheduled mailing page", "Canceled").EscapeHtml();
                    break;
                default:
                    throw new NotSupportedException();
            }

            Sendings = new List<MailingSendingViewModel>(db
                .Query<Sending>(DC.Equal("mailingid", mailing.Id.Value))
                .Select(s => new MailingSendingViewModel(translator, s)));

            Cancelable = mailing.Status.Value == MailingStatus.Scheduled ||
                         mailing.Status.Value == MailingStatus.Sending;
        }
    }

    public class MailingSendViewModel : MasterViewModel
    {
        public string PhraseFieldTitle;
        public string PhraseFieldRecipientFeed;
        public string PhraseFieldRecipientTag;
        public string PhraseFieldRecipientNumber;
        public string PhraseFieldSubject;
        public string PhraseFieldDate;
        public string PhraseFieldTime;
        public string PhraseButtonSend;
        public string PhraseButtonCancel;

        public string Id;
        public string RecipientFeed;
        public string RecipientTag;
        public string RecipientNumber;
        public string Subject;
        public string Date;
        public string Time;

        public MailingSendViewModel()
        { 
        }

        private MailingSendViewModel(Translator translator, Session session)
            : base(translator,
                   translator.Get("Mailing.Edit.Title", "Title of the edit mailing page", "Edit mailing"),
                   session)
        {
            PhraseFieldTitle = translator.Get("Mailing.Edit.Field.Title", "Title field in the edit mailing page", "Title").EscapeHtml();
            PhraseFieldRecipientFeed = translator.Get("Mailing.Edit.Field.RecipientFeed", "Recipient feed field  in the edit mailing page", "To feed").EscapeHtml();
            PhraseFieldRecipientTag = translator.Get("Mailing.Edit.Field.RecipientTag", "Recipient tag field in the edit mailing page", "To tag").EscapeHtml();
            PhraseFieldRecipientNumber = translator.Get("Mailing.Edit.Field.RecipientNumber", "Number of recipients field in the edit mailing page", "Number of recipients").EscapeHtml();
            PhraseFieldSubject = translator.Get("Mailing.Edit.Field.Subject", "Subject field in the edit mailing page", "Subject").EscapeHtml();
            PhraseFieldDate = translator.Get("Mailing.Edit.Field.Date", "Date field in the edit mailing page", "Date").EscapeHtml();
            PhraseFieldTime = translator.Get("Mailing.Edit.Field.Time", "Time field in the edit mailing page", "Time").EscapeHtml();
            PhraseButtonCancel = translator.Get("Mailing.Edit.Button.Cancel", "Cancel button the edit mailing page", "Cancel").EscapeHtml();
            PhraseButtonSend = translator.Get("Mailing.Edit.Button.Send", "Send button the edit mailing page", "Send").EscapeHtml();
        }

        public MailingSendViewModel(Translator translator, IDatabase db, Session session, Mailing mailing)
            : this(translator, session)
        {
            Id = mailing.Id.Value.ToString();
            Title = mailing.Title.Value.EscapeHtml();
            RecipientFeed = mailing.RecipientFeed.Value.Name.Value[translator.Language];

            if (mailing.RecipientTag.Value != null)
            {
                RecipientTag = mailing.RecipientTag.Value.Name.Value[translator.Language];
            }
            else
            {
                RecipientTag = translator.Get("Mailing.Edit.Field.RecipientTags.None", "No selection in the recipient tag field of the edit mailing page", "None");
            }

            RecipientNumber = "~" + db
                .Query<Contact>()
                .Where(p => p.ActiveSubscriptions.Any(m => m.Feed == mailing.RecipientFeed.Value) &&
                      (mailing.RecipientTag.Value == null || p.TagAssignments.Any(t => t.Tag == mailing.RecipientTag.Value)))
                .Count().ToString();

            Subject = mailing.Subject.Value.EscapeHtml();
        }
    }

    public class MailingEditViewModel : MasterViewModel
    {
        public string PhraseFieldTitle;
        public string PhraseFieldRecipientFeed;
        public string PhraseFieldRecipientTag;
        public string PhraseFieldRecipientLanguage;
        public string PhraseFieldSenderGroup;
        public string PhraseFieldHeader;
        public string PhraseFieldFooter;
        public string PhraseFieldSubject;
        public string PhraseFieldHtmlText;
        public string PhraseFieldTestAddress;
        public string PhraseButtonSendTest;
        public string PhraseButtonCancel;
        public string PhraseButtonSaveClose;
        public string PhraseButtonSaveSend;
        public string PhraseButtonOk;

        public List<NamedIdViewModel> RecipientFeeds;
        public List<NamedIdViewModel> RecipientTags;
        public List<NamedIntViewModel> RecipientLanguages;
        public List<NamedIdViewModel> SenderGroups;
        public List<NamedIdViewModel> Headers;
        public List<NamedIdViewModel> Footers;

        public string Id;
        public string Method;
        public string RecipientFeed;
        public string RecipientTag;
        public string RecipientLanguage;
        public string SenderGroup;
        public string Header;
        public string Footer;
        public string Subject;
        public string HtmlText;
        public string TestAddress;

        public MailingEditViewModel()
        { 
        }

        private MailingEditViewModel(Translator translator, Session session)
            : base(translator,
                   translator.Get("Mailing.Edit.Title", "Title of the edit mailing page", "Edit mailing"),
                   session)
        {
            PhraseFieldTitle = translator.Get("Mailing.Edit.Field.Title", "Title field in the edit mailing page", "Title").EscapeHtml();
            PhraseFieldRecipientFeed = translator.Get("Mailing.Edit.Field.RecipientFeed", "Recipient feed field  in the edit mailing page", "To feed").EscapeHtml();
            PhraseFieldRecipientTag = translator.Get("Mailing.Edit.Field.RecipientTag", "Recipient tag field in the edit mailing page", "To tag").EscapeHtml();
            PhraseFieldRecipientLanguage = translator.Get("Mailing.Edit.Field.RecipientLanguage", "Recipient language field in the edit mailing page", "To language").EscapeHtml();
            PhraseFieldSenderGroup = translator.Get("Mailing.Edit.Field.SenderGroup", "Sender group field in the edit mailing page", "Sender group").EscapeHtml();
            PhraseFieldHeader = translator.Get("Mailing.Edit.Field.Header", "Header field in the edit mailing page", "Header").EscapeHtml();
            PhraseFieldFooter = translator.Get("Mailing.Edit.Field.Footer", "Footer field in the edit mailing page", "Footer").EscapeHtml();
            PhraseFieldSubject = translator.Get("Mailing.Edit.Field.Subject", "Subject field in the edit mailing page", "Subject").EscapeHtml();
            PhraseFieldHtmlText = translator.Get("Mailing.Edit.Field.HtmlText", "Text field the edit mailing page", "Text").EscapeHtml();
            PhraseFieldTestAddress = translator.Get("Mailing.Edit.Field.TestAddress", "Test address field the edit mailing page", "E-Mail address").EscapeHtml();
            PhraseButtonSendTest = translator.Get("Mailing.Edit.Button.SendTest", "Send test button the edit mailing page", "Send test mail").EscapeHtml();
            PhraseButtonCancel = translator.Get("Mailing.Edit.Button.Cancel", "Cancel button the edit mailing page", "Cancel").EscapeHtml();
            PhraseButtonOk = translator.Get("Mailing.Edit.Button.Ok", "Ok button the edit mailing page", "OK").EscapeHtml();
            PhraseButtonSaveClose = translator.Get("Mailing.Edit.Button.SaveClose", "Save and close button the edit mailing page", "Save and close").EscapeHtml();
            PhraseButtonSaveSend = translator.Get("Mailing.Edit.Button.SaveSend", "Save and continue button the edit mailing page", "Save and continue").EscapeHtml();
        }

        public MailingEditViewModel(Translator translator, IDatabase db, Session session)
            : this(translator, session)
        {
            Method = "add";
            Id = Guid.NewGuid().ToString();
            Title = string.Empty;
            Subject = string.Empty;
            HtmlText = string.Empty;
            RecipientFeeds = new List<NamedIdViewModel>(db
                .Query<Feed>()
                .Where(o => session.HasAccess(o, PartAccess.Mailings, AccessRight.Write))
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
            RecipientFeeds.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.RecipientFeeds.None", "No selection in the recipient feed field of the edit mailing page", "Select option"), true, true));
            RecipientTags = new List<NamedIdViewModel>(db
                .Query<Tag>()
                .Where(t => t.Usage.Value.HasFlag(TagUsage.Mailing))
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            RecipientTags.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.RecipientTags.None", "No selection in the recipient tag field of the edit mailing page", "None"), false, true));
            RecipientLanguages = new List<NamedIntViewModel>();
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.English, false));
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.German, false));
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.French, false));
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.Italian, false));
            RecipientLanguages.Add(new NamedIntViewModel(translator.Get("Mailing.Edit.Field.RecipientLanguage.None", "No selection in the recipient language field of the edit mailing page", "None"), false, true));
            SenderGroups = new List<NamedIdViewModel>(db
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Mailings, AccessRight.Write))
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(g => g.Name));
            SenderGroups.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.SenderGroups.None", "No selection in the sender group field of the edit mailing page", "Select option"), true, true));
            Headers = new List<NamedIdViewModel>(db
                .Query<MailingElement>()
                .Where(e => e.Type.Value == MailingElementType.Header)
                .Select(e => new NamedIdViewModel(e, false))
                .OrderBy(e => e.Name));
            Headers.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.Headers.None", "No selection in the header field of the edit mailing page", "None"), false, true));
            Footers = new List<NamedIdViewModel>(db
                .Query<MailingElement>()
                .Where(e => e.Type.Value == MailingElementType.Footer)
                .Select(e => new NamedIdViewModel(e, false))
                .OrderBy(e => e.Name));
            Footers.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.Footers.None", "No selection in the footer field of the edit mailing page", "None"), false, true));
        }

        public MailingEditViewModel(Translator translator, IDatabase db, Session session, Mailing mailing)
        : this(translator, session)
        {
            Method = "edit";
            Id = mailing.Id.Value.ToString();
            Title = mailing.Title.Value.EscapeHtml();
            Subject = mailing.Subject.Value.EscapeHtml();
            HtmlText = mailing.HtmlText.Value;
            RecipientFeeds = new List<NamedIdViewModel>(db
                .Query<Feed>()
                .Where(o => session.HasAccess(o, PartAccess.Mailings, AccessRight.Write))
                .Select(o => new NamedIdViewModel(translator, o, mailing.RecipientFeed.Value == o))
                .OrderBy(o => o.Name));
            RecipientTags = new List<NamedIdViewModel>(db
                .Query<Tag>()
                .Where(t => t.Usage.Value.HasFlag(TagUsage.Mailing))
                .Select(t => new NamedIdViewModel(translator, t, mailing.RecipientTag.Value == t))
                .OrderBy(t => t.Name));
            RecipientTags.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.RecipientTags.None", "No selection in the recipient tag field of the edit mailing page", "None"), false, mailing.RecipientTag.Value == null));
            RecipientLanguages = new List<NamedIntViewModel>();
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.English, mailing.RecipientLanguage.Value == Language.English));
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.German, mailing.RecipientLanguage.Value == Language.German));
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.French, mailing.RecipientLanguage.Value == Language.French));
            RecipientLanguages.Add(new NamedIntViewModel(translator, Language.Italian, mailing.RecipientLanguage.Value == Language.Italian));
            RecipientLanguages.Add(new NamedIntViewModel(translator.Get("Mailing.Edit.Field.RecipientLanguage.None", "No selection in the recipient language field of the edit mailing page", "None"), false, mailing.RecipientLanguage.Value == null));
            SenderGroups = new List<NamedIdViewModel>(db
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Mailings, AccessRight.Write))
                .Select(g => new NamedIdViewModel(translator, g, mailing.Sender.Value == g))
                .OrderBy(g => g.Name));
            Headers = new List<NamedIdViewModel>(db
                .Query<MailingElement>()
                .Where(e => e.Type.Value == MailingElementType.Header)
                .Select(e => new NamedIdViewModel(e, mailing.Header.Value == e))
                .OrderBy(e => e.Name));
            Headers.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.Headers.None", "No selection in the header field of the edit mailing page", "None"), false, mailing.Header.Value == null));
            Footers = new List<NamedIdViewModel>(db
                .Query<MailingElement>()
                .Where(e => e.Type.Value == MailingElementType.Footer)
                .Select(e => new NamedIdViewModel(e, mailing.Footer.Value == e))
                .OrderBy(e => e.Name));
            Footers.Add(new NamedIdViewModel(translator.Get("Mailing.Edit.Field.Footers.None", "No selection in the footer field of the edit mailing page", "None"), false, mailing.Footer.Value == null));
        }
    }

    public class MailingViewModel : MasterViewModel
    {
        public MailingViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("Mailing.List.Title", "Title of the mailing list page", "Mailing"), 
            session)
        {
        }
    }

    public class MailingListItemViewModel
    {
        public string Id;
        public string Title;
        public string Feed;
        public string Status;
        public string Creator;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public MailingListItemViewModel(Translator translator, Session session, Mailing mailing)
        {
            Title = mailing.Title.Value.EscapeHtml();
            Feed = mailing.RecipientFeed.Value.Name.Value[translator.Language].EscapeHtml();
            Creator = mailing.Creator.Value.UserName.Value.EscapeHtml();

            switch (mailing.Status.Value)
            {
                case MailingStatus.Scheduled:
                    if (mailing.SendingDate.Value.HasValue)
                    {
                        var dateString = mailing.SendingDate.Value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                        Status = translator.Get("Mailing.List.Status.ScheduledFor", "Status 'Scheduled for' in the mailing list page", "Scheduled for {0}", dateString).EscapeHtml();
                    }
                    else
                    {
                        Status = mailing.Status.Value.Translate(translator).EscapeHtml();
                    }
                    break;
                case MailingStatus.Sending:
                    if (mailing.SendingDate.Value.HasValue)
                    {
                        var dateString = mailing.SendingDate.Value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                        Status = translator.Get("Mailing.List.Status.SendingSince", "Status 'Sending since' in the mailing list page", "Sending since {0}", dateString).EscapeHtml();
                    }
                    else
                    {
                        Status = mailing.Status.Value.Translate(translator).EscapeHtml();
                    }
                    break;
                case MailingStatus.Sent:
                    if (mailing.SentDate.Value.HasValue)
                    {
                        var dateString = mailing.SentDate.Value.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                        Status = translator.Get("Mailing.List.Status.SentAt", "Status 'Sent at' in the mailing list page", "Sent at {0}", dateString).EscapeHtml();
                    }
                    else
                    {
                        Status = mailing.Status.Value.Translate(translator).EscapeHtml();
                    }
                    break;
                default:
                    Status = mailing.Status.Value.Translate(translator).EscapeHtml();
                    break;
            }

            Id = mailing.Id.Value.ToString();
            bool access = session.HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write);
            Editable = access ? "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Mailing.List.Delete.Confirm.Question", "Delete mailing confirmation question", "Do you really wish to delete mailing {0}?", mailing.GetText(translator)).EscapeHtml();
        }
    }

    public class MailingListViewModel
    {
        public string PhraseHeaderTitle;
        public string PhraseHeaderFeed;
        public string PhraseHeaderStatus;
        public string PhraseHeaderCreator;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<MailingListItemViewModel> List;

        public MailingListViewModel(Translator translator, IDatabase database, Session session)
        {
            PhraseHeaderTitle = translator.Get("Mailing.List.Header.Title", "Column 'Title' in the mailing list", "Title").EscapeHtml();
            PhraseHeaderFeed = translator.Get("Mailing.List.Header.Feed", "Column 'Feed' in the mailing list", "Feed").EscapeHtml();
            PhraseHeaderStatus = translator.Get("Mailing.List.Header.Status", "Column 'Status' in the mailing list", "Status").EscapeHtml();
            PhraseHeaderCreator = translator.Get("Mailing.List.Header.Creator", "Column 'Creator' in the mailing list", "Creator").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Mailing.List.Delete.Confirm.Title", "Delete mailing confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
            List = new List<MailingListItemViewModel>(database
                .Query<Mailing>()
                .Where(m => session.HasAccess(m.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Read))
                .OrderByDescending(m => m.CreatedDate.Value)
                .Select(m => new MailingListItemViewModel(translator, session, m)));
        }
    }

    public class MailingModule : PublicusModule
    {
        private string HtmlPage(string content)
        {
            return string.Format("<html><body>{0}</body></html>", content);
        }

        public MailingModule()
        {
            this.RequiresAuthentication();

            Get("/mailing", parameters =>
            {
                return View["View/mailing.sshtml",
                    new MailingViewModel(Translator, CurrentSession)];
            });
            Get("/mailing/list", parameters =>
            {
                return View["View/mailinglist.sshtml",
                    new MailingListViewModel(Translator, Database, CurrentSession)];
            });
            Get("/mailing/add", parameters =>
            {
                return View["View/mailingedit.sshtml",
                    new MailingEditViewModel(Translator, Database, CurrentSession)];
            });
            Post("/mailing/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MailingEditViewModel>(ReadBody());
                var status = CreateStatus();

                if (Guid.TryParse(idString, out Guid id))
                {
                    var mailing = new Mailing(id);
                    status.AssignStringRequired("Title", mailing.Title, model.Title);
                    status.AssignObjectIdString("RecipientFeed", mailing.RecipientFeed, model.RecipientFeed);
                    status.AssignObjectIdString("RecipientTag", mailing.RecipientTag, model.RecipientTag);
                    status.AssignEnumIntString("RecipientLanguage", mailing.RecipientLanguage, model.RecipientLanguage);
                    status.AssignObjectIdString("SenderGroup", mailing.Sender, model.SenderGroup);
                    status.AssignObjectIdString("Header", mailing.Header, model.Header);
                    status.AssignObjectIdString("Footer", mailing.Footer, model.Footer);
                    status.AssignStringRequired("Subject", mailing.Subject, model.Subject);
                    var worker = new HtmlWorker(model.HtmlText);
                    mailing.HtmlText.Value = worker.CleanHtml;
                    mailing.PlainText.Value = worker.PlainText;
                    mailing.Creator.Value = CurrentSession.User;

                    if (status.IsSuccess)
                    {
                        if (status.HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write) &&
                            (mailing.Sender.Value == null || status.HasAccess(mailing.Sender.Value, PartAccess.Mailings, AccessRight.Write)))
                        {
                            Database.Save(mailing);
                            Notice("{0} added mailing {1}", CurrentSession.User.UserName.Value, mailing);
                        }
                    }
                }
                else
                {
                    status.SetErrorNotFound(); 
                }

                return status.CreateJsonData();
            });
            Get("/mailing/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailing = Database.Query<Mailing>(idString);

                if (mailing != null)
                {
                    if (mailing.Status.Value == MailingStatus.New)
                    {
                        return View["View/mailingedit.sshtml",
                            new MailingEditViewModel(Translator, Database, CurrentSession, mailing)];
                    }
                    else
                    {
                        return View["View/mailingscheduled.sshtml",
                            new MailingScheduledViewModel(Translator, Database, CurrentSession, mailing)];
                    }
                }

                return Response.AsRedirect("/mailing");
            });
            Post("/mailing/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MailingEditViewModel>(ReadBody());
                var mailing = Database.Query<Mailing>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(mailing))
                {
                    if (mailing.Status.Value == MailingStatus.New)
                    {
                        status.AssignStringRequired("Title", mailing.Title, model.Title);
                        status.AssignObjectIdString("RecipientFeed", mailing.RecipientFeed, model.RecipientFeed);
                        status.AssignObjectIdString("RecipientTag", mailing.RecipientTag, model.RecipientTag);
                        status.AssignEnumIntString("RecipientLanguage", mailing.RecipientLanguage, model.RecipientLanguage);
                        status.AssignObjectIdString("SenderGroup", mailing.Sender, model.SenderGroup);
                        status.AssignObjectIdString("Header", mailing.Header, model.Header);
                        status.AssignObjectIdString("Footer", mailing.Footer, model.Footer);
                        status.AssignStringRequired("Subject", mailing.Subject, model.Subject);
                        var worker = new HtmlWorker(model.HtmlText);
                        mailing.HtmlText.Value = worker.CleanHtml;
                        mailing.PlainText.Value = worker.PlainText;
                        mailing.Creator.Value = CurrentSession.User;

                        if (status.IsSuccess)
                        {
                            if (status.HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write) &&
                                (mailing.Sender.Value == null || status.HasAccess(mailing.Sender.Value, PartAccess.Mailings, AccessRight.Write)))
                            {
                                Database.Save(mailing);
                                Notice("{0} changed mailing {1}", CurrentSession.User.UserName.Value, mailing);
                            }
                        }
                    }
                    else
                    {
                        status.SetErrorAccessDenied(); 
                    }
                }

                return status.CreateJsonData();
            });
            Post("/mailing/test", parameters =>
            {
                var model = JsonConvert.DeserializeObject<MailingEditViewModel>(ReadBody());
                var worker = new HtmlWorker(model.HtmlText);
                var header = Database.Query<MailingElement>(model.Header);
                var footer = Database.Query<MailingElement>(model.Footer);
                var sender = Database.Query<Group>(model.SenderGroup);

                var htmlText = worker.CleanHtml;
                var plainText = worker.PlainText;

                if (header != null)
                {
                    htmlText = HtmlWorker.ConcatHtml(header.HtmlText.Value, htmlText);
                    plainText = header.PlainText.Value + plainText;
                }

                if (footer != null)
                {
                    htmlText = HtmlWorker.ConcatHtml(htmlText, footer.HtmlText.Value);
                    plainText = plainText + footer.PlainText.Value;
                }

                var contact = Database
                    .Query<Contact>()
                    .Where(p => HasAccess(p, PartAccess.Contact, AccessRight.Read))
                    .FirstOrDefault();

                if (contact == null)
                {
                    return PostResult.Failed(
                        Translate("Mailing.Edit.Test.Failed", "Failed message on sending test mail in mailing edit page", "E-Mail could not be sent."));
                }

                var templator = new Templator(new ContactContentProvider(Translator, contact));
                htmlText = templator.Apply(htmlText);
                plainText = templator.Apply(plainText);

                try
                {
                    var language = CurrentSession.User.Language.Value;
                    var from = new MailboxAddress(
                        sender.MailName.Value[language],
                        sender.MailAddress.Value[language]);
                    var to = new MailboxAddress(model.TestAddress);
                    var senderKey = sender.GpgKeyId.Value == null ? null :
                        new GpgPrivateKeyInfo(
                        sender.GpgKeyId.Value,
                        sender.GpgKeyPassphrase.Value);
                    var content = new Multipart("alternative");
                    var textPart = new TextPart("plain") { Text = plainText };
                    textPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                    content.Add(textPart);
                    var htmlPart = new TextPart("html") { Text = htmlText };
                    htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                    content.Add(htmlPart);

                    Global.Mail.Send(from, to, senderKey, null, model.Subject, content);
                    Notice("{0} tests mailing with subject {1}", CurrentSession.User.UserName.Value, model.Subject);

                    return PostResult.Success(
                        Translate("Mailing.Edit.Test.Success", "Success message on sending test mail in mailing edit page", "Test E-Mail sent."));
                }
                catch
                {
                    return PostResult.Failed(
                        Translate("Mailing.Edit.Test.Failed", "Failed message on sending test mail in mailing edit page", "E-Mail could not be sent."));
                }
            });
            Get("/mailing/send/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailing = Database.Query<Mailing>(idString);

                if (mailing != null)
                {
                    if (HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write) &&
                        (mailing.Sender.Value == null || HasAccess(mailing.Sender.Value, PartAccess.Mailings, AccessRight.Write)))
                    {
                        return View["View/mailingsend.sshtml",
                            new MailingSendViewModel(Translator, Database, CurrentSession, mailing)];
                    }
                }

                return AccessDenied();
            });
            Post("/mailing/send/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MailingSendViewModel>(ReadBody());
                var mailing = Database.Query<Mailing>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(mailing))
                {
                    status.AssignDateString("Date", mailing.SendingDate, model.Date, true);
                    status.AddAssignTimeString("Time", mailing.SendingDate, model.Time);

                    if (status.IsSuccess)
                    {
                        if (status.HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write) &&
                            (mailing.Sender.Value == null || status.HasAccess(mailing.Sender.Value, PartAccess.Mailings, AccessRight.Write)))
                        {
                            mailing.SendingDate.Value = mailing.SendingDate.Value.Value.ToUniversalTime();
                            mailing.Status.Value = MailingStatus.Scheduled;
                            Database.Save(mailing);
                            Notice("{0} sent mailing {1}", CurrentSession.User.UserName.Value, mailing);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/mailing/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailing = Database.Query<Mailing>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(mailing))
                {
                    if (status.HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write) &&
                        (mailing.Sender.Value == null || status.HasAccess(mailing.Sender.Value, PartAccess.Mailings, AccessRight.Write)))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            mailing.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted mailing {1}", CurrentSession.User.UserName.Value, mailing);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/mailing/cancel/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailing = Database.Query<Mailing>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(mailing))
                {
                    if (status.HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write) &&
                        (mailing.Sender.Value == null || status.HasAccess(mailing.Sender.Value, PartAccess.Mailings, AccessRight.Write)))
                    {
                        if (mailing.Status.Value == MailingStatus.Scheduled ||
                            mailing.Status.Value == MailingStatus.Sending)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                mailing.Status.Value = MailingStatus.Canceled;
                                Database.Save(mailing);
                                transaction.Commit();
                                Notice("{0} canceled mailing {1}", CurrentSession.User.UserName.Value, mailing);
                            }
                        }
                        else
                        {
                            status.SetErrorAccessDenied(); 
                        }
                    }
                }

                return status.CreateJsonData();
            }); 
            Get("/mailing/copy/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailing = Database.Query<Mailing>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(mailing))
                {
                    if (status.HasAccess(mailing.RecipientFeed.Value, PartAccess.Mailings, AccessRight.Write) &&
                        (mailing.Sender.Value == null || status.HasAccess(mailing.Sender.Value, PartAccess.Mailings, AccessRight.Write)))
                    {
                        var newMailing = new Mailing(Guid.NewGuid());
                        newMailing.Title.Value = mailing.Title.Value +
                            Translate("Mailing.Copy.Postfix", "Postfix of copied mailings", " (Copy)");
                        newMailing.RecipientFeed.Value = mailing.RecipientFeed.Value;
                        newMailing.RecipientTag.Value = mailing.RecipientTag.Value;
                        newMailing.RecipientLanguage.Value = mailing.RecipientLanguage.Value;
                        newMailing.Sender.Value = mailing.Sender.Value;
                        newMailing.Header.Value = mailing.Header.Value;
                        newMailing.Footer.Value = mailing.Footer.Value;
                        newMailing.Subject.Value = mailing.Subject.Value;
                        newMailing.HtmlText.Value = mailing.HtmlText.Value;
                        newMailing.PlainText.Value = mailing.PlainText.Value;
                        newMailing.Creator.Value = CurrentSession.User;
                        newMailing.CreatedDate.Value = DateTime.UtcNow;
                        newMailing.Status.Value = MailingStatus.New;
                        Database.Save(newMailing);
                        Notice("{0} copied mailing {1}", CurrentSession.User.UserName.Value, mailing);
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

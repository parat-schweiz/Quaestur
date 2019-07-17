using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class BillSendingTemplateEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public string ParentId;
        public string Language;
        public string Name;
        public string MinReminderLevel;
        public string MaxReminderLevel;
        public string MailSubject;
        public string MailHtmlText;
        public string MailSender;
        public string LetterLatex;
        public string SendingMode;
        public List<NamedIntViewModel> Languages;
        public List<NamedIdViewModel> MailSenders;
        public List<NamedIntViewModel> SendingModes;
        public string PhraseFieldLanguage;
        public string PhraseFieldName;
        public string PhraseFieldMinReminderLevel;
        public string PhraseFieldMaxReminderLevel;
        public string PhraseFieldMailSubject;
        public string PhraseFieldMailHtmlText;
        public string PhraseFieldMailSender;
        public string PhraseFieldLetterLatex;
        public string PhraseFieldSendingMode;
        public string PhraseButtonCancel;
        public string PhraseButtonSave;
        public string HtmlEditorId;

        public BillSendingTemplateEditViewModel()
        { 
        }

        public BillSendingTemplateEditViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("BillSendingTemplate.Edit.Title", "Title of the billSendingTemplate edit dialog", "Edit bill sending template"), 
            session)
        {
            PhraseFieldLanguage = translator.Get("BillSendingTemplate.Edit.Field.Language", "Language field in the bill sending template edit page", "Language").EscapeHtml();
            PhraseFieldName = translator.Get("BillSendingTemplate.Edit.Field.Name", "Name field in the bill emplate edit page", "Name").EscapeHtml();
            PhraseFieldMinReminderLevel = translator.Get("BillSendingTemplate.Edit.Field.MinReminderLevel", "Min reminder level field in the bill template edit page", "Min reminder level").EscapeHtml();
            PhraseFieldMaxReminderLevel = translator.Get("BillSendingTemplate.Edit.Field.MaxReminderLevel", "Max reminder level field in the bill template edit page", "Max reminder level").EscapeHtml();
            PhraseFieldMailSubject = translator.Get("BillSendingTemplate.Edit.Field.MailSubject", "Mail subject field in the bill template edit page", "Mail subject").EscapeHtml();
            PhraseFieldMailHtmlText = translator.Get("BillSendingTemplate.Edit.Field.MailHtmlText", "Mail text field in the bill template edit page", "Mail text").EscapeHtml();
            PhraseFieldMailSender = translator.Get("BillSendingTemplate.Edit.Field.MailSender", "Mail sender field in the bill template edit page", "Mail sender group").EscapeHtml();
            PhraseFieldLetterLatex = translator.Get("BillSendingTemplate.Edit.Field.LetterLatex", "Letter LaTeX field in the bill template edit page", "Letter LaTeX").EscapeHtml();
            PhraseFieldSendingMode = translator.Get("BillSendingTemplate.Edit.Field.SendingMode", "Sending mode field in the bill template edit page", "Sending mode").EscapeHtml();
            PhraseButtonCancel = translator.Get("BillSendingTemplate.Edit.Button.Cancel", "Cancel button in the bill template edit page", "Cancel").EscapeHtml();
            PhraseButtonSave = translator.Get("BillSendingTemplate.Edit.Button.Save", "Save button in the bill template edit page", "Save").EscapeHtml();
            HtmlEditorId = Guid.NewGuid().ToString();
        }

        public BillSendingTemplateEditViewModel(Translator translator, IDatabase db, Session session, MembershipType membershipType)
            : this(translator, session)
        {
            Method = "add";
            Id = membershipType.Id.Value.ToString();
            ParentId = membershipType.Id.Value.ToString();
            Language = string.Empty;
            Name = string.Empty;
            MinReminderLevel = "1";
            MaxReminderLevel = "1";
            MailSubject = string.Empty;
            MailHtmlText = string.Empty;
            MailSender = string.Empty;
            LetterLatex = string.Empty;
            SendingMode = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, false));
            MailSenders = new List<NamedIdViewModel>(membershipType.Organization.Value.Groups
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(g => g.Name));
            SendingModes = new List<NamedIntViewModel>();
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailOnly, false));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalOnly, false));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailPreferred, false));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalPrefrerred, false));
        }

        public BillSendingTemplateEditViewModel(Translator translator, IDatabase db, Session session, BillSendingTemplate billSendingTemplate)
            : this(translator, session)
        {
            Method = "edit";
            Id = billSendingTemplate.Id.ToString();
            ParentId = billSendingTemplate.MembershipType.Value.Id.Value.ToString();
            Language = string.Empty;
            Name = billSendingTemplate.Name.Value.EscapeHtml();
            MinReminderLevel = billSendingTemplate.MinReminderLevel.Value.ToString();
            MaxReminderLevel = billSendingTemplate.MaxReminderLevel.Value.ToString();
            MailSubject = billSendingTemplate.MailSubject.Value;
            MailHtmlText = billSendingTemplate.MailHtmlText.Value;
            MailSender = string.Empty;
            LetterLatex = billSendingTemplate.LetterLatex.Value;
            SendingMode = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, billSendingTemplate.Language.Value == SiteLibrary.Language.English));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, billSendingTemplate.Language.Value == SiteLibrary.Language.German));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, billSendingTemplate.Language.Value == SiteLibrary.Language.French));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, billSendingTemplate.Language.Value == SiteLibrary.Language.Italian));
            MailSenders = new List<NamedIdViewModel>(billSendingTemplate.MembershipType.Value.Organization.Value.Groups
                .Select(g => new NamedIdViewModel(translator, g, billSendingTemplate.MailSender.Value == g))
                .OrderBy(g => g.Name));
            SendingModes = new List<NamedIntViewModel>();
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailOnly, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.MailOnly));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalOnly, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.PostalOnly));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailPreferred, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.MailPreferred));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalPrefrerred, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.PostalPrefrerred));
        }
    }

    public class BillSendingTemplateViewModel : MasterViewModel
    {
        public string Id;

        public BillSendingTemplateViewModel(Translator translator, Session session, MembershipType membershipType)
            : base(translator, 
            translator.Get("BillSendingTemplate.List.Title", "Title of the bill sending template list page", "bill sending templates"), 
            session)
        {
            Id = membershipType.Id.Value.ToString();
        }
    }

    public class BillSendingTemplateListItemViewModel
    {
        public string Id;
        public string Language;
        public string Name;
        public string ReminderLevel;
        public string PhraseDeleteConfirmationQuestion;

        public BillSendingTemplateListItemViewModel(Translator translator, Session session, BillSendingTemplate billSendingTemplate)
        {
            Id = billSendingTemplate.Id.Value.ToString();
            Language = billSendingTemplate.Language.Value.Translate(translator);
            Name = billSendingTemplate.Name.Value.EscapeHtml();
            if (billSendingTemplate.MinReminderLevel.Value == billSendingTemplate.MaxReminderLevel.Value)
            {
                ReminderLevel = billSendingTemplate.MinReminderLevel.Value.ToString();
            }
            else
            {
                ReminderLevel = string.Format("{0}-{1}", billSendingTemplate.MinReminderLevel.Value, billSendingTemplate.MaxReminderLevel.Value);
            }
            PhraseDeleteConfirmationQuestion = translator.Get("BillSendingTemplate.List.Delete.Confirm.Question", "Delete bill sending template confirmation question", "Do you really wish to delete bill sending template {0}?", billSendingTemplate.GetText(translator));
        }
    }

    public class BillSendingTemplateListViewModel
    {
        public string OrganizationId;
        public string Id;
        public string Name;
        public string PhraseHeaderLanguage;
        public string PhraseHeaderName;
        public string PhraseHeaderReminderLevel;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<BillSendingTemplateListItemViewModel> List;
        public bool AddAccess;

        public BillSendingTemplateListViewModel(Translator translator, IDatabase database, Session session, MembershipType membershipType)
        {
            OrganizationId = membershipType.Organization.Value.Id.Value.ToString();
            Id = membershipType.Id.Value.ToString();
            Name = membershipType.Organization.Value.Name.Value[translator.Language].EscapeHtml() + " / " + membershipType.Name.Value[translator.Language].EscapeHtml();
            PhraseHeaderLanguage = translator.Get("BillSendingTemplate.List.Header.Language", "Header part 'Language' in the billSendingTemplate list", "Language").EscapeHtml();
            PhraseHeaderName = translator.Get("BillSendingTemplate.List.Header.Name", "Link 'Name' caption in the billSendingTemplate list", "Name").EscapeHtml();
            PhraseHeaderReminderLevel = translator.Get("BillSendingTemplate.List.Header.ReminderLevel", "Link 'ReminderLevel' caption in the billSendingTemplate list", "ReminderLevel").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("BillSendingTemplate.List.Delete.Confirm.Title", "Delete bill sending template confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("BillSendingTemplate.List.Delete.Confirm.Info", "Delete bill sending template confirmation info", "This might lead to bills not being sent if no template is available.").EscapeHtml();
            List = new List<BillSendingTemplateListItemViewModel>(database
                .Query<BillSendingTemplate>(DC.Equal("membershiptypeid", membershipType.Id.Value))
                .Where(bt => session.HasAccess(bt.MembershipType.Value.Organization.Value, PartAccess.Structure, AccessRight.Read))
                .Select(bt => new BillSendingTemplateListItemViewModel(translator, session, bt))
                .OrderBy(bt => bt.Name));
        }
    }

    public class BillSendingTemplateEdit : QuaesturModule
    {
        public BillSendingTemplateEdit()
        {
            RequireCompleteLogin();

            Get("/billsendingtemplate/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);

                if (membershipType != null)
                {
                    if (HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/billsendingtemplate.sshtml",
                            new BillSendingTemplateViewModel(Translator, CurrentSession, membershipType)];
                    }
                }

                return string.Empty;
            });
            Get("/billsendingtemplate/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);

                if (membershipType != null)
                {
                    if (HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/billsendingtemplatelist.sshtml",
                            new BillSendingTemplateListViewModel(Translator, Database, CurrentSession, membershipType)];
                    }
                }

                return string.Empty;
            });
            Get("/billsendingtemplate/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var billSendingTemplate = Database.Query<BillSendingTemplate>(idString);

                if (billSendingTemplate != null)
                {
                    if (HasAccess(billSendingTemplate.MembershipType.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/billsendingtemplateedit.sshtml",
                            new BillSendingTemplateEditViewModel(Translator, Database, CurrentSession, billSendingTemplate)];
                    }
                }

                return string.Empty;
            });
            Get("/billsendingtemplate/copy/{id}", parameters =>
            {
                string idString = parameters.id;
                var billSendingTemplate = Database.Query<BillSendingTemplate>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(billSendingTemplate))
                {
                    if (status.HasAccess(billSendingTemplate.MembershipType.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        var newTemplate = new BillSendingTemplate(Guid.NewGuid());
                        newTemplate.MembershipType.Value = billSendingTemplate.MembershipType.Value;
                        newTemplate.Name.Value = billSendingTemplate.Name.Value +=
                            Translate("BillSendingTemplate.Copy.NameSuffix", "Suffix on copyied bill sending template", " (Copy)");
                        newTemplate.Language.Value = billSendingTemplate.Language.Value;
                        newTemplate.LetterLatex.Value = billSendingTemplate.LetterLatex.Value;
                        newTemplate.MailHtmlText.Value = billSendingTemplate.MailHtmlText.Value;
                        newTemplate.MailPlainText.Value = billSendingTemplate.MailPlainText.Value;
                        newTemplate.MailSender.Value = billSendingTemplate.MailSender.Value;
                        newTemplate.MailSubject.Value = billSendingTemplate.MailSubject.Value;
                        newTemplate.MaxReminderLevel.Value = billSendingTemplate.MaxReminderLevel.Value;
                        newTemplate.MinReminderLevel.Value = billSendingTemplate.MinReminderLevel.Value;
                        newTemplate.SendingMode.Value = billSendingTemplate.SendingMode.Value;
                        Database.Save(newTemplate);
                    }
                }

                return status.CreateJsonData();
            });
            Post("/billsendingtemplate/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<BillSendingTemplateEditViewModel>(ReadBody());
                var billSendingTemplate = Database.Query<BillSendingTemplate>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(billSendingTemplate))
                {
                    if (status.HasAccess(billSendingTemplate.MembershipType.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignEnumIntString("Language", billSendingTemplate.Language, model.Language);
                        status.AssignStringRequired("Name", billSendingTemplate.Name, model.Name);
                        status.AssignInt32String("MinReminderLevel", billSendingTemplate.MinReminderLevel, model.MinReminderLevel);
                        status.AssignInt32String("MaxReminderLevel", billSendingTemplate.MaxReminderLevel, model.MaxReminderLevel);
                        status.AssignStringRequired("MailSubject", billSendingTemplate.MailSubject, model.MailSubject);
                        status.AssignObjectIdString("MailSender", billSendingTemplate.MailSender, model.MailSender);
                        status.AssignStringRequired("LetterLatex", billSendingTemplate.LetterLatex, model.LetterLatex);
                        status.AssignEnumIntString("SendingMode", billSendingTemplate.SendingMode, model.SendingMode);
                        var worker = new HtmlWorker(model.MailHtmlText);
                        billSendingTemplate.MailHtmlText.Value = worker.CleanHtml;
                        billSendingTemplate.MailPlainText.Value = worker.PlainText;

                        if (status.IsSuccess)
                        {
                            Database.Save(billSendingTemplate);
                            Notice("{0} changed bill sending template {1}", CurrentSession.User.ShortHand, billSendingTemplate);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/billsendingtemplate/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);

                if (membershipType != null)
                {
                    if (HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/billsendingtemplateedit.sshtml",
                            new BillSendingTemplateEditViewModel(Translator, Database, CurrentSession, membershipType)];
                    }
                }

                return string.Empty;
            });
            Post("/billsendingtemplate/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membershipType))
                {
                    if (status.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<BillSendingTemplateEditViewModel>(ReadBody());
                        var billSendingTemplate = new BillSendingTemplate(Guid.NewGuid());
                        status.AssignEnumIntString("Language", billSendingTemplate.Language, model.Language);
                        status.AssignStringRequired("Name", billSendingTemplate.Name, model.Name);
                        status.AssignInt32String("MinReminderLevel", billSendingTemplate.MinReminderLevel, model.MinReminderLevel);
                        status.AssignInt32String("MaxReminderLevel", billSendingTemplate.MaxReminderLevel, model.MaxReminderLevel);
                        status.AssignStringRequired("MailSubject", billSendingTemplate.MailSubject, model.MailSubject);
                        status.AssignObjectIdString("MailSender", billSendingTemplate.MailSender, model.MailSender);
                        status.AssignStringRequired("LetterLatex", billSendingTemplate.LetterLatex, model.LetterLatex);
                        status.AssignEnumIntString("SendingMode", billSendingTemplate.SendingMode, model.SendingMode);
                        var worker = new HtmlWorker(model.MailHtmlText);
                        billSendingTemplate.MailHtmlText.Value = worker.CleanHtml;
                        billSendingTemplate.MailPlainText.Value = worker.PlainText;

                        billSendingTemplate.MembershipType.Value = membershipType;

                        if (status.IsSuccess)
                        {
                            Database.Save(billSendingTemplate);
                            Notice("{0} added bill sending template {1}", CurrentSession.User.ShortHand, billSendingTemplate);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/billsendingtemplate/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var billSendingTemplate = Database.Query<BillSendingTemplate>(idString);

                if (billSendingTemplate != null)
                {
                    if (HasAccess(billSendingTemplate.MembershipType.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            billSendingTemplate.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted bill sending template {1}", CurrentSession.User.ShortHand, billSendingTemplate);
                        }
                    }
                }

                return string.Empty;
            });
        }
    }
}

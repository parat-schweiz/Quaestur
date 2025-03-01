﻿using System;
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
        public string Name;
        public string MinReminderLevel;
        public string MaxReminderLevel;
        public List<NamedIdViewModel> BillSendingMails;
        public string[] BillSendingMailTemplates;
        public List<NamedIdViewModel> BillSendingLetters;
        public string[] BillSendingLetterTemplates;
        public string SendingMode;
        public List<NamedIdViewModel> MailSenders;
        public List<NamedIntViewModel> SendingModes;
        public string PhraseFieldName;
        public string PhraseFieldMinReminderLevel;
        public string PhraseFieldMaxReminderLevel;
        public string PhraseFieldBillSendingMailTemplates;
        public string PhraseFieldBillSendingLetterTemplates;
        public string PhraseFieldMailSender;
        public string PhraseFieldSendingMode;
        public string PhraseButtonCancel;
        public string PhraseButtonSave;
        public string HtmlEditorId;

        public BillSendingTemplateEditViewModel()
        { 
        }

        public BillSendingTemplateEditViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, 
            translator.Get("BillSendingTemplate.Edit.Title", "Title of the billSendingTemplate edit dialog", "Edit bill sending template"), 
            session)
        {
            PhraseFieldName = translator.Get("BillSendingTemplate.Edit.Field.Name", "Name field in the bill emplate edit page", "Name").EscapeHtml();
            PhraseFieldMinReminderLevel = translator.Get("BillSendingTemplate.Edit.Field.MinReminderLevel", "Min reminder level field in the bill template edit page", "Min reminder level").EscapeHtml();
            PhraseFieldMaxReminderLevel = translator.Get("BillSendingTemplate.Edit.Field.MaxReminderLevel", "Max reminder level field in the bill template edit page", "Max reminder level").EscapeHtml();
            PhraseFieldBillSendingMailTemplates = translator.Get("BillSendingTemplate.Edit.Field.BillSendingMailTemplates", "Sending mail templates field in the bill template edit page", "Sending mails templates").EscapeHtml();
            PhraseFieldBillSendingLetterTemplates = translator.Get("BillSendingTemplate.Edit.Field.BillSendingLetterTemplates", "Sending letter templates field in the bill template edit page", "Sending letters templates").EscapeHtml();
            PhraseFieldMailSender = translator.Get("BillSendingTemplate.Edit.Field.MailSender", "Mail sender field in the bill template edit page", "Mail sender group").EscapeHtml();
            PhraseFieldSendingMode = translator.Get("BillSendingTemplate.Edit.Field.SendingMode", "Sending mode field in the bill template edit page", "Sending mode").EscapeHtml();
            PhraseButtonCancel = translator.Get("BillSendingTemplate.Edit.Button.Cancel", "Cancel button in the bill template edit page", "Cancel").EscapeHtml();
            PhraseButtonSave = translator.Get("BillSendingTemplate.Edit.Button.Save", "Save button in the bill template edit page", "Save").EscapeHtml();
            HtmlEditorId = Guid.NewGuid().ToString();
        }

        public BillSendingTemplateEditViewModel(Translator translator, IDatabase database, Session session, MembershipType membershipType)
            : this(database, translator, session)
        {
            Method = "add";
            Id = membershipType.Id.Value.ToString();
            ParentId = membershipType.Id.Value.ToString();
            Name = string.Empty;
            MinReminderLevel = "1";
            MaxReminderLevel = "1";
            SendingMode = string.Empty;
            MailSenders = new List<NamedIdViewModel>(membershipType.Organization.Value.Groups
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(g => g.Name));
            SendingModes = new List<NamedIntViewModel>();
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailOnly, false));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalOnly, false));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailPreferred, false));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalPrefrerred, false));
            BillSendingLetters = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.BillSendingTemplate)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            BillSendingMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.BillSendingTemplate)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
        }

        public BillSendingTemplateEditViewModel(Translator translator, IDatabase database, Session session, BillSendingTemplate billSendingTemplate)
            : this(database, translator, session)
        {
            Method = "edit";
            Id = billSendingTemplate.Id.ToString();
            ParentId = billSendingTemplate.MembershipType.Value.Id.Value.ToString();
            Name = billSendingTemplate.Name.Value.EscapeHtml();
            MinReminderLevel = billSendingTemplate.MinReminderLevel.Value.ToString();
            MaxReminderLevel = billSendingTemplate.MaxReminderLevel.Value.ToString();
            SendingMode = string.Empty;
            MailSenders = new List<NamedIdViewModel>(billSendingTemplate.MembershipType.Value.Organization.Value.Groups
                .Select(g => new NamedIdViewModel(translator, g, billSendingTemplate.MailSender.Value == g))
                .OrderBy(g => g.Name));
            SendingModes = new List<NamedIntViewModel>();
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailOnly, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.MailOnly));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalOnly, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.PostalOnly));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.MailPreferred, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.MailPreferred));
            SendingModes.Add(new NamedIntViewModel(translator, Quaestur.SendingMode.PostalPrefrerred, billSendingTemplate.SendingMode.Value == Quaestur.SendingMode.PostalPrefrerred));
            BillSendingLetters = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == billSendingTemplate.MembershipType.Value.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.BillSendingTemplate)
                .Select(t => new NamedIdViewModel(translator, t, billSendingTemplate.BillSendingLetters.Values(database).Contains(t)))
                .OrderBy(t => t.Name));
            BillSendingMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == billSendingTemplate.MembershipType.Value.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.BillSendingTemplate)
                .Select(t => new NamedIdViewModel(translator, t, billSendingTemplate.BillSendingMails.Values(database).Contains(t)))
                .OrderBy(t => t.Name));
        }
    }

    public class BillSendingTemplateViewModel : MasterViewModel
    {
        public string Id;

        public BillSendingTemplateViewModel(IDatabase database, Translator translator, Session session, MembershipType membershipType)
            : base(database, translator, 
            translator.Get("BillSendingTemplate.List.Title", "Title of the bill sending template list page", "bill sending templates"), 
            session)
        {
            Id = membershipType.Id.Value.ToString();
        }
    }

    public class BillSendingTemplateListItemViewModel
    {
        public string Id;
        public string Name;
        public string ReminderLevel;
        public string PhraseDeleteConfirmationQuestion;

        public BillSendingTemplateListItemViewModel(Translator translator, Session session, BillSendingTemplate billSendingTemplate)
        {
            Id = billSendingTemplate.Id.Value.ToString();
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
                            new BillSendingTemplateViewModel(Database, Translator, CurrentSession, membershipType)];
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
                        newTemplate.MailSender.Value = billSendingTemplate.MailSender.Value;
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
                        status.AssignStringRequired("Name", billSendingTemplate.Name, model.Name);
                        status.AssignInt32String("MinReminderLevel", billSendingTemplate.MinReminderLevel, model.MinReminderLevel);
                        status.AssignInt32String("MaxReminderLevel", billSendingTemplate.MaxReminderLevel, model.MaxReminderLevel);
                        status.AssignEnumIntString("SendingMode", billSendingTemplate.SendingMode, model.SendingMode);

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(billSendingTemplate);
                                status.UpdateTemplates(Database, billSendingTemplate.BillSendingLetters, model.BillSendingLetterTemplates);
                                status.UpdateTemplates(Database, billSendingTemplate.BillSendingMails, model.BillSendingMailTemplates);
                                transaction.Commit();
                                Notice("{0} changed bill sending template {1}", CurrentSession.User.ShortHand, billSendingTemplate);
                            }
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
                        billSendingTemplate.Language.Value = Language.English;
                        status.AssignStringRequired("Name", billSendingTemplate.Name, model.Name);
                        status.AssignInt32String("MinReminderLevel", billSendingTemplate.MinReminderLevel, model.MinReminderLevel);
                        status.AssignInt32String("MaxReminderLevel", billSendingTemplate.MaxReminderLevel, model.MaxReminderLevel);
                        status.AssignEnumIntString("SendingMode", billSendingTemplate.SendingMode, model.SendingMode);

                        billSendingTemplate.MembershipType.Value = membershipType;

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(billSendingTemplate);
                                status.UpdateTemplates(Database, billSendingTemplate.BillSendingLetters, model.BillSendingLetterTemplates);
                                status.UpdateTemplates(Database, billSendingTemplate.BillSendingMails, model.BillSendingMailTemplates);
                                transaction.Commit();
                                Notice("{0} added bill sending template {1}", CurrentSession.User.ShortHand, billSendingTemplate);
                            }
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

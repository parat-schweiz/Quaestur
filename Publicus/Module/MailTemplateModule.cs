using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Publicus
{
    public class MailTemplateEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public string Label;
        public string Language;
        public List<NamedIntViewModel> Languages;
        public string AssignmentType;
        public List<NamedIntViewModel> AssignmentTypes;
        public string Organization;
        public List<NamedIdViewModel> Organizations;
        public string Subject;
        public string HtmlText;
        public string HtmlEditorId;
        public string PhraseFieldLabel;
        public string PhraseFieldLanguage;
        public string PhraseFieldAssignmentType;
        public string PhraseFieldOrganization;
        public string PhraseFieldSubject;
        public string PhraseFieldHtmlText;
        public string PhraseButtonSave;
        public string PhraseButtonCancel;

        public MailTemplateEditViewModel()
        { 
        }

        public MailTemplateEditViewModel(Translator translator, Session session)
            : base(translator, translator.Get("MailTemplate.Edit.Title", "Title of the mail template edit page", "Edit mail template"), session)
        {
            PhraseFieldLabel = translator.Get("MailTemplate.Edit.Field.Label", "Label field in the mail template edit page", "Label");
            PhraseFieldLanguage = translator.Get("MailTemplate.Edit.Field.Language", "Language field in the mail template edit page", "Language");
            PhraseFieldAssignmentType = translator.Get("MailTemplate.Edit.Field.AssignmentType", "Assignment type field in the latex template edit page", "Assignment type");
            PhraseFieldOrganization = translator.Get("MailTemplate.Edit.Field.Organization", "Organization field in the latex template edit page", "Organization");
            PhraseFieldSubject = translator.Get("MailTemplate.Edit.Field.Subject", "Subject field in the mail template edit page", "Subject");
            PhraseFieldHtmlText = translator.Get("MailTemplate.Edit.Field.HtmlText", "Text field in the mail template edit page", "Text");
            PhraseButtonSave = translator.Get("MailTemplate.Edit.Button.Save", "Save button in the mail template edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("MailTemplate.Edit.Button.Cancel", "Cancel button in the mail template edit page", "Cancel").EscapeHtml();
            HtmlEditorId = Guid.NewGuid().ToString();
        }

        public MailTemplateEditViewModel(IDatabase database, Translator translator, Session session)
            : this(translator, session)
        {
            Method = "add";
            Id = "new";
            Label = string.Empty;
            Subject = string.Empty;
            HtmlText = string.Empty;
            Language = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, false));
            AssignmentTypes = new List<NamedIntViewModel>();
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.Petition, false));
            Organizations = new List<NamedIdViewModel>(database
                .Query<Feed>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
        }

        public MailTemplateEditViewModel(IDatabase database, Translator translator, Session session, MailTemplate mailTemplate)
            : this(translator, session)
        {
            Method = "edit";
            Id = mailTemplate.Id.ToString();
            Label = mailTemplate.Label.Value.EscapeHtml();
            Subject = mailTemplate.Subject.Value.EscapeHtml();
            HtmlText = mailTemplate.HtmlText.Value.SafeHtml();
            Language = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, mailTemplate.Language.Value == SiteLibrary.Language.English));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, mailTemplate.Language.Value == SiteLibrary.Language.German));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, mailTemplate.Language.Value == SiteLibrary.Language.French));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, mailTemplate.Language.Value == SiteLibrary.Language.Italian));
            AssignmentTypes = new List<NamedIntViewModel>();
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.Petition, TemplateAssignmentType.Petition == mailTemplate.AssignmentType.Value));
            Organizations = new List<NamedIdViewModel>(database
                .Query<Feed>()
                .Select(o => new NamedIdViewModel(translator, o, mailTemplate.Feed.Value == o))
                .OrderBy(o => o.Name));
        }
    }

    public class MailTemplateViewModel : MasterViewModel
    {
        public MailTemplateViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("MailTemplate.List.Title", "Title of the mail template list page", "Countries"), 
            session)
        { 
        }
    }

    public class MailTemplateListItemViewModel
    {
        public string Id;
        public string Label;
        public string Assigned;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public MailTemplateListItemViewModel(IDatabase database, Session session, Translator translator, MailTemplate mailTemplate)
        {
            Id = mailTemplate.Id.Value.ToString();
            Label = mailTemplate.Label.Value;
            Assigned = string.Join(", ", mailTemplate.Assignments(database).Select(a => a.GetText(database, translator)));
            Editable = session.HasAccess(mailTemplate.Feed.Value, mailTemplate.AssignmentType.Value.AccessPart(), AccessRight.Write) ? "editable" : string.Empty;
            PhraseDeleteConfirmationQuestion = translator.Get("MailTemplate.List.Delete.Confirm.Question", "Delete mail template confirmation question", "Do you really wish to delete mail template {0}?", mailTemplate.GetText(translator));
        }
    }

    public class MailTemplateListViewModel
    {
        public string PhraseHeaderLabel;
        public string PhraseHeaderAssigned;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<MailTemplateListItemViewModel> List;

        public MailTemplateListViewModel(IDatabase database, Translator translator, Session session)
        {
            PhraseHeaderLabel = translator.Get("MailTemplate.List.Header.Label", "Column 'Label' in the mail template list", "Label").EscapeHtml();
            PhraseHeaderAssigned = translator.Get("MailTemplate.List.Header.Assigned", "Column 'Assigned' in the mail template list", "Assigned").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("MailTemplate.List.Delete.Confirm.Title", "Delete mailTemplate confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("MailTemplate.List.Delete.Confirm.Info", "Delete mailTemplate confirmation info", "This will also delete all assignments of this mail template.").EscapeHtml();
            List = new List<MailTemplateListItemViewModel>(
                database.Query<MailTemplate>()
                .Where(t => session.HasAccess(t.Feed.Value, t.AssignmentType.Value.AccessPart(), AccessRight.Read))
                .OrderBy(t => t.Label.Value)
                .Select(c => new MailTemplateListItemViewModel(database, session, translator, c)));
        }
    }

    public class MailTemplateEdit : PublicusModule
    {
        public MailTemplateEdit()
        {
            this.RequiresAuthentication();

            Get("/mailtemplate", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/mailtemplate.sshtml",
                        new MailTemplateViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/mailtemplate/list", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/mailtemplatelist.sshtml",
                        new MailTemplateListViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/mailtemplate/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailTemplate = Database.Query<MailTemplate>(idString);

                if (mailTemplate != null)
                {
                    if (HasAccess(mailTemplate.Feed.Value,
                                  mailTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        return View["View/mailtemplateedit.sshtml",
                            new MailTemplateEditViewModel(Database, Translator, CurrentSession, mailTemplate)];
                    }
                }

                return string.Empty;
            });
            Post("/mailtemplate/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MailTemplateEditViewModel>(ReadBody());
                var mailTemplate = Database.Query<MailTemplate>(idString);

                if (status.ObjectNotNull(mailTemplate))
                {
                    if (HasAccess(mailTemplate.Feed.Value,
                                  mailTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        status.AssignStringRequired("Label", mailTemplate.Label, model.Label);
                        status.AssignEnumIntString("Language", mailTemplate.Language, model.Language);
                        status.AssignEnumIntString("AssignmentType", mailTemplate.AssignmentType, model.AssignmentType);
                        status.AssignObjectIdString("Feed", mailTemplate.Feed, model.Organization);
                        status.AssignStringRequired("Subject", mailTemplate.Subject, model.Subject);
                        var worker = new HtmlText(model.HtmlText);
                        mailTemplate.HtmlText.Value = worker.CleanHtml;
                        mailTemplate.PlainText.Value = worker.PlainText;

                        var usedInAssignment = Database
                            .Query<LatexTemplateAssignment>(DC.Equal("templateid", mailTemplate.Id.Value))
                            .FirstOrDefault();

                        if (usedInAssignment != null &&
                            usedInAssignment.AssignedType.Value != mailTemplate.AssignmentType.Value)
                        {
                            status.SetValidationError("AssignmentType", "MailTemplate.Validation.Error.AssignmentType", "Assignment type changed in used latex template", "Change not allowed in used template");
                        }

                        if (usedInAssignment != null &&
                            usedInAssignment.GetFeed(Database) != mailTemplate.Feed.Value)
                        {
                            status.SetValidationError("Organization", "MailTemplate.Validation.Error.Organization", "Organization changed in used latex template", "Change not allowed in used template");
                        }

                        if (status.IsSuccess)
                        {
                            if (status.HasAccess(mailTemplate.Feed.Value,
                                                 mailTemplate.AssignmentType.Value.AccessPart(),
                                                 AccessRight.Write))
                            {
                                Database.Save(mailTemplate);
                                Notice("{0} changed mail template {1}", CurrentSession.User.UserName.Value, mailTemplate);
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
            Get("/mailtemplate/add", parameters =>
            {
                if (SomeAccess(AccessRight.Write))
                {
                    return View["View/mailtemplateedit.sshtml",
                        new MailTemplateEditViewModel(Database, Translator, CurrentSession)];
                }
                return string.Empty;
            });
            Post("/mailtemplate/add/new", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MailTemplateEditViewModel>(ReadBody());
                var mailTemplate = new MailTemplate(Guid.NewGuid());
                status.AssignStringRequired("Label", mailTemplate.Label, model.Label);
                status.AssignEnumIntString("Language", mailTemplate.Language, model.Language);
                status.AssignEnumIntString("AssignmentType", mailTemplate.AssignmentType, model.AssignmentType);
                status.AssignObjectIdString("Feed", mailTemplate.Feed, model.Organization);
                status.AssignStringRequired("Subject", mailTemplate.Subject, model.Subject);
                var worker = new HtmlText(model.HtmlText);
                mailTemplate.HtmlText.Value = worker.CleanHtml;
                mailTemplate.PlainText.Value = worker.PlainText;

                if (status.IsSuccess)
                {
                    if (status.HasAccess(mailTemplate.Feed.Value,
                                         mailTemplate.AssignmentType.Value.AccessPart(),
                                         AccessRight.Write))
                    {
                        Database.Save(mailTemplate);
                        Notice("{0} added mail template {1}", CurrentSession.User.UserName.Value, mailTemplate);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/mailtemplate/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailTemplate = Database.Query<MailTemplate>(idString);

                if (mailTemplate != null)
                {
                    if (HasAccess(mailTemplate.Feed.Value,
                                  mailTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            mailTemplate.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted mail template {1}", CurrentSession.User.UserName.Value, mailTemplate.GetText(Translator));
                        }
                    }
                }

                return string.Empty;
            });
            Get("/mailtemplate/copy/{id}", parameters =>
            {
                string idString = parameters.id;
                var mailTemplate = Database.Query<MailTemplate>(idString);

                if (mailTemplate != null)
                {
                    if (HasAccess(mailTemplate.Feed.Value,
                                  mailTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        var newTemplate = new MailTemplate(Guid.NewGuid());
                        newTemplate.Feed.Value = mailTemplate.Feed.Value;
                        newTemplate.AssignmentType.Value = mailTemplate.AssignmentType.Value;
                        newTemplate.Language.Value = mailTemplate.Language.Value;
                        newTemplate.Label.Value = mailTemplate.Label.Value +
                            Translate("MailTemplate.Copy.Postfix", "Postfix of copied mailings", " (Copy)");
                        newTemplate.Subject.Value = mailTemplate.Subject.Value;
                        newTemplate.HtmlText.Value = mailTemplate.HtmlText.Value;
                        newTemplate.PlainText.Value = mailTemplate.PlainText.Value;
                        Database.Save(newTemplate);
                        Notice("{0} added mail template {1}", CurrentSession.User.UserName.Value, newTemplate.GetText(Translator));
                    }
                }

                return string.Empty;
            });
        }

        public bool SomeAccess(AccessRight right)
        {
            return HasAnyFeedAccess(PartAccess.Petition, right);
        }
    }
}

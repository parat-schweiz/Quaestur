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
    public class PageTemplateEditViewModel : MasterViewModel
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
        public string TitleText;
        public string HtmlText;
        public string HtmlEditorId;
        public string PhraseFieldLabel;
        public string PhraseFieldLanguage;
        public string PhraseFieldAssignmentType;
        public string PhraseFieldOrganization;
        public string PhraseFieldTitleText;
        public string PhraseFieldHtmlText;
        public string PhraseButtonSave;
        public string PhraseButtonCancel;

        public PageTemplateEditViewModel()
        { 
        }

        public PageTemplateEditViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, translator.Get("PageTemplate.Edit.Title", "Title of the mail template edit page", "Edit mail template"), session)
        {
            PhraseFieldLabel = translator.Get("PageTemplate.Edit.Field.Label", "Label field in the mail template edit page", "Label");
            PhraseFieldLanguage = translator.Get("PageTemplate.Edit.Field.Language", "Language field in the mail template edit page", "Language");
            PhraseFieldAssignmentType = translator.Get("PageTemplate.Edit.Field.AssignmentType", "Assignment type field in the latex template edit page", "Assignment type");
            PhraseFieldOrganization = translator.Get("PageTemplate.Edit.Field.Organization", "Organization field in the latex template edit page", "Organization");
            PhraseFieldTitleText = translator.Get("PageTemplate.Edit.Field.Title", "Title field in the mail template edit page", "Title");
            PhraseFieldHtmlText = translator.Get("PageTemplate.Edit.Field.HtmlText", "Text field in the mail template edit page", "Text");
            PhraseButtonSave = translator.Get("PageTemplate.Edit.Button.Save", "Save button in the mail template edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("PageTemplate.Edit.Button.Cancel", "Cancel button in the mail template edit page", "Cancel").EscapeHtml();
            HtmlEditorId = Guid.NewGuid().ToString();
            Method = "add";

            Id = "new";
            Label = string.Empty;
            TitleText = string.Empty;
            HtmlText = string.Empty;
            Language = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, false));
            AssignmentTypes = new List<NamedIntViewModel>();
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.BallotTemplate, false));
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.MembershipType, false));
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.BillSendingTemplate, false));
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.Subscription, false));
            Organizations = new List<NamedIdViewModel>(database
                .Query<Organization>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
        }

        public PageTemplateEditViewModel(IDatabase database, Translator translator, Session session, PageTemplate pageTemplate)
            : this(database, translator, session)
        {
            Method = "edit";
            Id = pageTemplate.Id.ToString();
            Label = pageTemplate.Label.Value.EscapeHtml();
            TitleText = pageTemplate.Title.Value.EscapeHtml();
            HtmlText = pageTemplate.HtmlText.Value.SafeHtml();
            Language = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, pageTemplate.Language.Value == SiteLibrary.Language.English));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, pageTemplate.Language.Value == SiteLibrary.Language.German));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, pageTemplate.Language.Value == SiteLibrary.Language.French));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, pageTemplate.Language.Value == SiteLibrary.Language.Italian));
            AssignmentTypes = new List<NamedIntViewModel>();
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.BallotTemplate, TemplateAssignmentType.BallotTemplate == pageTemplate.AssignmentType.Value));
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.MembershipType, TemplateAssignmentType.MembershipType == pageTemplate.AssignmentType.Value));
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.BillSendingTemplate, TemplateAssignmentType.BillSendingTemplate == pageTemplate.AssignmentType.Value));
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.Subscription, TemplateAssignmentType.Subscription == pageTemplate.AssignmentType.Value));
            Organizations = new List<NamedIdViewModel>(database
                .Query<Organization>()
                .Select(o => new NamedIdViewModel(translator, o, pageTemplate.Organization.Value == o))
                .OrderBy(o => o.Name));
        }
    }

    public class PageTemplateViewModel : MasterViewModel
    {
        public PageTemplateViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, 
            translator.Get("PageTemplate.List.Title", "Title of the mail template list page", "Countries"), 
            session)
        { 
        }
    }

    public class PageTemplateListItemViewModel
    {
        public string Id;
        public string Label;
        public string Assigned;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public PageTemplateListItemViewModel(IDatabase database, Session session, Translator translator, PageTemplate pageTemplate)
        {
            Id = pageTemplate.Id.Value.ToString();
            Label = pageTemplate.Label.Value;
            Assigned = string.Join(", ", pageTemplate.Assignments(database).Select(a => a.GetText(database, translator)));
            Editable = session.HasAccess(pageTemplate.Organization.Value, pageTemplate.AssignmentType.Value.AccessPart(), AccessRight.Write) ? "editable" : string.Empty;
            PhraseDeleteConfirmationQuestion = translator.Get("PageTemplate.List.Delete.Confirm.Question", "Delete mail template confirmation question", "Do you really wish to delete mail template {0}?", pageTemplate.GetText(translator));
        }
    }

    public class PageTemplateListViewModel
    {
        public string PhraseHeaderLabel;
        public string PhraseHeaderAssigned;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<PageTemplateListItemViewModel> List;

        public PageTemplateListViewModel(IDatabase database, Translator translator, Session session)
        {
            PhraseHeaderLabel = translator.Get("PageTemplate.List.Header.Label", "Column 'Label' in the mail template list", "Label").EscapeHtml();
            PhraseHeaderAssigned = translator.Get("PageTemplate.List.Header.Assigned", "Column 'Assigned' in the mail template list", "Assigned").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("PageTemplate.List.Delete.Confirm.Title", "Delete pageTemplate confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("PageTemplate.List.Delete.Confirm.Info", "Delete pageTemplate confirmation info", "This will also delete all assignments of this mail template.").EscapeHtml();
            List = new List<PageTemplateListItemViewModel>(
                database.Query<PageTemplate>()
                .Where(t => session.HasAccess(t.Organization.Value, t.AssignmentType.Value.AccessPart(), AccessRight.Read))
                .OrderBy(t => t.Label.Value)
                .Select(c => new PageTemplateListItemViewModel(database, session, translator, c)));
        }
    }

    public class PageTemplateEdit : QuaesturModule
    {
        public PageTemplateEdit()
        {
            RequireCompleteLogin();

            Get("/pagetemplate", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/pagetemplate.sshtml",
                        new PageTemplateViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/pagetemplate/list", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/pagetemplatelist.sshtml",
                        new PageTemplateListViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/pagetemplate/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var pageTemplate = Database.Query<PageTemplate>(idString);

                if (pageTemplate != null)
                {
                    if (HasAccess(pageTemplate.Organization.Value,
                                  pageTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        return View["View/pagetemplateedit.sshtml",
                            new PageTemplateEditViewModel(Database, Translator, CurrentSession, pageTemplate)];
                    }
                }

                return string.Empty;
            });
            Post("/pagetemplate/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PageTemplateEditViewModel>(ReadBody());
                var pageTemplate = Database.Query<PageTemplate>(idString);

                if (status.ObjectNotNull(pageTemplate))
                {
                    if (HasAccess(pageTemplate.Organization.Value,
                                  pageTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        status.AssignStringRequired("Label", pageTemplate.Label, model.Label);
                        status.AssignEnumIntString("Language", pageTemplate.Language, model.Language);
                        status.AssignEnumIntString("AssignmentType", pageTemplate.AssignmentType, model.AssignmentType);
                        status.AssignObjectIdString("Organization", pageTemplate.Organization, model.Organization);
                        status.AssignStringRequired("Title", pageTemplate.Title, model.TitleText);
                        pageTemplate.HtmlText.Value = model.HtmlText;

                        var usedInAssignment = Database
                            .Query<LatexTemplateAssignment>(DC.Equal("templateid", pageTemplate.Id.Value))
                            .FirstOrDefault();

                        if (usedInAssignment != null &&
                            usedInAssignment.AssignedType.Value != pageTemplate.AssignmentType.Value)
                        {
                            status.SetValidationError("AssignmentType", "PageTemplate.Validation.Error.AssignmentType", "Assignment type changed in used latex template", "Change not allowed in used template");
                        }

                        if (usedInAssignment != null &&
                            usedInAssignment.GetOrganization(Database) != pageTemplate.Organization.Value)
                        {
                            status.SetValidationError("Organization", "PageTemplate.Validation.Error.Organization", "Organization changed in used latex template", "Change not allowed in used template");
                        }

                        if (status.IsSuccess)
                        {
                            if (status.HasAccess(pageTemplate.Organization.Value,
                                                 pageTemplate.AssignmentType.Value.AccessPart(),
                                                 AccessRight.Write))
                            {
                                Database.Save(pageTemplate);
                                Notice("{0} changed mail template {1}", CurrentSession.User.ShortHand, pageTemplate);
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
            Get("/pagetemplate/add", parameters =>
            {
                if (SomeAccess(AccessRight.Write))
                {
                    return View["View/pagetemplateedit.sshtml",
                        new PageTemplateEditViewModel(Database, Translator, CurrentSession)];
                }
                return string.Empty;
            });
            Post("/pagetemplate/add/new", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PageTemplateEditViewModel>(ReadBody());
                var pageTemplate = new PageTemplate(Guid.NewGuid());
                status.AssignStringRequired("Label", pageTemplate.Label, model.Label);
                status.AssignEnumIntString("Language", pageTemplate.Language, model.Language);
                status.AssignEnumIntString("AssignmentType", pageTemplate.AssignmentType, model.AssignmentType);
                status.AssignObjectIdString("Organization", pageTemplate.Organization, model.Organization);
                status.AssignStringRequired("Title", pageTemplate.Title, model.TitleText);
                pageTemplate.HtmlText.Value = model.HtmlText;

                if (status.IsSuccess)
                {
                    if (status.HasAccess(pageTemplate.Organization.Value,
                                         pageTemplate.AssignmentType.Value.AccessPart(),
                                         AccessRight.Write))
                    {
                        Database.Save(pageTemplate);
                        Notice("{0} added mail template {1}", CurrentSession.User.ShortHand, pageTemplate);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/pagetemplate/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var pageTemplate = Database.Query<PageTemplate>(idString);

                if (pageTemplate != null)
                {
                    if (HasAccess(pageTemplate.Organization.Value,
                                  pageTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            pageTemplate.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted mail template {1}", CurrentSession.User.ShortHand, pageTemplate.GetText(Translator));
                        }
                    }
                }

                return string.Empty;
            });
            Get("/pagetemplate/copy/{id}", parameters =>
            {
                string idString = parameters.id;
                var pageTemplate = Database.Query<PageTemplate>(idString);

                if (pageTemplate != null)
                {
                    if (HasAccess(pageTemplate.Organization.Value,
                                  pageTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        var newTemplate = new PageTemplate(Guid.NewGuid());
                        newTemplate.Organization.Value = pageTemplate.Organization.Value;
                        newTemplate.AssignmentType.Value = pageTemplate.AssignmentType.Value;
                        newTemplate.Language.Value = pageTemplate.Language.Value;
                        newTemplate.Label.Value = pageTemplate.Label.Value +
                            Translate("PageTemplate.Copy.Postfix", "Postfix of copied mailings", " (Copy)");
                        newTemplate.Title.Value = pageTemplate.Title.Value;
                        newTemplate.HtmlText.Value = pageTemplate.HtmlText.Value;
                        Database.Save(newTemplate);
                        Notice("{0} added mail template {1}", CurrentSession.User.ShortHand, newTemplate.GetText(Translator));
                    }
                }

                return string.Empty;
            });
        }

        public bool SomeAccess(AccessRight right)
        {
            return HasAnyOrganizationAccess(PartAccess.Ballot, right);
        }
    }
}

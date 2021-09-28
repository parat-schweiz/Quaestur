using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Hospes
{
    public class LatexTemplateEditViewModel : MasterViewModel
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
        public string Text;
        public string PhraseFieldLabel;
        public string PhraseFieldLanguage;
        public string PhraseFieldAssignmentType;
        public string PhraseFieldOrganization;
        public string PhraseFieldText;
        public string PhraseButtonSave;
        public string PhraseButtonCancel;

        public LatexTemplateEditViewModel()
        { 
        }

        public LatexTemplateEditViewModel(Translator translator, Session session)
            : base(translator, translator.Get("LatexTemplate.Edit.Title", "Title of the latex template edit page", "Edit latex template"), session)
        {
            PhraseFieldLabel = translator.Get("LatexTemplate.Edit.Field.Label", "Label field in the latex template edit page", "Label");
            PhraseFieldLanguage = translator.Get("LatexTemplate.Edit.Field.Language", "Language field in the latex template edit page", "Language");
            PhraseFieldAssignmentType = translator.Get("LatexTemplate.Edit.Field.AssignmentType", "Assignment type field in the latex template edit page", "Assignment type");
            PhraseFieldOrganization = translator.Get("LatexTemplate.Edit.Field.Organization", "Organization field in the latex template edit page", "Organization");
            PhraseFieldText = translator.Get("LatexTemplate.Edit.Field.Text", "Text field in the latex template edit page", "Text");
            PhraseButtonSave = translator.Get("LatexTemplate.Edit.Button.Save", "Save button in the latex template edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("LatexTemplate.Edit.Button.Cancel", "Cancel button in the latex template edit page", "Cancel").EscapeHtml();
        }

        public LatexTemplateEditViewModel(IDatabase database, Translator translator, Session session)
            : this(translator, session)
        {
            Method = "add";
            Id = "new";
            Label = string.Empty;
            Text = string.Empty;
            Language = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, false));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, false));
            AssignmentTypes = new List<NamedIntViewModel>();
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.MembershipType, false));
            Organizations = new List<NamedIdViewModel>(database
                .Query<Organization>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
        }

        public LatexTemplateEditViewModel(IDatabase database, Translator translator, Session session, LatexTemplate latexTemplate)
            : this(translator, session)
        {
            Method = "edit";
            Id = latexTemplate.Id.ToString();
            Label = latexTemplate.Label.Value.EscapeHtml();
            Text = latexTemplate.Text.Value;
            Language = string.Empty;
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, latexTemplate.Language.Value == SiteLibrary.Language.English));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, latexTemplate.Language.Value == SiteLibrary.Language.German));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, latexTemplate.Language.Value == SiteLibrary.Language.French));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, latexTemplate.Language.Value == SiteLibrary.Language.Italian));
            AssignmentTypes = new List<NamedIntViewModel>();
            AssignmentTypes.Add(new NamedIntViewModel(translator, TemplateAssignmentType.MembershipType, TemplateAssignmentType.MembershipType == latexTemplate.AssignmentType.Value));
            Organizations = new List<NamedIdViewModel>(database
                .Query<Organization>()
                .Select(o => new NamedIdViewModel(translator, o, latexTemplate.Organization.Value == o))
                .OrderBy(o => o.Name));
        }
    }

    public class LatexTemplateViewModel : MasterViewModel
    {
        public LatexTemplateViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("LatexTemplate.List.Title", "Title of the latex template list page", "Countries"), 
            session)
        { 
        }
    }

    public class LatexTemplateListItemViewModel
    {
        public string Id;
        public string Label;
        public string Assigned;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public LatexTemplateListItemViewModel(IDatabase database, Session session, Translator translator, LatexTemplate latexTemplate)
        {
            Id = latexTemplate.Id.Value.ToString();
            Label = latexTemplate.Label.Value;
            Assigned = string.Join(", ", latexTemplate.Assignments(database).Select(a => a.GetText(database, translator)));
            Editable = session.HasAccess(latexTemplate.Organization.Value, latexTemplate.AssignmentType.Value.AccessPart(), AccessRight.Write) ? "editable" : string.Empty;
            PhraseDeleteConfirmationQuestion = translator.Get("LatexTemplate.List.Delete.Confirm.Question", "Delete latex template confirmation question", "Do you really wish to delete latex template {0}?", latexTemplate.GetText(translator));
        }
    }

    public class LatexTemplateListViewModel
    {
        public string PhraseHeaderLabel;
        public string PhraseHeaderAssigned;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<LatexTemplateListItemViewModel> List;

        public LatexTemplateListViewModel(IDatabase database, Translator translator, Session session)
        {
            PhraseHeaderLabel = translator.Get("LatexTemplate.List.Header.Label", "Column 'Label' in the latex template list", "Label").EscapeHtml();
            PhraseHeaderAssigned = translator.Get("LatexTemplate.List.Header.Assigned", "Column 'Assigned' in the latex template list", "Assigned").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("LatexTemplate.List.Delete.Confirm.Title", "Delete latexTemplate confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("LatexTemplate.List.Delete.Confirm.Info", "Delete latexTemplate confirmation info", "This will also delete all assignments of this latex template.").EscapeHtml();
            List = new List<LatexTemplateListItemViewModel>(
                database.Query<LatexTemplate>()
                .Where(t => session.HasAccess(t.Organization.Value, t.AssignmentType.Value.AccessPart(), AccessRight.Read))
                .OrderBy(t => t.Label.Value)
                .Select(c => new LatexTemplateListItemViewModel(database, session, translator, c)));
        }
    }

    public class LatexTemplateEdit : HospesModule
    {
        public LatexTemplateEdit()
        {
            RequireCompleteLogin();

            Get("/latextemplate", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/latextemplate.sshtml",
                        new LatexTemplateViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/latextemplate/list", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/latextemplatelist.sshtml",
                        new LatexTemplateListViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/latextemplate/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var latexTemplate = Database.Query<LatexTemplate>(idString);

                if (latexTemplate != null)
                {
                    if (HasAccess(latexTemplate.Organization.Value, 
                                  latexTemplate.AssignmentType.Value.AccessPart(), 
                                  AccessRight.Write))
                    {
                        return View["View/latextemplateedit.sshtml",
                            new LatexTemplateEditViewModel(Database, Translator, CurrentSession, latexTemplate)];
                    }
                }

                return string.Empty;
            });
            Post("/latextemplate/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<LatexTemplateEditViewModel>(ReadBody());
                var latexTemplate = Database.Query<LatexTemplate>(idString);

                if (status.ObjectNotNull(latexTemplate))
                {
                    if (HasAccess(latexTemplate.Organization.Value,
                                  latexTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        status.AssignStringRequired("Label", latexTemplate.Label, model.Label);
                        status.AssignEnumIntString("Language", latexTemplate.Language, model.Language);
                        status.AssignEnumIntString("AssignmentType", latexTemplate.AssignmentType, model.AssignmentType);
                        status.AssignObjectIdString("Organization", latexTemplate.Organization, model.Organization);
                        status.AssignStringFree("Text", latexTemplate.Text, model.Text);

                        var usedInAssignment = Database
                            .Query<LatexTemplateAssignment>(DC.Equal("templateid", latexTemplate.Id.Value))
                            .FirstOrDefault();

                        if (usedInAssignment != null &&
                            usedInAssignment.AssignedType.Value != latexTemplate.AssignmentType.Value)
                        {
                            status.SetValidationError("AssignmentType", "LatexTemplate.Validation.Error.AssignmentType", "Assignment type changed in used latex template", "Change not allowed in used template");
                        }

                        if (usedInAssignment != null &&
                            usedInAssignment.GetOrganization(Database) != latexTemplate.Organization.Value)
                        {
                            status.SetValidationError("Organization", "LatexTemplate.Validation.Error.Organization", "Organization changed in used latex template", "Change not allowed in used template");
                        }

                        if (status.IsSuccess)
                        {
                            if (status.HasAccess(latexTemplate.Organization.Value,
                                                 latexTemplate.AssignmentType.Value.AccessPart(),
                                                 AccessRight.Write))
                            {
                                Database.Save(latexTemplate);
                                Notice("{0} changed latex template {1}", CurrentSession.User.ShortHand, latexTemplate);
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
            Get("/latextemplate/add", parameters =>
            {
                if (SomeAccess(AccessRight.Write))
                {
                    return View["View/latextemplateedit.sshtml",
                        new LatexTemplateEditViewModel(Database, Translator, CurrentSession)];
                }
                return string.Empty;
            });
            Post("/latextemplate/add/new", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<LatexTemplateEditViewModel>(ReadBody());
                var latexTemplate = new LatexTemplate(Guid.NewGuid());
                status.AssignStringRequired("Label", latexTemplate.Label, model.Label);
                status.AssignEnumIntString("Language", latexTemplate.Language, model.Language);
                status.AssignEnumIntString("AssignmentType", latexTemplate.AssignmentType, model.AssignmentType);
                status.AssignObjectIdString("Organization", latexTemplate.Organization, model.Organization);
                status.AssignStringFree("Text", latexTemplate.Text, model.Text);

                if (status.IsSuccess)
                {
                    if (status.HasAccess(latexTemplate.Organization.Value,
                                         latexTemplate.AssignmentType.Value.AccessPart(),
                                         AccessRight.Write))
                    {
                        Database.Save(latexTemplate);
                        Notice("{0} added latex template {1}", CurrentSession.User.ShortHand, latexTemplate);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/latextemplate/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var latexTemplate = Database.Query<LatexTemplate>(idString);

                if (latexTemplate != null)
                {
                    if (HasAccess(latexTemplate.Organization.Value,
                                  latexTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            latexTemplate.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted latex template {1}", CurrentSession.User.ShortHand, latexTemplate.GetText(Translator));
                        }
                    }
                }

                return string.Empty;
            });
            Get("/latextemplate/copy/{id}", parameters =>
            {
                string idString = parameters.id;
                var latexTemplate = Database.Query<LatexTemplate>(idString);

                if (latexTemplate != null)
                {
                    if (HasAccess(latexTemplate.Organization.Value,
                                  latexTemplate.AssignmentType.Value.AccessPart(),
                                  AccessRight.Write))
                    {
                        var newTemplate = new LatexTemplate(Guid.NewGuid());
                        newTemplate.Organization.Value = latexTemplate.Organization.Value;
                        newTemplate.AssignmentType.Value = latexTemplate.AssignmentType.Value;
                        newTemplate.Language.Value = latexTemplate.Language.Value;
                        newTemplate.Label.Value = latexTemplate.Label.Value +
                            Translate("LatexTemplate.Copy.Postfix", "Postfix of copied latexings", " (Copy)");
                        newTemplate.Text.Value = latexTemplate.Text.Value;
                        Database.Save(newTemplate);
                        Notice("{0} added latex template {1}", CurrentSession.User.ShortHand, newTemplate.GetText(Translator));
                    }
                }

                return string.Empty;
            });
        }

        public bool SomeAccess(AccessRight right)
        {
            return HasAnyOrganizationAccess(PartAccess.Structure, right);
        }
    }
}

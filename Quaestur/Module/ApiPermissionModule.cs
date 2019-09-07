using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class ApiPermissionEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Part;
        public string Subject;
        public string Right;
        public List<NamedIntViewModel> Parts;
        public List<NamedIntViewModel> Subjects;
        public List<NamedIntViewModel> Rights;
        public string PhraseFieldPart;
        public string PhraseFieldSubject;
        public string PhraseFieldRight;

        public ApiPermissionEditViewModel()
        {
        }

        public ApiPermissionEditViewModel(Translator translator)
            : base(translator, translator.Get("ApiPermission.Edit.Title", "Title of the API permission edit dialog", "Edit API permission"), "apiPermissionEditDialog")
        {
            PhraseFieldPart = translator.Get("ApiPermission.Edit.Field.Part", "Part field in the API permission edit dialog", "Part").EscapeHtml();
            PhraseFieldSubject = translator.Get("ApiPermission.Edit.Field.Subject", "Subject field in the API permission edit dialog", "Subject").EscapeHtml();
            PhraseFieldRight = translator.Get("ApiPermission.Edit.Field.Right", "Right field in the API permission edit dialog", "Right").EscapeHtml();
            Parts = new List<NamedIntViewModel>();
            Subjects = new List<NamedIntViewModel>();
            Rights = new List<NamedIntViewModel>();
        }

        public ApiPermissionEditViewModel(Translator translator, IDatabase db, ApiClient apiClient)
            : this(translator)
        {
            Method = "add";
            Id = apiClient.Id.Value.ToString();
            Part = string.Empty;
            Subject = string.Empty;
            Right = string.Empty;
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Anonymous, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Ballot, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Billing, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Contact, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Crypto, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.CustomDefinitions, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Deleted, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Demography, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Documents, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Journal, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Mailings, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Membership, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.PointBudget, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Points, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.RoleAssignments, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Security, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Structure, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.TagAssignments, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Group, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Organization, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SubOrganization, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SystemWide, false));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Read, false));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Write, false));
        }

        public ApiPermissionEditViewModel(Translator translator, IDatabase db, ApiPermission permission)
            : this(translator)
        {
            Method = "edit";
            Id = permission.Id.ToString();
            Part = ((int)permission.Part.Value).ToString();
            Subject = ((int)permission.Subject.Value).ToString();
            Right = ((int)permission.Right.Value).ToString();
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Anonymous, permission.Part.Value == PartAccess.Anonymous));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Ballot, permission.Part.Value == PartAccess.Ballot));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Billing, permission.Part.Value == PartAccess.Billing));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Contact, permission.Part.Value == PartAccess.Contact));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Crypto, permission.Part.Value == PartAccess.Crypto));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.CustomDefinitions, permission.Part.Value == PartAccess.CustomDefinitions));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Deleted, permission.Part.Value == PartAccess.Deleted));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Demography, permission.Part.Value == PartAccess.Demography));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Documents, permission.Part.Value == PartAccess.Documents));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Journal, permission.Part.Value == PartAccess.Journal));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Mailings, permission.Part.Value == PartAccess.Mailings));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Membership, permission.Part.Value == PartAccess.Membership));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.PointBudget, permission.Part.Value == PartAccess.PointBudget));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Points, permission.Part.Value == PartAccess.Points));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.RoleAssignments, permission.Part.Value == PartAccess.RoleAssignments));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Security, permission.Part.Value == PartAccess.Security));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Structure, permission.Part.Value == PartAccess.Structure));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.TagAssignments, permission.Part.Value == PartAccess.TagAssignments));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Group, permission.Subject.Value == SubjectAccess.Group));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Organization, permission.Subject.Value == SubjectAccess.Organization));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SubOrganization, permission.Subject.Value == SubjectAccess.SubOrganization));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SystemWide, permission.Subject.Value == SubjectAccess.SystemWide));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Read, permission.Right.Value == AccessRight.Read));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Write, permission.Right.Value == AccessRight.Write));
        }
    }

    public class ApiPermissionViewModel : MasterViewModel
    {
        public string Id;

        public ApiPermissionViewModel(Translator translator, Session session, ApiClient apiClient)
            : base(translator, 
            translator.Get("ApiPermission.List.Title", "Title of the API permission list page", "API permissions"), 
            session)
        {
            Id = apiClient.Id.Value.ToString();
        }
    }

    public class ApiPermissionListItemViewModel
    {
        public string Id;
        public string Part;
        public string Subject;
        public string Right;
        public string PhraseDeleteConfirmationQuestion;

        public ApiPermissionListItemViewModel(Translator translator, ApiPermission permission)
        {
            Id = permission.Id.Value.ToString();
            Part = permission.Part.Value.Translate(translator).EscapeHtml();
            Subject = permission.Subject.Value.Translate(translator).EscapeHtml();
            Right = permission.Right.Value.Translate(translator).EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("ApiPermission.List.Delete.Confirm.Question", "Delete permission confirmation question", "Do you really wish to delete API permission {0}?", permission.GetText(translator)).EscapeHtml();
        }
    }

    public class ApiPermissionListViewModel
    {
        public string Id;
        public string ParentId;
        public string PhraseHeaderApiClient;
        public string PhraseHeaderPart;
        public string PhraseHeaderSubject;
        public string PhraseHeaderRight;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<ApiPermissionListItemViewModel> List;

        public ApiPermissionListViewModel(Translator translator, IDatabase database, Session session, ApiClient apiClient)
        {
            Id = apiClient.Id.Value.ToString();
            ParentId = apiClient.Group.Value.Id.Value.ToString();
            PhraseHeaderApiClient = apiClient.GetText(translator);
            PhraseHeaderPart = translator.Get("ApiPermission.List.Header.Part", "Link 'Part' caption in the API permission list", "Part").EscapeHtml();
            PhraseHeaderSubject = translator.Get("ApiPermission.List.Header.Subject", "Link 'Subject' caption in the API permission list", "Subject").EscapeHtml();
            PhraseHeaderRight = translator.Get("ApiPermission.List.Header.Right", "Link 'Right' caption in the API permission list", "Right").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("ApiPermission.List.Delete.Confirm.Title", "Delete API permission confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
            List = new List<ApiPermissionListItemViewModel>(
                database.Query<ApiPermission>(DC.Equal("apiClientid", apiClient.Id.Value))
                .Select(c => new ApiPermissionListItemViewModel(translator, c))
                .OrderBy(c => c.Subject + "/" + c.Part + "/" + c.Right));
        }
    }

    public class ApiPermissionEdit : QuaesturModule
    {
        public ApiPermissionEdit()
        {
            RequireCompleteLogin();

            Get("/apipermission/{id}", parameters =>
            {
                string idString = parameters.id;
                var apiClient = Database.Query<ApiClient>(idString);

                if (apiClient != null)
                {
                    if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/apipermission.sshtml",
                            new ApiPermissionViewModel(Translator, CurrentSession, apiClient)];
                    }
                }

                return string.Empty;
            });
            Get("/apipermission/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var apiClient = Database.Query<ApiClient>(idString);

                if (apiClient != null)
                {
                    if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/apipermissionlist.sshtml",
                            new ApiPermissionListViewModel(Translator, Database, CurrentSession, apiClient)];
                    }
                }

                return string.Empty;
            });
            Get("/apipermission/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var permission = Database.Query<ApiPermission>(idString);

                if (permission != null)
                {
                    if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/apipermissionedit.sshtml",
                            new ApiPermissionEditViewModel(Translator, Database, permission)];
                    }
                }

                return string.Empty;
            });
            Post("/apipermission/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<ApiPermissionEditViewModel>(ReadBody());
                var permission = Database.Query<ApiPermission>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(permission))
                {
                    if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignEnumIntString("Part", permission.Part, model.Part);
                        status.AssignEnumIntString("Subject", permission.Subject, model.Subject);
                        status.AssignEnumIntString("Right", permission.Right, model.Right);

                        if (status.IsSuccess)
                        {
                            Database.Save(permission);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/apipermission/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var apiClient = Database.Query<ApiClient>(idString);

                if (apiClient != null)
                {
                    if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/apipermissionedit.sshtml",
                            new ApiPermissionEditViewModel(Translator, Database, apiClient)];
                    }
                }

                return string.Empty;
            });
            Post("/apipermission/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var apiClient = Database.Query<ApiClient>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(apiClient))
                {
                    if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<ApiPermissionEditViewModel>(ReadBody());
                        var permission = new ApiPermission(Guid.NewGuid());
                        status.AssignEnumIntString("Part", permission.Part, model.Part);
                        status.AssignEnumIntString("Subject", permission.Subject, model.Subject);
                        status.AssignEnumIntString("Right", permission.Right, model.Right);
                        permission.ApiClient.Value = apiClient;

                        if (status.IsSuccess)
                        {
                            Database.Save(permission);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/apipermission/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var permission = Database.Query<ApiPermission>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(permission))
                {
                    if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            permission.Delete(Database);
                            transaction.Commit();
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

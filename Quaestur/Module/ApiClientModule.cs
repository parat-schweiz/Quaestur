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
    public class ApiClientEditViewModel : DialogViewModel
    {
        public const string SecretUnchanged = "===$$$§§§secretunchanged§§§$$$===";

        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public List<NamedIdViewModel> Groups;
        public string Group;
        public string Secret;
        public string PhraseFieldGroup;
        public string PhraseFieldId;
        public string PhraseFieldSecret;

        public ApiClientEditViewModel()
        { 
        }

        public ApiClientEditViewModel(Translator translator)
            : base(translator, translator.Get("ApiClient.Edit.Title", "Title of the API client edit dialog", "Edit API Client"), "apiClientEditDialog")
        {
            PhraseFieldGroup = translator.Get("ApiClient.Edit.Field.Group", "Group field in the API client edit dialog", "Group");
            PhraseFieldId = translator.Get("ApiClient.Edit.Field.Id", "Id field in the API client edit dialog", "ID");
            PhraseFieldSecret = translator.Get("ApiClient.Edit.Field.Secret", "Secret field in the API client edit dialog", "Secret");
        }

        public ApiClientEditViewModel(Translator translator, IDatabase database, Session session)
            : this(translator)
        {
            Method = "add";
            Id = string.Empty;
            Name = translator.CreateLanguagesMultiItem("ApiClient.Edit.Field.Name", "Name field in the API client edit dialog", "Name ({0})", new MultiLanguageString());
            Group = string.Empty;
            Secret = string.Empty;
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Structure, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(g => g.Name));
        }

        public ApiClientEditViewModel(Translator translator, IDatabase database, Session session, ApiClient apiClient)
            : this(translator)
        {
            Method = "edit";
            Id = apiClient.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("ApiClient.Edit.Field.Name", "Name field in the API client edit dialog", "Name ({0})", apiClient.Name.Value);
            Group = string.Empty;
            Secret = SecretUnchanged;
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Structure, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, g == apiClient.Group.Value))
                .OrderBy(g => g.Name));
        }
    }

    public class ApiClientViewModel : MasterViewModel
    {
        public ApiClientViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("ApiClient.List.Title", "Title of the API client list page", "API clients"), 
            session)
        {
        }
    }

    public class ApiClientListItemViewModel
    {
        public string Id;
        public string Name;
        public string Access;
        public string PhraseDeleteConfirmationQuestion;

        private string GetText(Translator translator, ApiPermission permission)
        {
            return translator.Get(
                "ApiClient.List.Access.Permission",
                "Specification of a single permission in the API client list",
                "{0} access to {1} of {2}",
                permission.Right.GetText(translator),
                permission.Part.GetText(translator),
                permission.Subject.GetText(translator));
        }

        public ApiClientListItemViewModel(Translator translator, IDatabase database, Session session, ApiClient apiClient)
        {
            Id = apiClient.Id.Value.ToString();
            Name = apiClient.Name.Value[translator.Language];
            Access = string.Join("<br/>", apiClient.Permissions
                .Select(p => GetText(translator, p))
                .OrderBy(p => p));
            if (string.IsNullOrEmpty(Access))
                Access = translator.Get("ApiClient.List.Access.None", "None access in role list", "None");
            PhraseDeleteConfirmationQuestion = translator.Get("ApiClient.List.Delete.Confirm.Question", "Delete role confirmation question", "Do you really wish to delete role {0}?", apiClient.GetText(translator)).EscapeHtml();
        }
    }

    public class ApiClientListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseHeaderAccess;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<ApiClientListItemViewModel> List;

        public ApiClientListViewModel(Translator translator, IDatabase database, Session session)
        {
            PhraseHeaderName = translator.Get("ApiClient.List.Header.Name", "Column 'Name' in the API client list", "Name").EscapeHtml();
            PhraseHeaderAccess = translator.Get("ApiClient.List.Header.Access", "Column 'Access' in the API client list", "Access").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("ApiClient.List.Delete.Confirm.Title", "Delete API client confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("ApiClient.List.Delete.Confirm.Info", "Delete API client confirmation info", "This will also delete all permission of that API client.").EscapeHtml();
            List = new List<ApiClientListItemViewModel>(database
                .Query<ApiClient>()
                .Select(r => new ApiClientListItemViewModel(translator, database, session, r))
                .OrderBy(r => r.Name));
        }
    }

    public class ApiClientEdit : QuaesturModule
    {
        public ApiClientEdit()
        {
            RequireCompleteLogin();

            Get("/apiclient", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Read))
                {
                    return View["View/apiclient.sshtml",
                        new ApiClientViewModel(Translator, CurrentSession)];
                }

                return string.Empty;
            });
            Get("/apiclient/list", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Read))
                {
                    return View["View/apiclientlist.sshtml",
                       new ApiClientListViewModel(Translator, Database, CurrentSession)];
                }

                return string.Empty;
            });
            Get("/apiclient/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var apiClient = Database.Query<ApiClient>(idString);

                if (apiClient != null)
                {
                    if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/apiclientedit.sshtml",
                            new ApiClientEditViewModel(Translator, Database, CurrentSession, apiClient)];
                    }
                }

                return string.Empty;
            });
            Post("/apiclient/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<ApiClientEditViewModel>(ReadBody());
                var apiClient = Database.Query<ApiClient>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(apiClient))
                {
                    if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", apiClient.Name, model.Name);
                        status.AssignObjectIdString("Group", apiClient.Group, model.Group);

                        if (model.Secret != ApiClientEditViewModel.SecretUnchanged)
                        {
                            if (model.Secret != null &&
                                model.Secret.Length < 16)
                            {
                                status.SetValidationError(
                                    "Secret", 
                                    "ApiClient.Edit.Validation.ErrorSecretTooShort", 
                                    "Validation error when secret too short in API client dialog", 
                                    "Too short");
                            }
                            else
                            {
                                apiClient.SecureSecret.Value = Global.Security.SecurePassword(model.Secret);
                            }
                        }

                        if (status.IsSuccess)
                        {
                            Database.Save(apiClient);
                            Notice("{0} changed API client {1}", CurrentSession.User.ShortHand, apiClient);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/apiclient/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    return View["View/apiclientedit.sshtml",
                        new ApiClientEditViewModel(Translator, Database, CurrentSession)];
                }

                return string.Empty;
            });
            Post("/apiclient/add", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    var model = JsonConvert.DeserializeObject<ApiClientEditViewModel>(ReadBody());
                    var apiClient = new ApiClient(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", apiClient.Name, model.Name);
                    status.AssignObjectIdString("Group", apiClient.Group, model.Group);

                    if (model.Secret != null &&
                        model.Secret.Length < 16)
                    {
                        status.SetValidationError(
                            "Secret",
                            "ApiClient.Edit.Validation.ErrorSecretTooShort",
                            "Validation error when secret too short in API client dialog",
                            "Too short");
                    }
                    else
                    {
                        apiClient.SecureSecret.Value = Global.Security.SecurePassword(model.Secret);
                    }

                    if (status.IsSuccess)
                    {
                        Database.Save(apiClient);
                        Notice("{0} added API client {1}", CurrentSession.User.ShortHand, apiClient);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/apiclient/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var apiClient = Database.Query<ApiClient>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(apiClient))
                {
                    if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            apiClient.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted API client {1}", CurrentSession.User.ShortHand, apiClient);
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

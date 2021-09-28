using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using SiteLibrary;

namespace Hospes
{
    public class Oauth2ClientEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Secret;
        public bool RequireTwoFactor;
        public string[] Access;
        public List<NamedIntViewModel> Accesses;
        public string RedirectUri;
        public string PhraseFieldId;
        public string PhraseFieldSecret;
        public string PhraseFieldRedirectUri;
        public string PhraseFieldRequireTwoFactor;
        public string PhraseFieldAccess;

        public Oauth2ClientEditViewModel()
        { 
        }

        public Oauth2ClientEditViewModel(Translator translator)
            : base(translator, translator.Get("Oauth2Client.Edit.Title", "Title of the OAuth2 client edit dialog", "Edit client"), "clientEditDialog")
        {
            PhraseFieldId = translator.Get("Oauth2Client.Edit.Field.Id", "Client ID field in the OAuth2 client edit dialog", "Client ID");
            PhraseFieldSecret = translator.Get("Oauth2Client.Edit.Field.Secret", "Secret field in the OAuth2 client edit dialog", "Secret");
            PhraseFieldRedirectUri = translator.Get("Oauth2Client.Edit.Field.RedirectUri", "Redirect URI field in the OAuth2 client edit dialog", "Redirect URI");
            PhraseFieldRequireTwoFactor = translator.Get("Oauth2Client.Edit.Field.RequireTwoFactor", "Require second factor field in the OAuth2 client edit dialog", "Require second factor");
            PhraseFieldAccess = translator.Get("Oauth2Client.Edit.Field.Access", "Access field in the OAuth2 client edit dialog", "Access");
        }

        public Oauth2ClientEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Oauth2Client.Edit.Field.Name", "Name field in the OAuth2 client edit dialog", "Name ({0})", new MultiLanguageString());
            Secret = Rng.Get(16).ToHexString();
            RedirectUri = string.Empty;
            RequireTwoFactor = false;
            Accesses = new List<NamedIntViewModel>();
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Membership, false));
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Email, false));
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Fullname, false));
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Roles, false));
        }

        public Oauth2ClientEditViewModel(Translator translator, IDatabase db, Oauth2Client client)
            : this(translator)
        {
            Method = "edit";
            Id = client.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Oauth2Client.Edit.Field.Name", "Name field in the OAuth2 client edit dialog", "Name ({0})", client.Name.Value);
            Secret = client.Secret.Value;
            RedirectUri = client.RedirectUri.Value;
            RequireTwoFactor = client.RequireTwoFactor.Value;
            Accesses = new List<NamedIntViewModel>();
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Membership, client.Access.Value.HasFlag(Oauth2ClientAccess.Membership)));
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Email, client.Access.Value.HasFlag(Oauth2ClientAccess.Email)));
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Fullname, client.Access.Value.HasFlag(Oauth2ClientAccess.Fullname)));
            Accesses.Add(new NamedIntViewModel(translator, Oauth2ClientAccess.Roles, client.Access.Value.HasFlag(Oauth2ClientAccess.Roles)));
        }
    }

    public class Oauth2ClientViewModel : MasterViewModel
    {
        public Oauth2ClientViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("Oauth2Client.List.Title", "Title of the OAuth2 client list page", "OAuth2 Clients"), 
            session)
        { 
        }
    }

    public class Oauth2ClientListItemViewModel
    {
        public string Id;
        public string Name;
        public string PhraseDeleteConfirmationQuestion;

        public Oauth2ClientListItemViewModel(Translator translator, Oauth2Client client)
        {
            Id = client.Id.Value.ToString();
            Name = client.Name.Value[translator.Language].EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Oauth2Client.List.Delete.Confirm.Question", "Delete OAuth2 client confirmation question", "Do you really wish to delete OAuth2 client {0}?", client.GetText(translator));
        }
    }

    public class Oauth2ClientListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<Oauth2ClientListItemViewModel> List;

        public Oauth2ClientListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Oauth2Client.List.Header.Name", "Column 'Name' in the OAuth2 client list", "Name").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Oauth2Client.List.Delete.Confirm.Title", "Delete OAuth2 client confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Oauth2Client.List.Delete.Confirm.Info", "Delete OAuth2 client confirmation info", "This will also terminate all sessions/tokens for that OAuth2 client.").EscapeHtml();
            List = new List<Oauth2ClientListItemViewModel>(
                database.Query<Oauth2Client>()
                .Select(c => new Oauth2ClientListItemViewModel(translator, c)));
        }
    }

    public class Oauth2ClientEdit : HospesModule
    {
        public Oauth2ClientEdit()
        {
            RequireCompleteLogin();

            Get("/oauth2client", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Crypto, AccessRight.Write))
                {
                    return View["View/oauth2client.sshtml",
                        new Oauth2ClientViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/oauth2client/list", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Crypto, AccessRight.Write))
                {
                    return View["View/oauth2clientlist.sshtml",
                        new Oauth2ClientListViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Get("/oauth2client/edit/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Crypto, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var client = Database.Query<Oauth2Client>(idString);

                    if (client != null)
                    {
                        return View["View/oauth2clientedit.sshtml",
                            new Oauth2ClientEditViewModel(Translator, Database, client)];
                    }
                }
                return string.Empty;
            });
            Post("/oauth2client/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.Crypto, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<Oauth2ClientEditViewModel>(ReadBody());
                    var client = Database.Query<Oauth2Client>(idString);

                    if (status.ObjectNotNull(client))
                    {
                        status.AssignMultiLanguageRequired("Name", client.Name, model.Name);
                        status.AssignStringRequired("Secret", client.Secret, model.Secret);
                        status.AssignStringRequired("RedirectUri", client.RedirectUri, model.RedirectUri);
                        status.AssignFlagIntsString("Access", client.Access, model.Access);
                        client.RequireTwoFactor.Value = model.RequireTwoFactor;

                        if (status.IsSuccess)
                        {
                            Database.Save(client);
                            Notice("{0} changed OAuth2 client {1}", CurrentSession.User.ShortHand, client);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/oauth2client/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/oauth2clientedit.sshtml",
                    new Oauth2ClientEditViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Post("/oauth2client/add/new", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.Crypto, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<Oauth2ClientEditViewModel>(ReadBody());
                    var client = new Oauth2Client(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", client.Name, model.Name);
                    status.AssignStringRequired("Secret", client.Secret, model.Secret);
                    status.AssignStringRequired("RedirectUri", client.RedirectUri, model.RedirectUri);
                    status.AssignFlagIntsString("Access", client.Access, model.Access);
                    client.RequireTwoFactor.Value = model.RequireTwoFactor;

                    if (status.IsSuccess)
                    {
                        Database.Save(client);
                        Notice("{0} added OAuth2 client {1}", CurrentSession.User.ShortHand, client);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/oauth2client/delete/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Crypto, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var client = Database.Query<Oauth2Client>(idString);

                    if (client != null)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            client.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted OAuth2 client {1}", CurrentSession.User.ShortHand, client);
                        }
                    }
                }
                return string.Empty;
            });
        }
    }
}

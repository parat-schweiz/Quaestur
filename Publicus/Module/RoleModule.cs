using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class RoleEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;

        public RoleEditViewModel()
        { 
        }

        public RoleEditViewModel(Translator translator)
            : base(translator, translator.Get("Role.Edit.Title", "Title of the role edit dialog", "Edit role"), "roleEditDialog")
        {
        }

        public RoleEditViewModel(Translator translator, IDatabase db, Group group)
            : this(translator)
        {
            Method = "add";
            Id = group.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("Role.Edit.Field.Name", "Name field in the role edit dialog", "Name ({0})", new MultiLanguageString());
        }

        public RoleEditViewModel(Translator translator, IDatabase db, Role role)
            : this(translator)
        {
            Method = "edit";
            Id = role.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Role.Edit.Field.Name", "Name field in the role edit dialog", "Name ({0})", role.Name.Value);
        }
    }

    public class RoleViewModel : MasterViewModel
    {
        public string Id;

        public RoleViewModel(Translator translator, Session session, Group group)
            : base(translator, 
            translator.Get("Role.List.Title", "Title of the role list page", "Roles"), 
            session)
        {
            Id = group.Id.Value.ToString();
        }
    }

    public class RoleListItemViewModel
    {
        public string Id;
        public string Name;
        public string Access;
        public string Occupants;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        private string GetText(Translator translator, Permission permission)
        {
            return translator.Get(
                "Role.List.Access.Permission",
                "Specification of a single permission in the role list",
                "{0} access to {1} of {2}",
                permission.Right.GetText(translator),
                permission.Part.GetText(translator),
                permission.Subject.GetText(translator));
        }

        public RoleListItemViewModel(Translator translator, IDatabase database, Session session, Role role)
        {
            Id = role.Id.Value.ToString();
            Name = role.Name.Value[translator.Language];
            Access = string.Join("<br/>", role.Permissions
                .Select(p => GetText(translator, p))
                .OrderBy(p => p));
            if (string.IsNullOrEmpty(Access))
                Access = translator.Get("Role.List.Access.None", "None access in role list", "None");
            Occupants = string.Join("<br/>", database
                .Query<RoleAssignment>(DC.Equal("roleid", role.Id.Value))
                .Select(ra => ra.MasterRole.Value.Name.Value[translator.Language])
                .OrderBy(p => p));
            if (string.IsNullOrEmpty(Occupants))
                Occupants = translator.Get("Role.List.Occupants.None", "No occupants in role list", "None");
            Editable =
                session.HasAccess(role.Group.Value.Feed.Value, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Role.List.Delete.Confirm.Question", "Delete role confirmation question", "Do you really wish to delete role {0}?", role.GetText(translator)).EscapeHtml();
        }
    }

    public class RoleListViewModel
    {
        public string Id;
        public string ParentId;
        public string PhraseHeaderFeedGroup;
        public string PhraseHeaderName;
        public string PhraseHeaderAccess;
        public string PhraseHeaderOccupants;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<RoleListItemViewModel> List;
        public bool AddAccess;

        public RoleListViewModel(Translator translator, IDatabase database, Session session, Group group)
        {
            PhraseHeaderFeedGroup = 
                translator.Get("Role.List.Header.FeedGroup", "Header with feed and group in the role list", "{0} of {1}",
                group.Name.Value[translator.Language],
                group.Feed.Value.Name.Value[translator.Language]).EscapeHtml();
            PhraseHeaderName = translator.Get("Role.List.Header.Name", "Column 'Name' in the role list", "Name").EscapeHtml();
            PhraseHeaderAccess = translator.Get("Role.List.Header.Access", "Column 'Access' in the role list", "Access").EscapeHtml();
            PhraseHeaderOccupants = translator.Get("Role.List.Header.Occupants", "Column 'Occupants' in the role list", "Occupants").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Role.List.Delete.Confirm.Title", "Delete role confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Role.List.Delete.Confirm.Info", "Delete role confirmation info", "This will also delete all permission of that role and remove all assignments of that role to any contact.").EscapeHtml();
            Id = group.Id.Value.ToString();
            ParentId = group.Feed.Value.Id.Value.ToString();
            List = new List<RoleListItemViewModel>(
                group.Roles
                .Select(r => new RoleListItemViewModel(translator, database, session, r))
                .OrderBy(r => r.Name));
            AddAccess = session.HasAccess(group, PartAccess.Structure, AccessRight.Write);
        }
    }

    public class RoleEdit : PublicusModule
    {
        public RoleEdit()
        {
            this.RequiresAuthentication();

            Get["/role/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var group = Database.Query<Group>(idString);

                if (group != null)
                {
                    if (HasAccess(group, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/role.sshtml",
                            new RoleViewModel(Translator, CurrentSession, group)];
                    }
                }

                return null;
            };
            Get["/role/list/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var group = Database.Query<Group>(idString);

                if (group != null)
                {
                    if (HasAccess(group, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/rolelist.sshtml",
                           new RoleListViewModel(Translator, Database, CurrentSession, group)];
                    }
                }

                return null;
            };
            Get["/role/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);

                if (role != null)
                {
                    if (HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/roleedit.sshtml",
                        new RoleEditViewModel(Translator, Database, role)];
                    }
                }

                return null;
            };
            Post["/role/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<RoleEditViewModel>(ReadBody());
                var role = Database.Query<Role>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(role))
                {
                    if (status.HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", role.Name, model.Name);

                        if (status.IsSuccess)
                        {
                            Database.Save(role);
                            Notice("{0} changed role {1}", CurrentSession.User.UserName.Value, role);
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/role/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var group = Database.Query<Group>(idString);

                if (group != null)
                {
                    if (HasAccess(group, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/roleedit.sshtml",
                            new RoleEditViewModel(Translator, Database, group)];
                    }
                }

                return null;
            };
            Post["/role/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var group = Database.Query<Group>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(group))
                {
                    if (status.HasAccess(group, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<RoleEditViewModel>(ReadBody());
                        var role = new Role(Guid.NewGuid());
                        status.AssignMultiLanguageRequired("Name", role.Name, model.Name);
                        role.Group.Value = group;

                        if (status.IsSuccess)
                        {
                            Database.Save(role);
                            Notice("{0} added role {1}", CurrentSession.User.UserName.Value, role);
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/role/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(role))
                {
                    if (status.HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            role.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted role {1}", CurrentSession.User.UserName.Value, role);
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

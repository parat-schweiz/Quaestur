using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class PermissionEditViewModel : DialogViewModel
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

        public PermissionEditViewModel()
        {
        }

        public PermissionEditViewModel(Translator translator)
            : base(translator, translator.Get("Permission.Edit.Title", "Title of the permission edit dialog", "Edit permission"), "permissionEditDialog")
        {
            PhraseFieldPart = translator.Get("Permission.Edit.Field.Part", "Part field in the permission edit dialog", "Part").EscapeHtml();
            PhraseFieldSubject = translator.Get("Permission.Edit.Field.Subject", "Subject field in the permission edit dialog", "Subject").EscapeHtml();
            PhraseFieldRight = translator.Get("Permission.Edit.Field.Right", "Right field in the permission edit dialog", "Right").EscapeHtml();
            Parts = new List<NamedIntViewModel>();
            Subjects = new List<NamedIntViewModel>();
            Rights = new List<NamedIntViewModel>();
        }

        public PermissionEditViewModel(Translator translator, IDatabase db, Role role)
            : this(translator)
        {
            Method = "add";
            Id = role.Id.Value.ToString();
            Part = string.Empty;
            Subject = string.Empty;
            Right = string.Empty;
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Anonymous, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Contact, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Crypto, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.CustomDefinitions, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Deleted, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Demography, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Documents, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Journal, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Mailings, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Subscription, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.RoleAssignments, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Structure, false));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.TagAssignments, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Group, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Feed, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SubFeed, false));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SystemWide, false));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Read, false));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Write, false));
        }

        public PermissionEditViewModel(Translator translator, IDatabase db, Permission permission)
            : this(translator)
        {
            Method = "edit";
            Id = permission.Id.ToString();
            Part = ((int)permission.Part.Value).ToString();
            Subject = ((int)permission.Subject.Value).ToString();
            Right = ((int)permission.Right.Value).ToString();
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Anonymous, permission.Part.Value == PartAccess.Anonymous));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Contact, permission.Part.Value == PartAccess.Contact));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Crypto, permission.Part.Value == PartAccess.Crypto));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.CustomDefinitions, permission.Part.Value == PartAccess.CustomDefinitions));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Deleted, permission.Part.Value == PartAccess.Deleted));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Demography, permission.Part.Value == PartAccess.Demography));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Documents, permission.Part.Value == PartAccess.Documents));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Journal, permission.Part.Value == PartAccess.Journal));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Mailings, permission.Part.Value == PartAccess.Mailings));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Subscription, permission.Part.Value == PartAccess.Subscription));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.RoleAssignments, permission.Part.Value == PartAccess.RoleAssignments));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.Structure, permission.Part.Value == PartAccess.Structure));
            Parts.Add(new NamedIntViewModel(translator, PartAccess.TagAssignments, permission.Part.Value == PartAccess.TagAssignments));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Group, permission.Subject.Value == SubjectAccess.Group));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.Feed, permission.Subject.Value == SubjectAccess.Feed));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SubFeed, permission.Subject.Value == SubjectAccess.SubFeed));
            Subjects.Add(new NamedIntViewModel(translator, SubjectAccess.SystemWide, permission.Subject.Value == SubjectAccess.SystemWide));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Read, permission.Right.Value == AccessRight.Read));
            Rights.Add(new NamedIntViewModel(translator, AccessRight.Write, permission.Right.Value == AccessRight.Write));
        }
    }

    public class PermissionViewModel : MasterViewModel
    {
        public string Id;

        public PermissionViewModel(Translator translator, Session session, Role role)
            : base(translator, 
            translator.Get("Permission.List.Title", "Title of the permission list page", "Permissions"), 
            session)
        {
            Id = role.Id.Value.ToString();
        }
    }

    public class PermissionListItemViewModel
    {
        public string Id;
        public string Part;
        public string Subject;
        public string Right;
        public string PhraseDeleteConfirmationQuestion;

        public PermissionListItemViewModel(Translator translator, Permission permission)
        {
            Id = permission.Id.Value.ToString();
            Part = permission.Part.Value.Translate(translator).EscapeHtml();
            Subject = permission.Subject.Value.Translate(translator).EscapeHtml();
            Right = permission.Right.Value.Translate(translator).EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Permission.List.Delete.Confirm.Question", "Delete permission confirmation question", "Do you really wish to delete permission {0}?", permission.GetText(translator)).EscapeHtml();
        }
    }

    public class PermissionListViewModel
    {
        public string Id;
        public string ParentId;
        public string PhraseHeaderRoleGroupFeed;
        public string PhraseHeaderPart;
        public string PhraseHeaderSubject;
        public string PhraseHeaderRight;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<PermissionListItemViewModel> List;
        public string Editable;
        public bool AddAccess;

        public PermissionListViewModel(Translator translator, IDatabase database, Session session, Role role)
        {
            Id = role.Id.Value.ToString();
            ParentId = role.Group.Value.Id.Value.ToString();
            PhraseHeaderRoleGroupFeed =
                translator.Get("Permission.List.Header.RoleGroupFeed", "Header part 'Feed' in the permission list", "{0} in {1} of {2}",
                role.Name.Value[translator.Language],
                role.Group.Value.Name.Value[translator.Language],
                role.Group.Value.Feed.Value.Name.Value[translator.Language]).EscapeHtml();
            PhraseHeaderPart = translator.Get("Permission.List.Header.Part", "Link 'Part' caption in the permission list", "Part").EscapeHtml();
            PhraseHeaderSubject = translator.Get("Permission.List.Header.Subject", "Link 'Subject' caption in the permission list", "Subject").EscapeHtml();
            PhraseHeaderRight = translator.Get("Permission.List.Header.Right", "Link 'Right' caption in the permission list", "Right").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Permission.List.Delete.Confirm.Title", "Delete permission confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
            List = new List<PermissionListItemViewModel>(
                database.Query<Permission>(DC.Equal("roleid", role.Id.Value))
                .Select(c => new PermissionListItemViewModel(translator, c))
                .OrderBy(c => c.Subject + "/" + c.Part + "/" + c.Right));
            AddAccess = session.HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Write);
            Editable = AddAccess ? "editable" : "accessdenied";
        }
    }

    public class PermissionEdit : PublicusModule
    {
        private bool IsPermissionPermitted(Permission permission)
        {
            switch (permission.Subject.Value)
            {
                case SubjectAccess.SystemWide:
                    return HasSystemWideAccess(PartAccess.Structure, AccessRight.Write);
                case SubjectAccess.Feed:
                case SubjectAccess.SubFeed:
                    return HasAccess(permission.Role.Value.Group.Value.Feed.Value, PartAccess.Structure, AccessRight.Write);
                case SubjectAccess.Group:
                    return HasAccess(permission.Role.Value.Group.Value, PartAccess.Structure, AccessRight.Write);
                default:
                    return false;
            }
        }

        public PermissionEdit()
        {
            this.RequiresAuthentication();

            Get["/permission/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);

                if (role != null)
                {
                    if (HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/permission.sshtml",
                            new PermissionViewModel(Translator, CurrentSession, role)];
                    }
                }

                return null;
            };
            Get["/permission/list/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);

                if (role != null)
                {
                    if (HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/permissionlist.sshtml",
                            new PermissionListViewModel(Translator, Database, CurrentSession, role)];
                    }
                }

                return null;
            };
            Get["/permission/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var permission = Database.Query<Permission>(idString);

                if (permission != null)
                {
                    if (HasAccess(permission.Role.Value.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/permissionedit.sshtml",
                            new PermissionEditViewModel(Translator, Database, permission)];
                    }
                }

                return null;
            };
            Post["/permission/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PermissionEditViewModel>(ReadBody());
                var permission = Database.Query<Permission>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(permission))
                {
                    if (status.HasAccess(permission.Role.Value.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignEnumIntString("Part", permission.Part, model.Part);
                        status.AssignEnumIntString("Subject", permission.Subject, model.Subject);
                        status.AssignEnumIntString("Right", permission.Right, model.Right);

                        if (status.IsSuccess)
                        {
                            if (IsPermissionPermitted(permission))
                            {
                                Database.Save(permission);
                            }
                            else
                            {
                                status.SetErrorAccessDenied(); 
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/permission/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);

                if (role != null)
                {
                    if (HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/permissionedit.sshtml",
                            new PermissionEditViewModel(Translator, Database, role)];
                    }
                }

                return null;
            };
            Post["/permission/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(role))
                {
                    if (status.HasAccess(role.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<PermissionEditViewModel>(ReadBody());
                        var permission = new Permission(Guid.NewGuid());
                        status.AssignEnumIntString("Part", permission.Part, model.Part);
                        status.AssignEnumIntString("Subject", permission.Subject, model.Subject);
                        status.AssignEnumIntString("Right", permission.Right, model.Right);
                        permission.Role.Value = role;

                        if (status.IsSuccess)
                        {
                            if (IsPermissionPermitted(permission))
                            {
                                Database.Save(permission);
                            }
                            else
                            {
                                status.SetErrorAccessDenied(); 
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/permission/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var permission = Database.Query<Permission>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(permission))
                {
                    if (status.HasAccess(permission.Role.Value.Group.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        permission.Delete(Database);
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

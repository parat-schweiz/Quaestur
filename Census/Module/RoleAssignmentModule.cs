using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Census
{
    public class RoleAssignmentEditByRoleViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string MasterRole;
        public List<NamedIdViewModel> MasterRoles;
        public string PhraseFieldMasterRole;

        public RoleAssignmentEditByRoleViewModel()
        { 
        }

        public RoleAssignmentEditByRoleViewModel(Translator translator)
            : base(translator, translator.Get("RoleAssignment.Edit.Title", "Title of the role assignment edit dialog", "Edit role assignment"), "roleAssignmentEditDialog")
        {
            PhraseFieldMasterRole = translator.Get("RoleAssignment.Edit.Field.MasterRole", "Master role field in the role assignment edit dialog", "MasterRole").EscapeHtml();
        }

        public RoleAssignmentEditByRoleViewModel(Translator translator, IDatabase db, Session session, Role role)
            : this(translator)
        {
            Method = "add";
            Id = role.Id.Value.ToString();
            MasterRole = string.Empty;
            MasterRoles = new List<NamedIdViewModel>(db
                .Query<MasterRole>()
                .Select(mr => new NamedIdViewModel(translator, mr, false))
                .OrderBy(mr => mr.Name));
        }
    }

    public class RoleAssignmentEditByContactViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Role;
        public List<NamedIdViewModel> Roles;
        public string PhraseFieldRole;

        public RoleAssignmentEditByContactViewModel()
        {
        }

        public RoleAssignmentEditByContactViewModel(Translator translator)
            : base(translator, translator.Get("RoleAssignment.Edit.Title", "Title of the role assignment edit dialog", "Edit role assignment"), "roleAssignmentEditDialog")
        {
            PhraseFieldRole = translator.Get("RoleAssignment.Edit.Field.Role", "Role field in the role assignment edit dialog", "Role").EscapeHtml();
        }
    }


    public class RoleAssignmentViewModel : MasterViewModel
    {
        public string Id;
        public string ParentId;

        public RoleAssignmentViewModel(Translator translator, Session session, Role role)
            : base(translator, 
            translator.Get("RoleAssignment.List.Title", "Title of the role assignment list page", "Role assignments"), 
            session)
        {
            Id = role.Id.Value.ToString();
            ParentId = role.Group.Value.Id.Value.ToString();
        }
    }

    public class RoleAssignmentListItemViewModel
    {
        public string Id;
        public string MasterRole;

        public RoleAssignmentListItemViewModel(Translator translator, RoleAssignment roleAssignment)
        {
            Id = roleAssignment.Id.Value.ToString();
            MasterRole = roleAssignment.MasterRole.Value.Name.Value[translator.Language].EscapeHtml();
        }
    }

    public class RoleAssignmentListViewModel
    {
        public string Id;
        public string ParentId;
        public string PhraseHeaderRoleGroupOrganization;
        public List<RoleAssignmentListItemViewModel> List;
        public string Editable;
        public bool AddAccess;

        public RoleAssignmentListViewModel(Translator translator, IDatabase database, Session session, Role role)
        {
            Id = role.Id.Value.ToString();
            ParentId = role.Group.Value.Id.Value.ToString();
            PhraseHeaderRoleGroupOrganization =
                translator.Get("RoleAssignment.List.Header.RoleGroupOrganization", "Header part 'Organization' in the permission list", "{0} in {1} of {2}",
                role.Name.Value[translator.Language],
                role.Group.Value.Name.Value[translator.Language],
                role.Group.Value.Organization.Value.Name.Value[translator.Language]).EscapeHtml();
            List = new List<RoleAssignmentListItemViewModel>(
                database.Query<RoleAssignment>(DC.Equal("roleid", role.Id.Value))
                .Select(ra => new RoleAssignmentListItemViewModel(translator, ra))
                .OrderBy(ra => ra.MasterRole));
            AddAccess = session.HasAccess(role.Group.Value, PartAccess.RoleAssignments, AccessRight.Write);
            Editable = AddAccess ? "editable" : "accessdenied";
        }
    }

    public class RoleAssignmentModule : CensusModule
    {
        public static bool IsAssingmentPermitted(Session session, Role role)
        {
            if (role.Permissions.Any(p => p.Subject.Value == SubjectAccess.SystemWide))
            {
                return session.HasSystemWideAccess(PartAccess.RoleAssignments, AccessRight.Write);
            }
            else if (role.Permissions.Any(p => p.Subject.Value == SubjectAccess.SubOrganization))
            {
                return session.HasAccess(role.Group.Value.Organization.Value, PartAccess.RoleAssignments, AccessRight.Write);
            }
            else if (role.Permissions.Any(p => p.Subject.Value == SubjectAccess.Organization))
            {
                return session.HasAccess(role.Group.Value.Organization.Value, PartAccess.RoleAssignments, AccessRight.Write);
            }
            else
            {
                return session.HasAccess(role.Group.Value, PartAccess.RoleAssignments, AccessRight.Write);
            }
        }

        private bool IsAssingmentPermitted(Role role)
        {
            return IsAssingmentPermitted(CurrentSession, role);
        }

        public RoleAssignmentModule()
        {
            this.RequiresAuthentication();

            Get("/roleassignment/{id}", parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);

                if (role != null)
                {
                    if (HasAccess(role.Group.Value, PartAccess.RoleAssignments, AccessRight.Read))
                    {
                        return View["View/roleAssignment.sshtml",
                            new RoleAssignmentViewModel(Translator, CurrentSession, role)];
                    }
                }

                return null;
            });
            Get("/roleassignment/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);

                if (role != null)
                {
                    if (HasAccess(role.Group.Value, PartAccess.RoleAssignments, AccessRight.Read))
                    {
                        return View["View/roleAssignmentlist.sshtml",
                           new RoleAssignmentListViewModel(Translator, Database, CurrentSession, role)];
                    }
                }

                return null;
            });
            Get("/roleassignment/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);

                if (role != null)
                {
                    if (HasAccess(role.Group.Value, PartAccess.RoleAssignments, AccessRight.Write))
                    {
                        return View["View/roleAssignmentedit_role.sshtml",
                            new RoleAssignmentEditByRoleViewModel(Translator, Database, CurrentSession, role)];
                    }
                }

                                return null;
            });
            Post("/roleassignment/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var role = Database.Query<Role>(idString);
                var status = CreateStatus();

                if (role != null)
                {
                    if (status.HasAccess(role.Group.Value, PartAccess.RoleAssignments, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<RoleAssignmentEditByRoleViewModel>(ReadBody());
                        var roleAssignment = new RoleAssignment(Guid.NewGuid());
                        status.AssignObjectIdString("MasterRole", roleAssignment.MasterRole, model.MasterRole);
                        roleAssignment.Role.Value = role;

                        if (status.IsSuccess)
                        {
                            if (IsAssingmentPermitted(roleAssignment.Role.Value))
                            {
                                Database.Save(roleAssignment);
                                Global.Log.Notice("{0} added role assingment from {1} to {2}",
                                    CurrentSession.User.UserName.Value,
                                    roleAssignment.Role.Value.Name.Value[Translator.Language],
                                    roleAssignment.MasterRole.Value.Name.Value[Translator.Language]);
                            }
                            else
                            {
                                status.SetErrorAccessDenied();
                            }
                        }
                    }
                }
                else
                {
                    var masterRole = Database.Query<MasterRole>(idString);

                    if (masterRole != null)
                    {
                        var model = JsonConvert.DeserializeObject<RoleAssignmentEditByContactViewModel>(ReadBody());
                        var roleAssignment = new RoleAssignment(Guid.NewGuid());
                        status.AssignObjectIdString("Role", roleAssignment.Role, model.Role);
                        roleAssignment.MasterRole.Value = masterRole;

                        if (status.HasAccess(roleAssignment.Role.Value.Group.Value, PartAccess.RoleAssignments, AccessRight.Write))
                        {
                            if (status.IsSuccess)
                            {
                                if (IsAssingmentPermitted(roleAssignment.Role.Value))
                                {
                                    Database.Save(roleAssignment);
                                    Global.Log.Notice("{0} added role assingment from {1} to {2}",
                                        CurrentSession.User.UserName.Value,
                                        roleAssignment.Role.Value.Name.Value[Translator.Language],
                                        roleAssignment.MasterRole.Value.Name.Value[Translator.Language]);
                                }
                                else
                                {
                                    status.SetErrorAccessDenied();
                                }
                            }
                        }
                    }
                    else
                    {
                        status.SetError("Error.Object.NotFound", "Error message when object not found", "Object not found.");
                    }
                }

                return status.CreateJsonData();
            });
            Get("/roleassignment/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var roleAssignment = Database.Query<RoleAssignment>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(roleAssignment))
                {
                    if (status.HasAccess(roleAssignment.Role.Value.Group.Value, PartAccess.RoleAssignments, AccessRight.Write))
                    {
                        if (IsAssingmentPermitted(roleAssignment.Role.Value))
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                roleAssignment.Delete(Database);

                                Global.Log.Notice("{0} removed role assingment from {1} to {2}",
                                    CurrentSession.User.UserName.Value,
                                    roleAssignment.Role.Value.Name.Value[Translator.Language],
                                    roleAssignment.MasterRole.Value.Name.Value[Translator.Language]);

                                transaction.Commit();
                            }
                        }
                        else
                        {
                            status.SetErrorAccessDenied();
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

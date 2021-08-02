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
    public class RoleAssignmentEditByRoleViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Person;
        public List<NamedIdViewModel> Persons;
        public string PhraseFieldPerson;

        public RoleAssignmentEditByRoleViewModel()
        { 
        }

        public RoleAssignmentEditByRoleViewModel(Translator translator)
            : base(translator, translator.Get("RoleAssignment.Edit.Title", "Title of the role assignment edit dialog", "Edit role assignment"), "roleAssignmentEditDialog")
        {
            PhraseFieldPerson = translator.Get("RoleAssignment.Edit.Field.Person", "Person field in the role assignment edit dialog", "Person").EscapeHtml();
        }

        public RoleAssignmentEditByRoleViewModel(Translator translator, IDatabase db, Session session, Role role)
            : this(translator)
        {
            Method = "add";
            Id = role.Id.Value.ToString();
            Person = string.Empty;
            Persons = new List<NamedIdViewModel>(db
                .Query<Person>()
                .Where(p => session.HasAccess(p, PartAccess.Anonymous, AccessRight.Read))
                .Select(p => new NamedIdViewModel(session, p, false))
                .OrderBy(p => p.Name));
        }
    }

    public class RoleAssignmentEditByPersonViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Role;
        public List<NamedIdViewModel> Roles;
        public string PhraseFieldRole;

        public RoleAssignmentEditByPersonViewModel()
        {
        }

        public RoleAssignmentEditByPersonViewModel(Translator translator)
            : base(translator, translator.Get("RoleAssignment.Edit.Title", "Title of the role assignment edit dialog", "Edit role assignment"), "roleAssignmentEditDialog")
        {
            PhraseFieldRole = translator.Get("RoleAssignment.Edit.Field.Role", "Role field in the role assignment edit dialog", "Role").EscapeHtml();
        }

        public RoleAssignmentEditByPersonViewModel(Translator translator, IDatabase db, Session session, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.Value.ToString();
            Role = string.Empty;
            Roles = new List<NamedIdViewModel>(db
                .Query<Role>()
                .Where(r => RoleAssignmentModule.IsAssingmentPermitted(session, r))
                .Select(r => new NamedIdViewModel(translator, r, false))
                .OrderBy(r => r.Name));
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
        public string Person;

        public RoleAssignmentListItemViewModel(Translator translator, RoleAssignment roleAssignment)
        {
            Id = roleAssignment.Id.Value.ToString();
            Person = roleAssignment.Person.Value.ShortHand.EscapeHtml();
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
                .OrderBy(ra => ra.Person));
            AddAccess = session.HasAccess(role.Group.Value, PartAccess.RoleAssignments, AccessRight.Write);
            Editable = AddAccess ? "editable" : "accessdenied";
        }
    }

    public class RoleAssignmentModule : QuaesturModule
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
            RequireCompleteLogin();

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

                return string.Empty;
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

                return string.Empty;
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

                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.RoleAssignments, AccessRight.Write))
                    {
                        return View["View/roleAssignmentedit_person.sshtml",
                            new RoleAssignmentEditByPersonViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
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
                        status.AssignObjectIdString("Person", roleAssignment.Person, model.Person);
                        roleAssignment.Role.Value = role;

                        if (status.IsSuccess)
                        {
                            if (IsAssingmentPermitted(roleAssignment.Role.Value))
                            {
                                Database.Save(roleAssignment);
                                Journal(roleAssignment.Person.Value,
                                    "RoleAssignment.Journal.Add",
                                    "Journal entry assigned role",
                                    "Assigned role {0}",
                                    t => roleAssignment.Role.Value.GetText(t));
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
                    var person = Database.Query<Person>(idString);

                    if (person != null)
                    {
                        if (status.HasAccess(person, PartAccess.RoleAssignments, AccessRight.Write))
                        {
                            var model = JsonConvert.DeserializeObject<RoleAssignmentEditByPersonViewModel>(ReadBody());
                            var roleAssignment = new RoleAssignment(Guid.NewGuid());
                            status.AssignObjectIdString("Role", roleAssignment.Role, model.Role);
                            roleAssignment.Person.Value = person;

                            if (status.IsSuccess)
                            {
                                if (IsAssingmentPermitted(roleAssignment.Role.Value))
                                {
                                    Database.Save(roleAssignment);
                                    Journal(roleAssignment.Person.Value,
                                        "RoleAssignment.Journal.Add",
                                        "Journal entry assigned role",
                                        "Assigned role {0}",
                                        t => roleAssignment.Role.Value.GetText(t));
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
                    if (status.HasAccess(roleAssignment.Person.Value, PartAccess.RoleAssignments, AccessRight.Write))
                    {
                        if (IsAssingmentPermitted(roleAssignment.Role.Value))
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                roleAssignment.Delete(Database);

                                Journal(roleAssignment.Person.Value,
                                    "RoleAssignment.Journal.Delete",
                                    "Journal entry removed role",
                                    "Removed role {0}",
                                    t => roleAssignment.Role.Value.GetText(t));

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

using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Hospes
{
    public class PersonDetailRoleAssignmentItemViewModel
    {
        public string Id;
        public string Name;

        public PersonDetailRoleAssignmentItemViewModel(Translator translator, RoleAssignment roleAssignment)
        {
            Id = roleAssignment.Id.Value.ToString();
            Name = roleAssignment.Role.Value.Group.Value.Organization.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   roleAssignment.Role.Value.Group.Value.Name.Value[translator.Language].EscapeHtml() + " / " +
                   roleAssignment.Role.Value.Name.Value[translator.Language].EscapeHtml();
        }
    }

    public class PersonDetailRoleAssignmentViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailRoleAssignmentItemViewModel> List;
        public string PhraseHeaderName;

        public PersonDetailRoleAssignmentViewModel(Translator translator, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailRoleAssignmentItemViewModel>(
                person.RoleAssignments
                .Select(m => new PersonDetailRoleAssignmentItemViewModel(translator, m))
                .OrderBy(m => m.Name));
            Editable =
                session.HasAccess(person, PartAccess.TagAssignments, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderName = translator.Get("Person.Detail.RoleAssignment.Header.Name", "Column 'Name' on the roleAssignment tab of the person detail page", "Name").EscapeHtml();
        }
    }

    public class PersonDetailRoleAssignmentModule : HospesModule
    {
        public PersonDetailRoleAssignmentModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/roleassignments/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.RoleAssignments, AccessRight.Read))
                    {
                        return View["View/persondetail_roleassignments.sshtml",
                            new PersonDetailRoleAssignmentViewModel(Translator, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class PersonDetailMembershipItemViewModel
    {
        public string Id;
        public string Organization;
        public string Type;
        public string Status;
        public string VotingRight;

        public PersonDetailMembershipItemViewModel(IDatabase database, Translator translator, Membership membership)
        {
            Id = membership.Id.Value.ToString();
            Type = membership.Type.Value.Name.Value[translator.Language].EscapeHtml();
            Organization = membership.Organization.Value.Name.Value[translator.Language].EscapeHtml();

            if (DateTime.Now.Date < membership.StartDate.Value.Date)
            {
                Status = translator.Get("Person.Detail.Membership.Status.NotYet", "Status 'Not active yet' on the membership tab in the person detail page", "Not active yet").EscapeHtml();
            }
            else if (!membership.EndDate.Value.HasValue)
            {
                Status = translator.Get("Person.Detail.Membership.Status.Active", "Status 'Active' on the membership tab in the person detail page", "Active").EscapeHtml();
            }
            else if (DateTime.Now.Date <= membership.EndDate.Value.Value.Date)
            {
                Status = translator.Get("Person.Detail.Membership.Status.Active", "Status 'Active' on the membership tab in the person detail page", "Active").EscapeHtml();
            }
            else
            {
                Status = translator.Get("Person.Detail.Membership.Status.Ended", "Status 'Ended' on the membership tab in the person detail page", "Ended").EscapeHtml();
            }

            if (!membership.HasVotingRight.Value.HasValue)
            {
                membership.UpdateVotingRight(database);
                database.Save(membership); 
            }

            VotingRight = membership.HasVotingRight.Value.Value ?
                translator.Get("Person.Detail.VotingRight.Yes", "Voting right 'Yes' on the membership tab in the person detail page", "Yes").EscapeHtml() :
                translator.Get("Person.Detail.VotingRight.No", "Voting right 'No' on the membership tab in the person detail page", "No").EscapeHtml();
        }
    }

    public class PersonDetailMembershipViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailMembershipItemViewModel> List;
        public string PhraseHeaderOrganization;
        public string PhraseHeaderType;
        public string PhraseHeaderStatus;
        public string PhraseHeaderVotingRight;

        public PersonDetailMembershipViewModel(IDatabase database, Translator translator, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailMembershipItemViewModel>(
                person.Memberships
                .Select(m => new PersonDetailMembershipItemViewModel(database, translator, m))
                .OrderBy(m => m.Organization));
            Editable =
                session.HasAccess(person, PartAccess.TagAssignments, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderOrganization = translator.Get("Person.Detail.Membership.Header.Organization", "Column 'Organization' on the membership tab of the person detail page", "Organization");
            PhraseHeaderType = translator.Get("Person.Detail.Membership.Header.Type", "Column 'Type' on the membership tab of the person detail page", "Type");
            PhraseHeaderStatus = translator.Get("Person.Detail.Membership.Header.Status", "Column 'Status' on the membership tab of the person detail page", "Status");
            PhraseHeaderVotingRight = translator.Get("Person.Detail.Membership.Header.VotingRight", "Column 'Voting right' on the membership tab of the person detail page", "Voting right");
        }
    }

    public class PersonDetailMembershipModule : QuaesturModule
    {
        public PersonDetailMembershipModule()
        {
            RequireCompleteLogin();

            Get["/person/detail/memberships/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Membership, AccessRight.Read))
                    {
                        return View["View/persondetail_memberships.sshtml", 
                            new PersonDetailMembershipViewModel(Database, Translator, CurrentSession, person)];
                    }
                }

                return null;
            };
        }
    }
}

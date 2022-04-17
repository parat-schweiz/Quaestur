using System;
using System.Linq;
using Nancy;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDetailViewModel : MasterViewModel
    {
        public string Id;
        public bool MasterReadable;
        public bool MembershipsReadable;
        public bool TagAssignmentReadable;
        public bool RoleAssignmentReadable;
        public bool DocumentReadable;
        public bool BillingReadable;
        public bool PointsReadable;
        public bool CreditsReadable;
        public bool JournalReadable;
        public bool SecurityReadable;
        public bool ActionsReadable;
        public string PhraseTabMaster;
        public string PhraseTabMemberships;
        public string PhraseTabTags;
        public string PhraseTabRoles;
        public string PhraseTabDocuments;
        public string PhraseTabBilling;
        public string PhraseTabPrepayment;
        public string PhraseTabPointsTally;
        public string PhraseTabPoints;
        public string PhraseTabCredits;
        public string PhraseTabJournal;
        public string PhraseTabSecurity;
        public string PhraseTabActions;

        public PersonDetailViewModel(Translator translator, Session session, Person person)
            : base(translator, 
            session.HasAccess(person, PartAccess.Demography, AccessRight.Read) ? person.ShortHand : person.UserName, 
            session)
        {
            Id = person.Id.ToString();
            PhraseTabMaster = translator.Get("Person.Detail.Tab.Master", "Tab 'Master data' in the person detail page", "Master data");
            PhraseTabMemberships = translator.Get("Person.Detail.Tab.Memberships", "Tab 'Memberships' in the person detail page", "Memberships");
            PhraseTabTags = translator.Get("Person.Detail.Tab.Tags", "Tab 'Tags' in the person detail page", "Tags");
            PhraseTabRoles = translator.Get("Person.Detail.Tab.Roles", "Tab 'Roles' in the person detail page", "Roles");
            PhraseTabDocuments = translator.Get("Person.Detail.Tab.Documents", "Tab 'Documents' in the person detail page", "Documents");
            PhraseTabBilling = translator.Get("Person.Detail.Tab.Billing", "Tab 'Billing' in the person detail page", "Billing");
            PhraseTabPrepayment = translator.Get("Person.Detail.Tab.Prepayment", "Tab 'Prepayment' in the person detail page", "Prepayment");
            PhraseTabPointsTally = translator.Get("Person.Detail.Tab.PointsTally", "Tab 'Points tally' in the person detail page", "Points tally");
            PhraseTabPoints = translator.Get("Person.Detail.Tab.Points", "Tab 'Points' in the person detail page", "Points");
            PhraseTabCredits = translator.Get("Person.Detail.Tab.Credits", "Tab 'Credits' in the person detail page", "Credits");
            PhraseTabJournal = translator.Get("Person.Detail.Tab.Journal", "Tab 'Journal' in the person detail page", "Journal");
            PhraseTabSecurity = translator.Get("Person.Detail.Tab.Security", "Tab 'Security' in the person detail page", "Security");
            PhraseTabActions = translator.Get("Person.Detail.Tab.Actions", "Tab 'Actions' in the person detail page", "Actions");
            MasterReadable = session.HasAccess(person, PartAccess.Demography, AccessRight.Read) ||
                             session.HasAccess(person, PartAccess.Contact, AccessRight.Read);
            MembershipsReadable = session.HasAccess(person, PartAccess.Membership, AccessRight.Read);
            TagAssignmentReadable = session.HasAccess(person, PartAccess.TagAssignments, AccessRight.Read);
            RoleAssignmentReadable = session.HasAccess(person, PartAccess.RoleAssignments, AccessRight.Read);
            DocumentReadable = session.HasAccess(person, PartAccess.Documents, AccessRight.Read);
            BillingReadable = session.HasAccess(person, PartAccess.Billing, AccessRight.Read);
            PointsReadable = session.HasAccess(person, PartAccess.Points, AccessRight.Read);
            CreditsReadable = session.HasAccess(person, PartAccess.Credits, AccessRight.Read);
            JournalReadable = session.HasAccess(person, PartAccess.Journal, AccessRight.Read);
            SecurityReadable = session.HasAccess(person, PartAccess.Security, AccessRight.Read);
            ActionsReadable = session.HasAccess(person, PartAccess.Billing, AccessRight.Extended);
        }
    }

    public class PersonDetailHeadViewModel
    {
        public string Id;
        public string Number;
        public string UserName;
        public string FullName;
        public string VotingRight;
        public string Editable;

        public string PhraseHeadNumber;
        public string PhraseHeadUserName;
        public string PhraseHeadFullName;
        public string PhraseHeadVotingRight;

        public PersonDetailHeadViewModel(IDatabase database, Translator translator, Session session, Person person)
        {
            var writeAccess = session.HasAccess(person, PartAccess.Demography, AccessRight.Read);

            Id = person.Id.ToString();
            UserName = person.UserName.Value.EscapeHtml();

            if (session.HasAccess(person, PartAccess.Demography, AccessRight.Read))
            {
                Number = person.Number.ToString();
                FullName = person.FullName.EscapeHtml();
            }
            else
            {
                Number = string.Empty;
                FullName = string.Empty;
            }

            if (session.HasAccess(person, PartAccess.Demography, AccessRight.Write))
            {
                Editable = "editable";
            }
            else
            {
                Editable = "accessdenied";
            }

            VotingRight = person.HasVotingRight(database, translator);

            PhraseHeadNumber = translator.Get("Person.Detail.Head.Header.Number", "Column 'Number' in the head of the person detail page", "#").EscapeHtml();
            PhraseHeadUserName = translator.Get("Person.Detail.Head.Header.UserName", "Column 'Username' in the head of the person detail page", "Username").EscapeHtml();
            PhraseHeadFullName = translator.Get("Person.Detail.Head.Header.FullName", "Column 'Full name' in the head of the person detail page", "Full name").EscapeHtml();
            PhraseHeadVotingRight = translator.Get("Person.Detail.Head.Header.VotingRight", "Column 'Voting right' in the head of the person detail page", "Voting right").EscapeHtml();
        }
    }

    public class PersonDetailModule : QuaesturModule
    {
        public PersonDetailModule()
        {
            RequireCompleteLogin();

            Get("/person/new", parameters =>
            {
                var organization = CurrentSession.User.RoleAssignments
                    .Select(ra => ra.Role.Value.Group.Value.Organization.Value)
                    .Where(o => HasAccess(o, PartAccess.Demography, AccessRight.Write))
                    .Where(o => HasAccess(o, PartAccess.Membership, AccessRight.Write))
                    .Where(o => HasAccess(o, PartAccess.Contact, AccessRight.Write))
                    .Where(o => o.MembershipTypes.Count > 0)
                    .OrderBy(o => o.Subordinates.Count())
                    .FirstOrDefault();

                if (organization != null)
                {
                    using (var transaction = Database.BeginTransaction())
                    {
                        var person = new Person(Guid.NewGuid());
                        var sequence = Database.Query<Sequence>().Single();
                        person.Number.Value = sequence.NextPersonNumber.Value;
                        sequence.NextPersonNumber.Value++;
                        Database.Save(sequence);
                        person.UserName.Value = "user" + person.Number.Value.ToString();

                        var membership = new Membership(Guid.NewGuid());
                        membership.Organization.Value = organization;
                        membership.Type.Value = organization.MembershipTypes
                            .OrderBy(Value)
                            .FirstOrDefault();
                        membership.Person.Value = person;

                        foreach (var tag in Database.Query<Tag>())
                        {
                            if (tag.Mode.Value.HasFlag(TagMode.Default))
                            {
                                var tagAssignment = new TagAssignment(Guid.NewGuid());
                                tagAssignment.Tag.Value = tag;
                                tagAssignment.Person.Value = person;
                            }
                        }

                        Database.Save(person);
                        Journal(person,
                            "Person.Journal.Add",
                            "Journal entry added person",
                            "Added person {0} with membership {1}",
                            t => person.GetText(t),
                            t => membership.GetText(t));

                        transaction.Commit();
                        return Response.AsRedirect("/person/detail/" + person.Id.Value.ToString());
                    }
                }
                return AccessDenied();
            });
            Get("/person/detail/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Anonymous, AccessRight.Read))
                    {
                        return View["View/persondetail.sshtml",
                            new PersonDetailViewModel(Translator, CurrentSession, person)];
                    }
                }

                return View["View/info.sshtml", new InfoViewModel(Translator,
                    Translate("Person.Detail.NotFound.Title", "Title of the message when person is not found", "Not found"),
                    Translate("Person.Detail.NotFound.Message", "Text of the message when person is not found", "No person was found."),
                    Translate("Person.Detail.NotFound.BackLink", "Link text of the message when person is not found", "Back"),
                    "/")];
            });
            Get("/person/detail/head/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Anonymous, AccessRight.Read))
                    {
                        return View["View/persondetail_head.sshtml", new PersonDetailHeadViewModel(Database, Translator, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }

        private static long Value(MembershipType type)
        {
            var value = 0L;
            value += type.Rights.Value.HasFlag(MembershipRight.Voting) ? (1 << 60) : 0;
            value += type.Payment.Value != PaymentModel.None ? (1 << 30) : 0;
            return value; 
        }
    }
}

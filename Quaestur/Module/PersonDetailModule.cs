using System;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

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
        public bool JournalReadable;
        public bool SecurityReadable;
        public string PhraseTabMaster;
        public string PhraseTabMemberships;
        public string PhraseTabTags;
        public string PhraseTabRoles;
        public string PhraseTabDocuments;
        public string PhraseTabBilling;
        public string PhraseTabJournal;
        public string PhraseTabSecurity;

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
            PhraseTabJournal = translator.Get("Person.Detail.Tab.Journal", "Tab 'Journal' in the person detail page", "Journal");
            PhraseTabSecurity = translator.Get("Person.Detail.Tab.Security", "Tab 'Security' in the person detail page", "Security");
            MasterReadable = session.HasAccess(person, PartAccess.Demography, AccessRight.Read) ||
                             session.HasAccess(person, PartAccess.Contact, AccessRight.Read);
            MembershipsReadable = session.HasAccess(person, PartAccess.Membership, AccessRight.Read);
            TagAssignmentReadable = session.HasAccess(person, PartAccess.TagAssignments, AccessRight.Read);
            RoleAssignmentReadable = session.HasAccess(person, PartAccess.RoleAssignments, AccessRight.Read);
            DocumentReadable = session.HasAccess(person, PartAccess.Documents, AccessRight.Read);
            BillingReadable = session.HasAccess(person, PartAccess.Billing, AccessRight.Read);
            JournalReadable = session.HasAccess(person, PartAccess.Journal, AccessRight.Read);
            SecurityReadable = session.HasAccess(person, PartAccess.Security, AccessRight.Read);
        }
    }

    public class DialogViewModel
    {
        public string Title;
        public string DialogId;
        public string ButtonId;
        public string PhraseButtonOk;
        public string PhraseButtonCancel;

        public DialogViewModel()
        { 
        }

        public DialogViewModel(Translator translator, string title, string dialogId)
        {
            PhraseButtonOk = translator.Get("Dialog.Button.OK", "Button 'OK' in any dialog", "OK").EscapeHtml();
            PhraseButtonCancel = translator.Get("Dialog.Button.Cancel", "Button 'Cancel' in any dialog", "Cancel").EscapeHtml();
            Title = title.EscapeHtml();
            DialogId = dialogId;
            ButtonId = dialogId + "Button";
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

            Get["/person/new"] = parameters =>
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
                    var person = new Person(Guid.NewGuid());
                    person.Number.Value = Database.Query<Person>().Max(p => p.Number.Value) + 1;
                    person.UserName.Value = "user" + person.Number.Value.ToString();

                    var membership = new Membership(Guid.NewGuid());
                    membership.Organization.Value = organization;
                    membership.Type.Value = organization.MembershipTypes.FirstOrDefault();
                    membership.Person.Value = person;

                    Database.Save(person);
                    Journal(person,
                        "Person.Journal.Add",
                        "Journal entry added person",
                        "Added person {0} with membership {1}",
                        t => person.GetText(t),
                        t => membership.GetText(t));

                    return Response.AsRedirect("/person/detail/" + person.Id.Value.ToString());
                }
                return AccessDenied();
            };
            Get["/person/detail/{id}"] = parameters =>
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
            };
            Get["/person/detail/head/{id}"] = parameters =>
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

                return null;
            };
        }
    }
}

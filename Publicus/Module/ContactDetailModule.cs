using System;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailViewModel : MasterViewModel
    {
        public string Id;
        public bool MasterReadable;
        public bool SubscriptionsReadable;
        public bool TagAssignmentReadable;
        public bool RoleAssignmentReadable;
        public bool DocumentReadable;
        public bool JournalReadable;
        public string PhraseTabMaster;
        public string PhraseTabSubscriptions;
        public string PhraseTabTags;
        public string PhraseTabRoles;
        public string PhraseTabDocuments;
        public string PhraseTabJournal;

        public ContactDetailViewModel(Translator translator, Session session, Contact contact)
            : base(translator, 
            session.HasAccess(contact, PartAccess.Demography, AccessRight.Read) ? contact.ShortHand : contact.Organization, 
            session)
        {
            Id = contact.Id.ToString();
            PhraseTabMaster = translator.Get("Contact.Detail.Tab.Master", "Tab 'Master data' in the contact detail page", "Master data");
            PhraseTabSubscriptions = translator.Get("Contact.Detail.Tab.Subscriptions", "Tab 'Subscriptions' in the contact detail page", "Subscriptions");
            PhraseTabTags = translator.Get("Contact.Detail.Tab.Tags", "Tab 'Tags' in the contact detail page", "Tags");
            PhraseTabRoles = translator.Get("Contact.Detail.Tab.Roles", "Tab 'Roles' in the contact detail page", "Roles");
            PhraseTabDocuments = translator.Get("Contact.Detail.Tab.Documents", "Tab 'Documents' in the contact detail page", "Documents");
            PhraseTabJournal = translator.Get("Contact.Detail.Tab.Journal", "Tab 'Journal' in the contact detail page", "Journal");
            MasterReadable = session.HasAccess(contact, PartAccess.Demography, AccessRight.Read) ||
                             session.HasAccess(contact, PartAccess.Contact, AccessRight.Read);
            SubscriptionsReadable = session.HasAccess(contact, PartAccess.Subscription, AccessRight.Read);
            TagAssignmentReadable = session.HasAccess(contact, PartAccess.TagAssignments, AccessRight.Read);
            RoleAssignmentReadable = session.HasAccess(contact, PartAccess.RoleAssignments, AccessRight.Read);
            DocumentReadable = session.HasAccess(contact, PartAccess.Documents, AccessRight.Read);
            JournalReadable = session.HasAccess(contact, PartAccess.Journal, AccessRight.Read);
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

    public class ContactDetailHeadViewModel
    {
        public string Id;
        public string Organization;
        public string FullName;
        public string VotingRight;
        public string Editable;

        public string PhraseHeadOrganization;
        public string PhraseHeadFullName;
        public string PhraseHeadVotingRight;

        public ContactDetailHeadViewModel(IDatabase database, Translator translator, Session session, Contact contact)
        {
            var writeAccess = session.HasAccess(contact, PartAccess.Demography, AccessRight.Read);

            Id = contact.Id.ToString();
            Organization = contact.Organization.Value.EscapeHtml();

            if (session.HasAccess(contact, PartAccess.Demography, AccessRight.Read))
            {
                FullName = contact.FullName.EscapeHtml();
            }
            else
            {
                FullName = string.Empty;
            }

            if (session.HasAccess(contact, PartAccess.Demography, AccessRight.Write))
            {
                Editable = "editable";
            }
            else
            {
                Editable = "accessdenied";
            }

            PhraseHeadOrganization = translator.Get("Contact.Detail.Head.Header.Organization", "Column 'Organization' in the head of the contact detail page", "Organization").EscapeHtml();
            PhraseHeadFullName = translator.Get("Contact.Detail.Head.Header.FullName", "Column 'Full name' in the head of the contact detail page", "Full name").EscapeHtml();
            PhraseHeadVotingRight = translator.Get("Contact.Detail.Head.Header.VotingRight", "Column 'Voting right' in the head of the contact detail page", "Voting right").EscapeHtml();
        }
    }

    public class ContactDetailModule : PublicusModule
    {
        public ContactDetailModule()
        {
            this.RequiresAuthentication();

            Get["/contact/new"] = parameters =>
            {
                var feed = CurrentSession.RoleAssignments
                    .Select(ra => ra.Role.Value.Group.Value.Feed.Value)
                    .Where(o => HasAccess(o, PartAccess.Demography, AccessRight.Write))
                    .Where(o => HasAccess(o, PartAccess.Subscription, AccessRight.Write))
                    .Where(o => HasAccess(o, PartAccess.Contact, AccessRight.Write))
                    .OrderBy(o => o.Subordinates.Count())
                    .FirstOrDefault();

                if (feed != null)
                {
                    var contact = new Contact(Guid.NewGuid());
                    contact.Organization.Value = "New Organization";

                    var subscription = new Subscription(Guid.NewGuid());
                    subscription.Feed.Value = feed;
                    subscription.Contact.Value = contact;

                    Database.Save(contact);
                    Journal(contact,
                        "Contact.Journal.Add",
                        "Journal entry added contact",
                        "Added contact {0} with subscription {1}",
                        t => contact.GetText(t),
                        t => subscription.GetText(t));

                    return Response.AsRedirect("/contact/detail/" + contact.Id.Value.ToString());
                }
                return AccessDenied();
            };
            Get["/contact/detail/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Anonymous, AccessRight.Read))
                    {
                        return View["View/contactdetail.sshtml",
                            new ContactDetailViewModel(Translator, CurrentSession, contact)];
                    }
                }

                return View["View/info.sshtml", new InfoViewModel(Translator,
                    Translate("Contact.Detail.NotFound.Title", "Title of the message when contact is not found", "Not found"),
                    Translate("Contact.Detail.NotFound.Message", "Text of the message when contact is not found", "No contact was found."),
                    Translate("Contact.Detail.NotFound.BackLink", "Link text of the message when contact is not found", "Back"),
                    "/")];
            };
            Get["/contact/detail/head/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Anonymous, AccessRight.Read))
                    {
                        return View["View/contactdetail_head.sshtml", new ContactDetailHeadViewModel(Database, Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

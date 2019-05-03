using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailSubscriptionItemViewModel
    {
        public string Id;
        public string Feed;
        public string Status;
        public string VotingRight;

        public ContactDetailSubscriptionItemViewModel(IDatabase database, Translator translator, Subscription subscription)
        {
            Id = subscription.Id.Value.ToString();
            Feed = subscription.Feed.Value.Name.Value[translator.Language].EscapeHtml();

            if (DateTime.Now.Date < subscription.StartDate.Value.Date)
            {
                Status = translator.Get("Contact.Detail.Subscription.Status.NotYet", "Status 'Not active yet' on the subscription tab in the contact detail page", "Not active yet").EscapeHtml();
            }
            else if (!subscription.EndDate.Value.HasValue)
            {
                Status = translator.Get("Contact.Detail.Subscription.Status.Active", "Status 'Active' on the subscription tab in the contact detail page", "Active").EscapeHtml();
            }
            else if (DateTime.Now.Date <= subscription.EndDate.Value.Value.Date)
            {
                Status = translator.Get("Contact.Detail.Subscription.Status.Active", "Status 'Active' on the subscription tab in the contact detail page", "Active").EscapeHtml();
            }
            else
            {
                Status = translator.Get("Contact.Detail.Subscription.Status.Ended", "Status 'Ended' on the subscription tab in the contact detail page", "Ended").EscapeHtml();
            }
        }
    }

    public class ContactDetailSubscriptionViewModel
    {
        public string Id;
        public string Editable;
        public List<ContactDetailSubscriptionItemViewModel> List;
        public string PhraseHeaderFeed;
        public string PhraseHeaderStatus;
        public string PhraseHeaderVotingRight;

        public ContactDetailSubscriptionViewModel(IDatabase database, Translator translator, Session session, Contact contact)
        {
            Id = contact.Id.Value.ToString();
            List = new List<ContactDetailSubscriptionItemViewModel>(
                contact.Subscriptions
                .Select(m => new ContactDetailSubscriptionItemViewModel(database, translator, m))
                .OrderBy(m => m.Feed));
            Editable =
                session.HasAccess(contact, PartAccess.TagAssignments, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderFeed = translator.Get("Contact.Detail.Subscription.Header.Feed", "Column 'Feed' on the subscription tab of the contact detail page", "Feed");
            PhraseHeaderStatus = translator.Get("Contact.Detail.Subscription.Header.Status", "Column 'Status' on the subscription tab of the contact detail page", "Status");
            PhraseHeaderVotingRight = translator.Get("Contact.Detail.Subscription.Header.VotingRight", "Column 'Voting right' on the subscription tab of the contact detail page", "Voting right");
        }
    }

    public class ContactDetailSubscriptionModule : PublicusModule
    {
        public ContactDetailSubscriptionModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/subscriptions/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Subscription, AccessRight.Read))
                    {
                        return View["View/contactdetail_subscriptions.sshtml", 
                            new ContactDetailSubscriptionViewModel(Database, Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

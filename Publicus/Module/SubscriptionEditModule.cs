using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class SubscriptionEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Feed;
        public string StartDate;
        public string EndDate;
        public List<NamedIdViewModel> Feeds;
        public string PhraseFieldFeed;
        public string PhraseFieldStartDate;
        public string PhraseFieldEndDate;

        public SubscriptionEditViewModel()
        { 
        }

        public SubscriptionEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Subscription.Edit.Title", "Title of the edit subscription dialog", "Edit subscription"), 
                   "subscriptionEditDialog")
        {
            PhraseFieldFeed = translator.Get("Subscription.Edit.Field.Feed", "Field 'Feed' in the edit subscription dialog", "Feed").EscapeHtml();
            PhraseFieldStartDate = translator.Get("Subscription.Edit.Field.StartDate", "Field 'Start date' in the edit subscription dialog", "Start date").EscapeHtml();
            PhraseFieldEndDate = translator.Get("Subscription.Edit.Field.EndDate", "Field 'End date' in the edit subscription dialog", "End date").EscapeHtml();
            Feeds = new List<NamedIdViewModel>();
        }

        public SubscriptionEditViewModel(Translator translator, IDatabase db, Contact contact)
            : this(translator)
        {
            Method = "add";
            Id = contact.Id.ToString();
            Feed = string.Empty;
            StartDate = string.Empty;
            EndDate = string.Empty;
            Feeds.AddRange(
                db.Query<Feed>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
        }

        public SubscriptionEditViewModel(Translator translator, IDatabase db, Subscription subscription)
            : this(translator)
        {
            Method = "edit";
            Id = subscription.Id.ToString();
            Feed = subscription.Feed.Value.Name.Value[translator.Language].EscapeHtml();
            StartDate = subscription.StartDate.Value.ToString("dd.MM.yyyy");
            EndDate =
                subscription.EndDate.Value.HasValue ?
                subscription.EndDate.Value.Value.ToString("dd.MM.yyyy") :
                string.Empty;
            Feeds.AddRange(
                db.Query<Feed>()
                .Select(o => new NamedIdViewModel(translator, o, o == subscription.Feed))
                .OrderBy(o => o.Name));
        }
    }

    public class SubscriptionEdit : PublicusModule
    {
        public SubscriptionEdit()
        {
            this.RequiresAuthentication();

            Get["/subscription/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var subscription = Database.Query<Subscription>(idString);

                if (subscription != null)
                {
                    if (HasAccess(subscription.Contact.Value, PartAccess.Subscription, AccessRight.Write))
                    {
                        return View["View/subscriptionedit.sshtml",
                            new SubscriptionEditViewModel(Translator, Database, subscription)];
                    }
                }

                return null;
            };
            Post["/subscription/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<SubscriptionEditViewModel>(ReadBody());
                var subscription = Database.Query<Subscription>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(subscription))
                {
                    if (status.HasAccess(subscription.Contact.Value, PartAccess.Subscription, AccessRight.Write))
                    {
                        status.AssignObjectIdString("Feed", subscription.Feed, model.Feed);
                        status.AssignDateString("StartDate", subscription.StartDate, model.StartDate);
                        status.AssignDateString("EndDate", subscription.EndDate, model.EndDate);

                        if (status.IsSuccess)
                        {
                            Database.Save(subscription);
                            Journal(subscription.Contact.Value,
                                "Subscription.Journal.Edit",
                                "Journal entry edited subscription",
                                "Change subscription {0}",
                                t => subscription.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/subscription/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Subscription, AccessRight.Write))
                    {
                        return View["View/subscriptionedit.sshtml",
                            new SubscriptionEditViewModel(Translator, Database, contact)];
                    }
                }

                return null;
            };
            Post["/subscription/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<SubscriptionEditViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    if (status.HasAccess(contact, PartAccess.Subscription, AccessRight.Write))
                    {
                        var subscription = new Subscription(Guid.NewGuid());
                        status.AssignObjectIdString("Feed", subscription.Feed, model.Feed);
                        status.AssignDateString("StartDate", subscription.StartDate, model.StartDate);
                        status.AssignDateString("EndDate", subscription.EndDate, model.EndDate);
                        subscription.Contact.Value = contact;

                        if (status.IsSuccess)
                        {
                            Database.Save(subscription);
                            Journal(subscription.Contact.Value,
                                "Subscription.Journal.Add",
                                "Journal entry addded subscription",
                                "Added subscription {0}",
                                t => subscription.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/subscription/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var subscription = Database.Query<Subscription>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(subscription))
                {
                    if (status.HasAccess(subscription.Contact.Value, PartAccess.Subscription, AccessRight.Write))
                    {
                        subscription.Delete(Database);
                        Journal(subscription.Contact.Value,
                            "Subscription.Journal.Delete",
                            "Journal entry removed subscription",
                            "Removed subscription {0}",
                            t => subscription.GetText(t));
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

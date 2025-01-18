using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class SubscriptionEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string MembershipType;
        public List<NamedIdViewModel> MembershipTypes;
        public string MembershipTypeEditable;
        public string Tag;
        public List<NamedIdViewModel> Tags;
        public string SenderGroup;
        public List<NamedIdViewModel> SenderGroups;
        public List<NamedIdViewModel> SubscribePrePages;
        public string[] SubscribePrePageTemplates;
        public List<NamedIdViewModel> SubscribePostPages;
        public string[] SubscribePostPageTemplates;
        public List<NamedIdViewModel> SubscribeMails;
        public string[] SubscribeMailTemplates;
        public List<NamedIdViewModel> UnsubscribePrePages;
        public string[] UnsubscribePrePageTemplates;
        public List<NamedIdViewModel> UnsubscribePostPages;
        public string[] UnsubscribePostPageTemplates;
        public List<NamedIdViewModel> JoinPrePages;
        public string[] JoinPrePageTemplates;
        public List<NamedIdViewModel> JoinPages;
        public string[] JoinPageTemplates;
        public List<NamedIdViewModel> JoinPostPages;
        public string[] JoinPostPageTemplates;
        public List<NamedIdViewModel> JoinConfirmMails;
        public string[] JoinConfirmMailTemplates;
        public List<NamedIdViewModel> ConfirmMailPages;
        public string[] ConfirmMailPageTemplates;
        public List<NamedIdViewModel> PageHeaders;
        public string[] PageHeaderTemplates;
        public List<NamedIdViewModel> PageFooters;
        public string[] PageFooterTemplates;

        public string PhraseFieldMembershipType;
        public string PhraseFieldTag;
        public string PhraseFieldSenderGroup;
        public string PhraseFieldSubscribePrePageTemplates;
        public string PhraseFieldSubscribePostPageTemplates;
        public string PhraseFieldSubscribeMailTemplates;
        public string PhraseFieldUnsubscribePrePageTemplates;
        public string PhraseFieldUnsubscribePostPageTemplates;
        public string PhraseFieldJoinPrePageTemplates;
        public string PhraseFieldJoinPageTemplates;
        public string PhraseFieldJoinPostPageTemplates;
        public string PhraseFieldJoinConfirmMailTemplates;
        public string PhraseFieldConfirmMailPageTemplates;
        public string PhraseFieldPageHeaderTemplates;
        public string PhraseFieldPageFooterTemplates;

        public SubscriptionEditViewModel()
        { 
        }

        public SubscriptionEditViewModel(Translator translator)
            : base(translator, translator.Get("Subscription.Edit.Title", "Title of the subscription edit dialog", "Subscription"), "subscriptionEditDialog")
        {
            PhraseFieldMembershipType = translator.Get("Subscription.Edit.Field.MembershipType", "Membership type in the subscription edit dialog", "Membership").EscapeHtml();
            PhraseFieldTag = translator.Get("Subscription.Edit.Field.Tag", "Tag in the subscription edit dialog", "Tag").EscapeHtml();
            PhraseFieldSenderGroup = translator.Get("Subscription.Edit.Field.SenderGroup", "Sender group in the subscription edit dialog", "Sender group").EscapeHtml();
            PhraseFieldSubscribePrePageTemplates = translator.Get("Subscription.Edit.Field.SubscribePrePageTemplates", "Subscribe pre pages in the subscription edit dialog", "Subscribe pre pages").EscapeHtml();
            PhraseFieldSubscribePostPageTemplates = translator.Get("Subscription.Edit.Field.SubscribePostPageTemplates", "Subscribe post pages in the subscription edit dialog", "Subscribe post pages").EscapeHtml();
            PhraseFieldSubscribeMailTemplates = translator.Get("Subscription.Edit.Field.SubscribeMailTemplates", "Subscribe mails in the subscription edit dialog", "Subscribe mails").EscapeHtml();
            PhraseFieldUnsubscribePrePageTemplates = translator.Get("Unsubscription.Edit.Field.UnsubscribePrePageTemplates", "Unsubscribe pre pages in the subscription edit dialog", "Unsubscribe pre pages").EscapeHtml();
            PhraseFieldUnsubscribePostPageTemplates = translator.Get("Unsubscription.Edit.Field.UnsubscribePostPageTemplates", "Unsubscribe post pages in the subscription edit dialog", "Unsubscribe post pages").EscapeHtml();
            PhraseFieldJoinPrePageTemplates = translator.Get("Unsubscription.Edit.Field.JoinPrePageTemplates", "Join pre pages in the subscription edit dialog", "Join pre pages").EscapeHtml();
            PhraseFieldJoinPageTemplates = translator.Get("Unsubscription.Edit.Field.JoinPageTemplates", "Join pages in the subscription edit dialog", "Join pages").EscapeHtml();
            PhraseFieldJoinPostPageTemplates = translator.Get("Unsubscription.Edit.Field.JoinPostPageTemplates", "Join post pages in the subscription edit dialog", "Join post pages").EscapeHtml();
            PhraseFieldJoinConfirmMailTemplates = translator.Get("Unsubscription.Edit.Field.JoinConfirmMailTemplates", "Join confirm mails in the subscription edit dialog", "Join confirm mails mails").EscapeHtml();
            PhraseFieldConfirmMailPageTemplates = translator.Get("Unsubscription.Edit.Field.ConfirmMailPageTemplates", "Confirm mail pages in the subscription edit dialog", "Confirm mail pages").EscapeHtml();
            PhraseFieldPageHeaderTemplates = translator.Get("Unsubscription.Edit.Field.PageHeaderTemplates", "Page headers in the subscription edit dialog", "Page headers").EscapeHtml();
            PhraseFieldPageFooterTemplates = translator.Get("Unsubscription.Edit.Field.PageFooterTemplates", "Page footers in the subscription edit dialog", "Page footers").EscapeHtml();
        }

        public SubscriptionEditViewModel(Translator translator, IDatabase database, Session session)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Subscription.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", new MultiLanguageString());
            MembershipType = string.Empty;
            MembershipTypeEditable = string.Empty;
            Tag = string.Empty;
            MembershipTypes = database
                .Query<MembershipType>()
                .Select(m => new NamedIdViewModel(translator, m.Organization.Value, m, false))
                .OrderBy(t => t.Name)
                .ToList();
            Tags = database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name)
                .ToList();
            SenderGroups = database
                .Query<Group>()
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(t => t.Name)
                .ToList();
            var mailTemplates = database
                .Query<MailTemplate>()
                .Where(t => t.AssignmentType.Value == TemplateAssignmentType.Subscription)
               .Select(t => new NamedIdViewModel(translator, t, false))
               .OrderBy(t => t.Name)
               .ToList();
            var pageTemplates = database
                .Query<PageTemplate>()
                .Where(t => t.AssignmentType.Value == TemplateAssignmentType.Subscription)
               .Select(t => new NamedIdViewModel(translator, t, false))
               .OrderBy(t => t.Name)
               .ToList();
            SubscribePrePages = pageTemplates.ToList();
            SubscribePostPages = pageTemplates.ToList();
            SubscribeMails = mailTemplates.ToList();
            UnsubscribePrePages = pageTemplates.ToList();
            UnsubscribePostPages = pageTemplates.ToList();
            JoinPrePages = pageTemplates.ToList();
            JoinPages = pageTemplates.ToList();
            JoinPostPages = pageTemplates.ToList();
            JoinConfirmMails = mailTemplates.ToList();
            ConfirmMailPages = pageTemplates.ToList();
            PageHeaders = pageTemplates.ToList();
            PageFooters = pageTemplates.ToList();
        }

        public SubscriptionEditViewModel(Translator translator, IDatabase database, Session session, Subscription subscription)
            : this(translator)
        {
            Method = "edit";
            Id = subscription.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Subscription.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", subscription.Name.Value);
            MembershipType = string.Empty;
            MembershipTypeEditable = "disabled";
            Tag = string.Empty;
            var organization = subscription.Membership.Value.Organization.Value;
            MembershipTypes = database
                .Query<MembershipType>()
                .Where(m => m.Organization.Value == organization)
                .Select(m => new NamedIdViewModel(translator, m.Organization.Value, m, m == subscription.Membership.Value))
                .OrderBy(x => x.Name)
                .ToList();
            Tags = database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, t == subscription.Tag.Value))
                .OrderBy(x => x.Name)
                .ToList();
            SenderGroups = database
                .Query<Group>()
                .Where(g => g.Organization.Value == organization)
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(x => x.Name)
                .ToList();
            var pageTemplates = database
                .Query<PageTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.Subscription)
                .ToList();
            var mailTemplates = database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.Subscription)
                .ToList();
            SubscribePrePages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.SubscribePrePages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            SubscribePostPages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.SubscribePostPages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            SubscribeMails = mailTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.SubscribeMails.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            UnsubscribePrePages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.UnsubscribePrePages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            UnsubscribePostPages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.UnsubscribePostPages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            JoinPrePages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.JoinPrePages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            JoinPages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.JoinPages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            JoinPostPages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.JoinPostPages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            JoinConfirmMails = mailTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.JoinConfirmMails.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            ConfirmMailPages = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.ConfirmMailPages.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            PageHeaders = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.PageHeaders.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
            PageFooters = pageTemplates
                .Select(t => new NamedIdViewModel(translator, t, subscription.PageFooters.Values(database).Contains(t)))
                .OrderBy(t => t.Name)
                .ToList();
        }
    }

    public class SubscriptionViewModel : MasterViewModel
    {
        public SubscriptionViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, 
            translator.Get("Subscription.List.Title", "Title of the subscription list page", "Subscriptions"), 
            session)
        {
        }
    }

    public class SubscriptionListItemViewModel
    {
        public string Id;
        public string Name;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public SubscriptionListItemViewModel(Translator translator, Session session, Subscription subscription)
        {
            Id = subscription.Id.Value.ToString();
            Name = subscription.Name.Value[translator.Language].EscapeHtml();
            Editable =
                session.HasAccess(subscription.Membership.Value.Organization.Value, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Subscription.List.Delete.Confirm.Question", "Delete subscription confirmation question", "Do you really wish to delete subscription {0}?", subscription.GetText(translator)).EscapeHtml();
        }
    }

    public class SubscriptionListViewModel
    {
        public bool AddAccess;
        public string PhraseHeaderName;
        public string PhraseDeleteConfirmationTitle;
        public List<SubscriptionListItemViewModel> List;

        public SubscriptionListViewModel(Translator translator, IDatabase database, Session session)
        {
            AddAccess = session.HasAnyOrganizationAccess(PartAccess.Structure, AccessRight.Write);
            PhraseHeaderName = translator.Get("Subscription.List.Header.Name", "Header part 'Name' caption in the subscription list", "Name").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Subscription.List.Delete.Confirm.Title", "Delete subscription confirmation title", "Delete?").EscapeHtml();
            List = new List<SubscriptionListItemViewModel>(database
                .Query<Subscription>()
                .Where(s => session.HasAccess(s.Membership.Value.Organization.Value, PartAccess.Structure, AccessRight.Read))
                .Select(s => new SubscriptionListItemViewModel(translator, session, s))
                .OrderBy(s => s.Name));
        }
    }

    public class SubscriptionEditModule : QuaesturModule
    {
        public SubscriptionEditModule()
        {
            RequireCompleteLogin();

            Get("/subscription", parameters =>
            {
                return View["View/subscription.sshtml",
                    new SubscriptionViewModel(Database, Translator, CurrentSession)];
            });
            Get("/subscription/list", parameters =>
            {
                return View["View/subscriptionlist.sshtml",
                    new SubscriptionListViewModel(Translator, Database, CurrentSession)];
            });
            Get("/subscription/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var subscription = Database.Query<Subscription>(idString);

                if (subscription != null)
                {
                    return View["View/subscriptionedit.sshtml",
                        new SubscriptionEditViewModel(Translator, Database, CurrentSession, subscription)];
                }

                return string.Empty;
            });
            base.Post("/subscription/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<SubscriptionEditViewModel>(ReadBody());
                var subscription = Database.Query<Subscription>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(subscription))
                {
                    if (status.HasAccess(subscription.Membership.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        UpdateFields(status, model, subscription);

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(subscription);
                                UpdateTemplatesAndParameters(model, subscription, status);

                                if (status.IsSuccess)
                                {
                                    transaction.Commit();
                                    Notice("{0} changed subscription {1}", CurrentSession.User.ShortHand, subscription);
                                }
                                else
                                {
                                    transaction.Rollback();
                                }
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/subscription/add", parameters =>
            {
                if (HasAnyOrganizationAccess(PartAccess.Structure, AccessRight.Write))
                {
                    return View["View/subscriptionedit.sshtml",
                        new SubscriptionEditViewModel(Translator, Database, CurrentSession)];
                }

                return string.Empty;
            });
            Post("/subscription/add/new", parameters =>
            {
                var status = CreateStatus();

                if (status.HasAnyOrganizationAccess(PartAccess.Structure, AccessRight.Write))
                {
                    var model = JsonConvert.DeserializeObject<SubscriptionEditViewModel>(ReadBody());
                    var subscription = new Subscription(Guid.NewGuid());
                    status.AssignObjectIdString("MembershipType", subscription.Membership, model.MembershipType);
                    UpdateFields(status, model, subscription);

                    if (status.IsSuccess)
                    {
                        if (status.HasAccess(subscription.Membership.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(subscription);
                                UpdateTemplatesAndParameters(model, subscription, status);

                                if (status.IsSuccess)
                                {
                                    transaction.Commit();
                                    Notice("{0} added subscription {1}", CurrentSession.User.ShortHand, subscription);
                                }
                                else
                                {
                                    transaction.Rollback();
                                }
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/subscription/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var subscription = Database.Query<Subscription>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(subscription))
                {
                    if (status.HasAccess(subscription.Membership.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            subscription.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted subscription type {1}", CurrentSession.User.ShortHand, subscription);
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }

        private void UpdateTemplatesAndParameters(SubscriptionEditViewModel model, Subscription subscription, PostStatus status)
        {
            status.UpdateTemplates(Database, subscription.SubscribePrePages, model.SubscribePrePageTemplates);
            status.UpdateTemplates(Database, subscription.SubscribePostPages, model.SubscribePostPageTemplates);
            status.UpdateTemplates(Database, subscription.SubscribeMails, model.SubscribeMailTemplates);
            status.UpdateTemplates(Database, subscription.UnsubscribePrePages, model.UnsubscribePrePageTemplates);
            status.UpdateTemplates(Database, subscription.UnsubscribePostPages, model.UnsubscribePostPageTemplates);
            status.UpdateTemplates(Database, subscription.JoinPrePages, model.JoinPrePageTemplates);
            status.UpdateTemplates(Database, subscription.JoinPages, model.JoinPageTemplates);
            status.UpdateTemplates(Database, subscription.JoinPostPages, model.JoinPostPageTemplates);
            status.UpdateTemplates(Database, subscription.JoinConfirmMails, model.JoinConfirmMailTemplates);
            status.UpdateTemplates(Database, subscription.ConfirmMailPages, model.ConfirmMailPageTemplates);
            status.UpdateTemplates(Database, subscription.PageHeaders, model.PageHeaderTemplates);
            status.UpdateTemplates(Database, subscription.PageFooters, model.PageFooterTemplates);
        }

        private static void UpdateFields(PostStatus status, SubscriptionEditViewModel model, Subscription subscription)
        {
            status.AssignObjectIdString("MembershipType", subscription.Membership, model.MembershipType);
            status.AssignMultiLanguageRequired("Name", subscription.Name, model.Name);
            status.AssignObjectIdString("Tag", subscription.Tag, model.Tag);
            status.AssignObjectIdString("SenderGroup", subscription.SenderGroup, model.SenderGroup);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Hospes
{
    public class MembershipTypeEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string SenderGroup;
        public List<NamedIdViewModel> Groups;
        public string PhraseFieldSenderGroup;

        public MembershipTypeEditViewModel()
        { 
        }

        public MembershipTypeEditViewModel(Translator translator)
            : base(translator, translator.Get("MembershipType.Edit.Title", "Title of the membership type edit dialog", "Edit membership type"), "membershipTypeEditDialog")
        {
            PhraseFieldSenderGroup = translator.Get("MembershipType.Edit.Field.SenderGroup", "Sender group field in the membership type edit dialog", "Sender group").EscapeHtml();
        }

        public MembershipTypeEditViewModel(Translator translator, IDatabase database, Session session, Organization organization)
            : this(translator)
        {
            Method = "add";
            Id = organization.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", new MultiLanguageString());
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Structure, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(g => g.Name));
        }

        public MembershipTypeEditViewModel(Translator translator, IDatabase database, Session session, MembershipType membershipType)
            : this(translator)
        {
            Method = "edit";
            Id = membershipType.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", membershipType.Name.Value);
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Structure, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, membershipType.SenderGroup.Value == g))
                .OrderBy(g => g.Name));
        }
    }

    public class MembershipTypeViewModel : MasterViewModel
    {
        public string Id;

        public MembershipTypeViewModel(Translator translator, Session session, Organization organization)
            : base(translator, 
            translator.Get("MembershipType.List.Title", "Title of the membership type list page", "Membership type"), 
            session)
        {
            Id = organization.Id.Value.ToString();
        }
    }

    public class MembershipTypeListItemViewModel
    {
        public string Id;
        public string Name;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public MembershipTypeListItemViewModel(Translator translator, Session session, MembershipType membershipType)
        {
            Id = membershipType.Id.Value.ToString();
            Name = membershipType.Name.Value[translator.Language].EscapeHtml();
            Editable =
                session.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("MembershipType.List.Delete.Confirm.Question", "Delete membershipType confirmation question", "Do you really wish to delete membershipType {0}?", membershipType.GetText(translator)).EscapeHtml();
        }
    }

    public class MembershipTypeListViewModel
    {
        public string Id;
        public string Name;
        public string PhraseHeaderOrganization;
        public string PhraseHeaderPaymentParameters;
        public string PhraseHeaderBillTemplates;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<MembershipTypeListItemViewModel> List;
        public bool AddAccess;

        public MembershipTypeListViewModel(Translator translator, IDatabase database, Session session, Organization organization)
        {
            Id = organization.Id.Value.ToString();
            Name = organization.Name.Value[translator.Language].EscapeHtml();
            PhraseHeaderOrganization = translator.Get("MembershipType.List.Header.Organization", "Header part 'Organization' in the membership type list", "Organization").EscapeHtml();
            PhraseHeaderPaymentParameters = translator.Get("MembershipType.List.Header.PaymentParameters", "Link 'Payment parameters' caption in the membership type list", "Payment parameters").EscapeHtml();
            PhraseHeaderBillTemplates =
                session.HasAccess(organization, PartAccess.Structure, AccessRight.Write) ?
                translator.Get("MembershipType.List.Header.BillTemplates", "Link 'Bill templates' caption in the membership type list", "Bill templates").EscapeHtml() :
                string.Empty;
            PhraseDeleteConfirmationTitle = translator.Get("MembershipType.List.Delete.Confirm.Title", "Delete membership type confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("MembershipType.List.Delete.Confirm.Info", "Delete membership type confirmation info", "This will also delete all roles and permissions under that membershipType.").EscapeHtml();
            List = new List<MembershipTypeListItemViewModel>(
                organization.MembershipTypes
                .Where(mt => session.HasAccess(mt.Organization.Value, PartAccess.Structure, AccessRight.Read))
                .Select(mt => new MembershipTypeListItemViewModel(translator, session, mt))
                .OrderBy(mt => mt.Name));
            AddAccess = session.HasAccess(organization, PartAccess.Structure, AccessRight.Write);
        }
    }

    public class MembershipTypeEditModule : QuaesturModule
    {
        public MembershipTypeEditModule()
        {
            RequireCompleteLogin();

            Get("/membershiptype/{id}", parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);

                if (organization != null)
                {
                    return View["View/membershiptype.sshtml",
                        new MembershipTypeViewModel(Translator, CurrentSession, organization)];
                }

                return string.Empty;
            });
            Get("/membershiptype/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);

                if (organization != null)
                {
                    return View["View/membershiptypelist.sshtml",
                        new MembershipTypeListViewModel(Translator, Database, CurrentSession, organization)];
                }

                return string.Empty;
            });
            Get("/membershiptype/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);

                if (membershipType != null)
                {
                    return View["View/membershiptypeedit.sshtml",
                        new MembershipTypeEditViewModel(Translator, Database, CurrentSession, membershipType)];
                }

                return string.Empty;
            });
            base.Post("/membershiptype/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MembershipTypeEditViewModel>(ReadBody());
                var membershipType = Database.Query<MembershipType>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membershipType))
                {
                    if (status.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        Updatefields(status, model, membershipType);

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(membershipType);

                                if (status.IsSuccess)
                                {
                                    transaction.Commit();
                                    Notice("{0} changed membership type {1}", CurrentSession.User.ShortHand, membershipType);
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
            Get("/membershiptype/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);

                if (organization != null)
                {
                    if (HasAccess(organization, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/membershipTypeedit.sshtml",
                            new MembershipTypeEditViewModel(Translator, Database, CurrentSession, organization)];
                    }
                }

                return string.Empty;
            });
            base.Post("/membershiptype/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(organization))
                {
                    if (status.HasAccess(organization, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<MembershipTypeEditViewModel>(ReadBody());
                        var membershipType = new MembershipType(Guid.NewGuid());
                        Updatefields(status, model, membershipType);
                        membershipType.Organization.Value = organization;

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(membershipType);

                                if (status.IsSuccess)
                                {
                                    transaction.Commit();
                                    Notice("{0} added membership type {1}", CurrentSession.User.ShortHand, membershipType);
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
            Get("/membershiptype/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membershipType))
                {
                    if (status.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            membershipType.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted membership type {1}", CurrentSession.User.ShortHand, membershipType);
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }

        private static void Updatefields(PostStatus status, MembershipTypeEditViewModel model, MembershipType membershipType)
        {
            status.AssignMultiLanguageRequired("Name", membershipType.Name, model.Name);
            status.AssignObjectIdString("SenderGroup", membershipType.SenderGroup, model.SenderGroup);
        }
    }
}

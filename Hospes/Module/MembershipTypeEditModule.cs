using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MimeKit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class PaymentParameterEditViewModel : DialogViewModel
    {
        public string Key;
        public string Phrase;
        public string Value;

        public PaymentParameterEditViewModel(Translator translator, PaymentParameterType type, PaymentParameter parameter)
        {
            Key = type.Key.Replace(".", string.Empty).EscapeHtml();
            Phrase = type.GetTranslation(translator).EscapeHtml();
            Value = Math.Round(parameter.Value, 2).ToString();
        }
    }

    public class PaymentParametersEditViewModel : DialogViewModel
    {
        public string Id;
        public List<PaymentParameterEditViewModel> List;

        public PaymentParametersEditViewModel(IDatabase database, Translator translator, MembershipType type)
            : base(translator, translator.Get("PaymentParameters.Edit.Title", "Title of the payment parameters edit dialog", "Edit payment parameters"), "paymentParametersEditDialog")
        {
            Id = type.Id.Value.ToString();
            var model = type.CreatePaymentModel(database);
            List = new List<PaymentParameterEditViewModel>();

            if (model != null)
            {
                List.AddRange(model.ParameterTypes
                    .Select(pt => new PaymentParameterEditViewModel(translator, pt, 
                        type.PaymentParameters.First(p => p.Key.Value == pt.Key))));
            }
        }
    }

    public class MembershipTypeEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string[] Right;
        public string Payment;
        public string Collection;
        public string MaximumPoints;
        public string MaximumBalanceForward;
        public string MaximumDiscount;
        public string TriplePoints;
        public string DoublePoints;
        public string SenderGroup;
        public List<NamedIntViewModel> Rights;
        public List<NamedIntViewModel> Payments;
        public List<NamedIntViewModel> Collections;
        public List<NamedIdViewModel> Groups;
        public List<NamedIdViewModel> PointsTallyMails;
        public string[] PointsTallyMailTemplates;
        public List<NamedIdViewModel> SettlementMails;
        public string[] SettlementMailTemplates;
        public List<NamedIdViewModel> BillDocuments;
        public string[] BillDocumentTemplates;
        public List<NamedIdViewModel> SettlementDocuments;
        public string[] SettlementDocumentTemplates;
        public List<NamedIdViewModel> PointsTallyDocuments;
        public string[] PointsTallyDocumentTemplates;
        public List<NamedIdViewModel> PaymentParameterUpdateRequiredMails;
        public string[] PaymentParameterUpdateRequiredMailTemplates;
        public List<NamedIdViewModel> PaymentParameterUpdateInvitationMails;
        public string[] PaymentParameterUpdateInvitationMailTemplates;
        public string PhraseFieldRight;
        public string PhraseFieldPayment;
        public string PhraseFieldCollection;
        public string PhraseFieldMaximumPoints;
        public string PhraseFieldMaximumBalanceForward;
        public string PhraseFieldMaximumDiscount;
        public string PhraseFieldTriplePoints;
        public string PhraseFieldDoublePoints;
        public string PhraseFieldPointsTallyMailTemplates;
        public string PhraseFieldSettlementMailTemplates;
        public string PhraseFieldSettlementDocumentTemplates;
        public string PhraseFieldBillDocumentTemplates;
        public string PhraseFieldPointsTallyDocumentTemplates;
        public string PhraseFieldPaymentParameterUpdateRequiredMailTemplates;
        public string PhraseFieldPaymentParameterUpdateInvitationMailTemplates;
        public string PhraseFieldSenderGroup;

        public MembershipTypeEditViewModel()
        { 
        }

        public MembershipTypeEditViewModel(Translator translator)
            : base(translator, translator.Get("MembershipType.Edit.Title", "Title of the membership type edit dialog", "Edit membership type"), "membershipTypeEditDialog")
        {
            PhraseFieldRight = translator.Get("MembershipType.Edit.Field.Right", "Right field in the membership type edit dialog", "Membership rights").EscapeHtml();
            PhraseFieldPayment = translator.Get("MembershipType.Edit.Field.Payment", "Payment field in the membership type edit dialog", "Payment model").EscapeHtml();
            PhraseFieldCollection = translator.Get("MembershipType.Edit.Field.Collection", "Collection field in the membership type edit dialog", "Collection model").EscapeHtml();
            PhraseFieldMaximumPoints = translator.Get("MembershipType.Edit.Field.MaximumPoints", "Maximum points field in the membership type edit dialog", "Maximum points").EscapeHtml();
            PhraseFieldMaximumBalanceForward = translator.Get("MembershipType.Edit.Field.MaximumBalanceForward", "Maximum balance forward points field in the membership type edit dialog", "Maximum balance forward").EscapeHtml();
            PhraseFieldMaximumDiscount = translator.Get("MembershipType.Edit.Field.MaximumDiscount", "Maximum discount field in the membership type edit dialog", "Maximum discount").EscapeHtml();
            PhraseFieldTriplePoints = translator.Get("MembershipType.Edit.Field.TriplePoints", "Triple points field in the membership type edit dialog", "Triple points").EscapeHtml();
            PhraseFieldDoublePoints = translator.Get("MembershipType.Edit.Field.DoublePoints", "Double points field in the membership type edit dialog", "Double points").EscapeHtml();
            PhraseFieldPointsTallyMailTemplates = translator.Get("MembershipType.Edit.Field.PointsTallyMailTemplates", "Points tally mail templates field in the membership type edit dialog", "Points tally mail templates").EscapeHtml();
            PhraseFieldSettlementDocumentTemplates = translator.Get("MembershipType.Edit.Field.SettlementDocumentTemplates ", "Settlement document templates field in the membership type edit dialog", "Settlement document templates").EscapeHtml();
            PhraseFieldSettlementMailTemplates = translator.Get("MembershipType.Edit.Field.SettlementMailTemplates ", "Settlement mail templates field in the membership type edit dialog", "Settlement mail templates").EscapeHtml();
            PhraseFieldBillDocumentTemplates = translator.Get("MembershipType.Edit.Field.BillDocumentTemplates", "Bill document templates field in the membership type edit dialog", "Bill document templates").EscapeHtml();
            PhraseFieldPointsTallyDocumentTemplates = translator.Get("MembershipType.Edit.Field.PointsTallyDocumentTemplates", "Points tally document templates field in the membership type edit dialog", "Points tally document templates").EscapeHtml();
            PhraseFieldPaymentParameterUpdateRequiredMailTemplates = translator.Get("MembershipType.Edit.Field.PaymentParameterUpdateRequiredMailTemplates", "Payment parameter update required templates field in the membership type edit dialog", "Payment parameter update required templates").EscapeHtml();
            PhraseFieldPaymentParameterUpdateInvitationMailTemplates = translator.Get("MembershipType.Edit.Field.PaymentParameterUpdateInvitationMailTemplates", "Payment parameter update invitation templates field in the membership type edit dialog", "Payment parameter update invitation templates").EscapeHtml();
            PhraseFieldSenderGroup = translator.Get("MembershipType.Edit.Field.SenderGroup", "Sender group field in the membership type edit dialog", "Sender group").EscapeHtml();
        }

        public MembershipTypeEditViewModel(Translator translator, IDatabase database, Session session, Organization organization)
            : this(translator)
        {
            Method = "add";
            Id = organization.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", new MultiLanguageString());
            Right = new string[0];
            Payment = string.Empty;
            Collection = string.Empty;
            MaximumPoints = string.Empty;
            MaximumBalanceForward = string.Empty;
            MaximumDiscount = string.Empty;
            TriplePoints = string.Empty;
            DoublePoints = string.Empty;
            Rights = new List<NamedIntViewModel>();
            Rights.Add(new NamedIntViewModel(translator, MembershipRight.Voting, false));
            Payments = new List<NamedIntViewModel>();
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.None, false));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.Fixed, false));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.FederalTax, false));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.Flat, false));
            Collections = new List<NamedIntViewModel>();
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.None, false));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.Direct, false));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.ByParent, false));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.BySub, false));
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Structure, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(g => g.Name));
            PointsTallyMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            SettlementMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            BillDocuments = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            SettlementDocuments = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            PointsTallyDocuments = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            PaymentParameterUpdateRequiredMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            PaymentParameterUpdateInvitationMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == organization && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
        }

        public MembershipTypeEditViewModel(Translator translator, IDatabase database, Session session, MembershipType membershipType)
            : this(translator)
        {
            Method = "edit";
            Id = membershipType.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("MembershipType.Edit.Field.Name", "Name field in the membership type edit dialog", "Name ({0})", membershipType.Name.Value);
            Right = new string[0];
            Payment = string.Empty;
            Collection = string.Empty;
            MaximumPoints = membershipType.MaximumPoints.Value.ToString();
            MaximumBalanceForward = membershipType.MaximumBalanceForward.Value.ToString();
            MaximumDiscount = membershipType.MaximumDiscount.Value.ToString();
            TriplePoints = membershipType.TriplePoints.Value.ToString();
            DoublePoints = membershipType.DoublePoints.Value.ToString();
            Rights = new List<NamedIntViewModel>();
            Rights.Add(new NamedIntViewModel(translator, MembershipRight.Voting, membershipType.Rights.Value.HasFlag(MembershipRight.Voting)));
            Payments = new List<NamedIntViewModel>();
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.None, membershipType.Payment.Value == PaymentModel.None));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.Fixed, membershipType.Payment.Value == PaymentModel.Fixed));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.FederalTax, membershipType.Payment.Value == PaymentModel.FederalTax));
            Payments.Add(new NamedIntViewModel(translator, PaymentModel.Flat, membershipType.Payment.Value == PaymentModel.Flat));
            Collections = new List<NamedIntViewModel>();
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.None, membershipType.Collection.Value == CollectionModel.None));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.Direct, membershipType.Collection.Value == CollectionModel.Direct));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.ByParent, membershipType.Collection.Value == CollectionModel.ByParent));
            Collections.Add(new NamedIntViewModel(translator, CollectionModel.BySub, membershipType.Collection.Value == CollectionModel.BySub));
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Structure, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, membershipType.SenderGroup.Value == g))
                .OrderBy(g => g.Name));
            PointsTallyMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, membershipType.PointsTallyMails(database).Any(x => x.Template.Value == t)))
                .OrderBy(t => t.Name));
            SettlementMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, membershipType.SettlementMails(database).Any(x => x.Template.Value == t)))
                .OrderBy(t => t.Name));
            BillDocuments = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, membershipType.BillDocuments(database).Any(x => x.Template.Value == t)))
                .OrderBy(t => t.Name));
            SettlementDocuments = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, membershipType.SettlementDocuments(database).Any(x => x.Template.Value == t)))
                .OrderBy(t => t.Name));
            PointsTallyDocuments = new List<NamedIdViewModel>(database
                .Query<LatexTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, membershipType.PointsTallyDocuments(database).Any(x => x.Template.Value == t)))
                .OrderBy(t => t.Name));
            PaymentParameterUpdateRequiredMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, membershipType.PaymentParameterUpdateRequiredMails(database).Any(x => x.Template.Value == t)))
                .OrderBy(t => t.Name));
            PaymentParameterUpdateInvitationMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.Organization.Value == membershipType.Organization.Value && t.AssignmentType.Value == TemplateAssignmentType.MembershipType)
                .Select(t => new NamedIdViewModel(translator, t, membershipType.PaymentParameterUpdateInvitationMails(database).Any(x => x.Template.Value == t)))
                .OrderBy(t => t.Name));
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
                                UpdateTemplatesAndParameters(model, membershipType, status);

                                foreach (var period in Database
                                    .Query<BudgetPeriod>(DC.Equal("organizationid", membershipType.Organization.Value.Id.Value))
                                    .Where(p => p.EndDate.Value.Date >= DateTime.UtcNow.Date))
                                {
                                    period.UpdateTotalPoints(Database);
                                    Database.Save(period);
                                }

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
                                UpdateTemplatesAndParameters(model, membershipType, status);

                                foreach (var period in Database
                                    .Query<BudgetPeriod>(DC.Equal("organizationid", membershipType.Organization.Value.Id.Value))
                                    .Where(p => p.EndDate.Value.Date >= DateTime.UtcNow.Date))
                                {
                                    period.UpdateTotalPoints(Database);
                                    Database.Save(period);
                                }

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
            Get("/membershiptype/parameters/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);

                if (membershipType != null)
                {
                    if (HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/membershipedit_parameters.sshtml",
                            new PaymentParametersEditViewModel(Database, Translator, membershipType)];
                    }
                }

                return string.Empty;
            });
            Post("/membershiptype/parameters/{id}", parameters =>
            {
                string idString = parameters.id;
                var membershipType = Database.Query<MembershipType>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membershipType))
                {
                    if (status.HasAccess(membershipType.Organization.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JObject.Parse(ReadBody());
                        var payment = membershipType.CreatePaymentModel(Database);

                        if (payment != null)
                        {
                            foreach (var property in model.Properties())
                            {
                                var parameter = membershipType.PaymentParameters
                                    .First(p => p.Key.Value.Replace(".", string.Empty) == property.Name);

                                if (parameter != null)
                                {
                                    status.AssignDecimalString(property.Name, parameter.Value, property.Value.ToString());
                                }
                                else
                                {
                                    status.SetErrorNotFound();
                                }
                            }

                            if (status.IsSuccess)
                            {
                                Database.Save(membershipType);
                                Notice("{0} changed payment parameter types of {1}", CurrentSession.User.ShortHand, membershipType);
                            }
                        }
                        else
                        {
                            status.SetErrorNotFound();
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

        private void UpdateTemplatesAndParameters(MembershipTypeEditViewModel model, MembershipType membershipType, PostStatus status)
        {
            status.UpdateMailTemplates(Database, membershipType.PointsTallyMail, model.PointsTallyMailTemplates);
            status.UpdateMailTemplates(Database, membershipType.SettlementMail, model.SettlementMailTemplates);
            status.UpdateLatexTemplates(Database, membershipType.BillDocument, model.BillDocumentTemplates);
            status.UpdateLatexTemplates(Database, membershipType.SettlementDocument, model.SettlementDocumentTemplates);
            status.UpdateLatexTemplates(Database, membershipType.PointsTallyDocument, model.PointsTallyDocumentTemplates);
            status.UpdateMailTemplates(Database, membershipType.PaymentParameterUpdateRequiredMail, model.PaymentParameterUpdateRequiredMailTemplates);
            status.UpdateMailTemplates(Database, membershipType.PaymentParameterUpdateInvitationMail, model.PaymentParameterUpdateInvitationMailTemplates);
            AddPaymentModelParameters(membershipType);
        }

        private static void Updatefields(PostStatus status, MembershipTypeEditViewModel model, MembershipType membershipType)
        {
            status.AssignMultiLanguageRequired("Name", membershipType.Name, model.Name);
            status.AssignFlagIntsString("Right", membershipType.Rights, model.Right);
            status.AssignEnumIntString("Payment", membershipType.Payment, model.Payment);
            status.AssignEnumIntString("Collection", membershipType.Collection, model.Collection);
            status.AssignInt64String("MaximumPoints", membershipType.MaximumPoints, model.MaximumPoints);
            status.AssignInt64String("TriplePoints", membershipType.TriplePoints, model.TriplePoints);
            status.AssignInt64String("DoublePoints", membershipType.DoublePoints, model.DoublePoints);
            status.AssignInt64String("MaximumBalanceForward", membershipType.MaximumBalanceForward, model.MaximumBalanceForward);
            status.AssignDecimalString("MaximumDiscount", membershipType.MaximumDiscount, model.MaximumDiscount);
            status.AssignObjectIdString("SenderGroup", membershipType.SenderGroup, model.SenderGroup);
        }

        private IEnumerable<Points> CreateTestPoints(Person person)
        {
            var list = new List<Points>();

            list.Add(CreateTestPoint(person, -90, 200, "Teilnahme Parteiversammlung"));
            list.Add(CreateTestPoint(person, -82, 500, "Blogpost"));
            list.Add(CreateTestPoint(person, -77, 1200, "Neumitglied geworben"));

            return list;
        }

        private Points CreateTestPoint(Person person, int addDays, int amount, string reason)
        {
            var points = new Points(Guid.NewGuid());
            points.Owner.Value = person;
            points.Moment.Value = DateTime.Now.AddDays(addDays);
            points.Amount.Value = amount;
            points.Reason.Value = reason;
            points.Url.Value = string.Empty;
            return points;
        }

        private void AddPaymentModelParameters(MembershipType type)
        {
            var model = type.CreatePaymentModel(Database);

            if (model != null)
            {
                foreach (var parameterType in model.ParameterTypes)
                {
                    var parameter = type.PaymentParameters.FirstOrDefault(p => p.Key.Value == parameterType.Key);

                    if (parameter == null)
                    {
                        parameter = new PaymentParameter(Guid.NewGuid());
                        parameter.Key.Value = parameterType.Key;
                        parameter.Value.Value = parameterType.DefaultValue;
                        parameter.Type.Value = type;
                        Database.Save(parameter);
                    }
                }
            }
        }
    }
}

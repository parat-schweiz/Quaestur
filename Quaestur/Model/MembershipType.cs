using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    [Flags]
    public enum MembershipRight
    { 
        None = 0,
        Voting = 1,
    }

    public static class MembershipRightExtensions
    {
        public static string Translate(this MembershipRight right, Translator translator)
        {
            switch (right)
            {
                case MembershipRight.Voting:
                    return translator.Get("Enum.MembershipRight.Voting", "Voting value in the membership right enum", "Voting");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum PaymentModel
    { 
        None = 0,
        Fixed = 1,
        FederalTax = 2,
        Flat = 3,
    }

    public static class PaymentModelExtensions
    {
        public static string Translate(this PaymentModel model, Translator translator)
        {
            switch (model)
            {
                case PaymentModel.None:
                    return translator.Get("Enum.PaymentModel.None", "None value in the payment model enum", "None");
                case PaymentModel.Fixed:
                    return translator.Get("Enum.PaymentModel.Fixed", "Fixed value in the payment model enum", "Fixed");
                case PaymentModel.FederalTax:
                    return translator.Get("Enum.PaymentModel.FederalTax", "Federal tax value in the payment model enum", "Federal tax");
                case PaymentModel.Flat:
                    return translator.Get("Enum.PaymentModel.Flat", "Flat value in the payment model enum", "Flat");
                default:
                    throw new NotSupportedException();
            }
        }

        public static IPaymentModel Create(this PaymentModel model, MembershipType membershipType, IDatabase database)
        {
            switch (model)
            {
                case PaymentModel.None:
                    return null;
                case PaymentModel.Fixed:
                    return new PaymentModelFixed(membershipType, database);
                case PaymentModel.FederalTax:
                    return new PaymentModelFederalTax(membershipType, database);
                case PaymentModel.Flat:
                    return new PaymentModelFlat(membershipType, database);
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum CollectionModel
    {
        None = 0,
        Direct = 1,
        ByParent = 2,
        BySub = 3,
    }

    public static class CollectionModelExtensions
    {
        public static string Translate(this CollectionModel model, Translator translator)
        {
            switch (model)
            {
                case CollectionModel.None:
                    return translator.Get("Enum.CollectionModel.None", "None value in the collection model enum", "None");
                case CollectionModel.Direct:
                    return translator.Get("Enum.CollectionModel.Direct", "Direct value in the collection model enum", "Direct");
                case CollectionModel.ByParent:
                    return translator.Get("Enum.CollectionModel.ByParent", "By parent organization value in the collection model enum", "By parent organization");
                case CollectionModel.BySub:
                    return translator.Get("Enum.CollectionModel.BySub", "By suborganization value in the collection model enum", "By suborganization");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class MembershipType : DatabaseObject
    {
        public ForeignKeyField<Organization, MembershipType> Organization { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public EnumField<MembershipRight> Rights { get; private set; }
        public EnumField<PaymentModel> Payment { get; private set; }
        public EnumField<CollectionModel> Collection { get; private set; }
        public Field<long> MaximumPoints { get; private set; }
        public Field<long> MaximumBalanceForward { get; private set; }
        public DecimalField MaximumDiscount { get; private set; }
        public Field<long> TriplePoints { get; private set; }
        public Field<long> DoublePoints { get; private set; }
        public ForeignKeyField<Group, MembershipType> SenderGroup { get; private set; }
        public ForeignKeyField<Group, MembershipType> NotificationGroup { get; private set; }
        public List<PaymentParameter> PaymentParameters { get; private set; }

        public MembershipType() : this(Guid.Empty)
        {
        }

		public MembershipType(Guid id) : base(id)
        {
            PaymentParameters = new List<PaymentParameter>();
            Organization = new ForeignKeyField<Organization, MembershipType>(this, "organizationid", false, o => o.MembershipTypes);
            Name = new MultiLanguageStringField(this, "name");
            Rights = new EnumField<MembershipRight>(this, "membershiprights", MembershipRight.None, MembershipRightExtensions.Translate);
            Payment = new EnumField<PaymentModel>(this, "paymentmode", PaymentModel.None, PaymentModelExtensions.Translate);
            Collection = new EnumField<CollectionModel>(this, "collectionmodel", CollectionModel.None, CollectionModelExtensions.Translate);
            MaximumPoints = new Field<long>(this, "maximumpoints", 0);
            MaximumBalanceForward = new Field<long>(this, "maximumbalanceforward", 0);
            MaximumDiscount = new DecimalField(this, "maximumdiscount", 16, 4);
            TriplePoints = new Field<long>(this, "triplepoints", 0);
            DoublePoints = new Field<long>(this, "doublepoints", 0);
            SenderGroup = new ForeignKeyField<Group, MembershipType>(this, "sendergroup", true, null);
            NotificationGroup = new ForeignKeyField<Group, MembershipType>(this, "notificationgroup", true, null);
        }

        public const string PointsTallyMailFieldName = "PointsTallyMails";
        public const string SettlementMailFieldName = "SettlementMails";
        public const string BillDocumentFieldName = "BillDocuments";
        public const string SettlementDocumentFieldName = "SettlementDocuments";
        public const string PointsTallyDocumentFieldName = "PointsTallyDocuments";
        public const string PaymentParameterUpdateRequiredMailFieldName = "PaymentParameterUpdateRequiredMails";
        public const string PaymentParameterUpdateInvitationMailFieldName = "PaymentParameterUpdateInvitationMails";

        public MailTemplateAssignmentField PointsTallyMails
        {
            get { return new MailTemplateAssignmentField(TemplateAssignmentType.MembershipType, Id.Value, PointsTallyMailFieldName); }
        }

        public MailTemplateAssignmentField SettlementMails
        {
            get { return new MailTemplateAssignmentField(TemplateAssignmentType.MembershipType, Id.Value, SettlementMailFieldName); }
        }

        public LatexTemplateAssignmentField BillDocuments
        {
            get { return new LatexTemplateAssignmentField(TemplateAssignmentType.MembershipType, Id.Value, BillDocumentFieldName); }
        }

        public LatexTemplateAssignmentField SettlementDocuments
        {
            get { return new LatexTemplateAssignmentField(TemplateAssignmentType.MembershipType, Id.Value, SettlementDocumentFieldName); }
        }

        public LatexTemplateAssignmentField PointsTallyDocuments
        {
            get { return new LatexTemplateAssignmentField(TemplateAssignmentType.MembershipType, Id.Value, PointsTallyDocumentFieldName); }
        }

        public MailTemplateAssignmentField PaymentParameterUpdateRequiredMails
        {
            get { return new MailTemplateAssignmentField(TemplateAssignmentType.MembershipType, Id.Value, PaymentParameterUpdateRequiredMailFieldName); }
        }

        public MailTemplateAssignmentField PaymentParameterUpdateInvitationMails
        {
            get { return new MailTemplateAssignmentField(TemplateAssignmentType.MembershipType, Id.Value, PaymentParameterUpdateInvitationMailFieldName); }
        }

        public static string GetFieldNameTranslation(Translator translator, string fieldName)
        {
            switch (fieldName)
            {
                case PointsTallyMailFieldName:
                    return translator.Get("BallotTemplate.FieldName.PointsTallyMail", "Points tally mail field name of the ballot template", "Points tally mail");
                case BillDocumentFieldName:
                    return translator.Get("BallotTemplate.FieldName.BillDocument", "Bill document field name of the ballot template", "Bill document");
                case PointsTallyDocumentFieldName:
                    return translator.Get("BallotTemplate.FieldName.PointsTallyDocument", "Points tally document field name of the ballot template", "Points tally document");
                default:
                    throw new NotSupportedException();
            }
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get 
            {
                yield return new MultiCascade<PaymentParameter>("membershiptypeid", Id.Value, () => PaymentParameters);
            }
        }

        public int GetReminderPeriod(IDatabase database)
        {
            var model = CreatePaymentModel(database);

            if (model != null)
            {
                return model.GetReminderPeriod();
            }
            else
            {
                return 10; 
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var billSendingTemplate in database
                .Query<BillSendingTemplate>(DC.Equal("membershiptypeid", Id.Value))
                .ToList())
            {
                billSendingTemplate.Delete(database);
            }

            foreach (var membership in database
                .Query<Membership>(DC.Equal("membershiptypeid", Id.Value))
                .ToList())
            {
                membership.Delete(database);
            }

            foreach (var subscription in database
                .Query<Subscription>(DC.Equal("membershiptypeid", Id.Value))
                .ToList())
            {
                subscription.Delete(database);
            }

            foreach (var parameter in PaymentParameters)
            {
                parameter.Delete(database); 
            }

            foreach (var template in database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value)))
            {
                template.Delete(database);
            }

            foreach (var template in database.Query<LatexTemplateAssignment>(DC.Equal("assignedid", Id.Value)))
            {
                template.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }

        public IPaymentModel CreatePaymentModel(IDatabase database)
        {
            return Payment.Value.Create(this, database);
        }
    }
}

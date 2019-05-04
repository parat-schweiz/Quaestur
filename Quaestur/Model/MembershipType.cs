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
                default:
                    throw new NotSupportedException();
            }
        }

        public static IPaymentModel Create(this PaymentModel model, MembershipType membershipType)
        {
            switch (model)
            {
                case PaymentModel.None:
                    return null;
                case PaymentModel.Fixed:
                    return new PaymentModelFixed(membershipType);
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
        public MultiLanguageStringField BillTemplateLatex { get; private set; }
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
            BillTemplateLatex = new MultiLanguageStringField(this, "billtemplatelatex", AllowStringType.SafeLatex);
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get 
            {
                yield return new MultiCascade<PaymentParameter>("membershiptypeid", Id.Value, () => PaymentParameters);
            }
        }

        public int GetReminderPeriod()
        {
            var model = CreatePaymentModel();

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

            foreach (var parameter in PaymentParameters)
            {
                parameter.Delete(database); 
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

        public IPaymentModel CreatePaymentModel()
        {
            return Payment.Value.Create(this);
        }
    }
}

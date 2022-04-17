using System;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public enum PrepaymentType
    {
        None = 0,
        BankTransaction = 1,
        Expenses = 2,
        StorePayment = 3,
        Judgement = 4,
        MembershipFee = 5,
    }

    public static class PrepaymentTypeExtensions
    {
        public static string Translate(this PrepaymentType type, Translator translator)
        {
            switch (type)
            {
                case PrepaymentType.None:
                    return translator.Get("Enum.PrepaymentType.None", "Value 'None' in PrepaymentType enum", "None");
                case PrepaymentType.BankTransaction:
                    return translator.Get("Enum.PrepaymentType.BankTransaction", "Value 'BankTransaction' in PrepaymentType enum", "Bank Transaction");
                case PrepaymentType.Expenses:
                    return translator.Get("Enum.PrepaymentType.Expenses", "Value 'Expenses' in PrepaymentType enum", "Expenses");
                case PrepaymentType.StorePayment:
                    return translator.Get("Enum.PrepaymentType.StorePayment", "Value 'StorePayment' in PrepaymentType enum", "Store Payment");
                case PrepaymentType.Judgement:
                    return translator.Get("Enum.PrepaymentType.Judgement", "Value 'Judgement' in PrepaymentType enum", "Judgement");
                case PrepaymentType.MembershipFee:
                    return translator.Get("Enum.PrepaymentType.MembershipFee", "Value 'Membership Fee' in PrepaymentType enum", "Membership Fee");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Prepayment : DatabaseObject
    {
		public ForeignKeyField<Person, Prepayment> Person { get; private set; }
        public Field<DateTime> Moment { get; private set; }
        public DecimalField Amount { get; private set; }
        public StringField Reason { get; private set; }
        public StringField Url { get; private set; }
        public EnumField<PrepaymentType> ReferenceType { get; private set; }
        public StringField Reference { get; private set; }

        public Prepayment() : this(Guid.Empty)
        {
        }

        public Prepayment(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, Prepayment>(this, "personid", false, null);
            Moment = new Field<DateTime>(this, "moment", new DateTime(1850, 1, 1));
            Amount = new DecimalField(this, "amount", 16, 4);
            Reason = new StringField(this, "reason", 4096, AllowStringType.SimpleText);
            Url = new StringField(this, "url", 2048, AllowStringType.SimpleText);
            ReferenceType = new EnumField<PrepaymentType>(this, "referencetype", PrepaymentType.None, PrepaymentTypeExtensions.Translate);
            Reference = new StringField(this, "reference", 2048, AllowStringType.SimpleText);
        }

        public override string ToString()
        {
            return Moment.Value.FormatSwissDateDay() + " " + Reason.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Moment.Value.FormatSwissDateDay() + " " + Reason.Value;
        }
    }
}

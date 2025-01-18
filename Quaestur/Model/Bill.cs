using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public enum BillStatus
    {
        New = 0,
        Payed = 1,
        Canceled = 2,
    }

    public static class BillStatusExtensions
    {
        public static string Translate(this BillStatus status, Translator translator)
        {
            switch (status)
            {
                case BillStatus.New:
                    return translator.Get("Enum.BillStatus.New", "Value 'New' in BillStatus enum", "New");
                case BillStatus.Payed:
                    return translator.Get("Enum.BillStatus.Payed", "Value 'Payed' in BillStatus enum", "Payed");
                case BillStatus.Canceled:
                    return translator.Get("Enum.BillStatus.Canceled", "Value 'Canceled' in BillStatus enum", "Canceled");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Bill : DatabaseObject
    {
        public ForeignKeyField<Membership, Bill> Membership { get; private set; }
        public StringField Number { get; private set; }
        public EnumField<BillStatus> Status { get; private set; }
        public DateField FromDate { get; private set; }
        public DateField UntilDate { get; private set; }
        public DecimalField Amount { get; private set; }
        public DateField CreatedDate { get; private set; }
        public DateNullField PayedDate { get; private set; }
        public ByteArrayField DocumentData { get; private set; }
        public DateNullField ReminderDate { get; private set; }
        public Field<int> ReminderLevel { get; private set; }

        public Bill() : this(Guid.Empty)
        {
        }

        public Bill(Guid id) : base(id)
        {
            Membership = new ForeignKeyField<Membership, Bill>(this, "membershipid", false, null);
            Number = new StringField(this, "number", 256);
            Status = new EnumField<BillStatus>(this, "status", BillStatus.New, BillStatusExtensions.Translate);
            FromDate = new DateField(this, "fromdate", new DateTime(1850, 1, 1));
            UntilDate = new DateField(this, "untildate", new DateTime(1850, 1, 1));
            Amount = new DecimalField(this, "amount", 16, 4);
            CreatedDate = new DateField(this, "createddate", new DateTime(1850, 1, 1));
            PayedDate = new DateNullField(this, "payeddate");
            DocumentData = new ByteArrayField(this, "documentdata", false);
            ReminderDate = new DateNullField(this, "reminderdate");
            ReminderLevel = new Field<int>(this, "reminderlevel", 0);
        }

        public override string ToString()
        {
            return Number.Value;
        }

        public override string GetText(Translator translator)
        {
            return Number.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

using System;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Hospes
{
    public class Prepayment : DatabaseObject
    {
		public ForeignKeyField<Person, Prepayment> Person { get; private set; }
        public Field<DateTime> Moment { get; private set; }
        public DecimalField Amount { get; private set; }
        public StringField Reason { get; private set; }

        public Prepayment() : this(Guid.Empty)
        {
        }

        public Prepayment(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, Prepayment>(this, "personid", false, null);
            Moment = new Field<DateTime>(this, "moment", new DateTime(1850, 1, 1));
            Amount = new DecimalField(this, "amount", 16, 4);
            Reason = new StringField(this, "reason", 4096, AllowStringType.SimpleText);
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

using System;
using System.Collections.Generic;

namespace Quaestur
{
    public class PaymentParameter : DatabaseObject
    {
        public ForeignKeyField<MembershipType, PaymentParameter> Type { get; private set; }
        public StringField Key { get; private set; }
        public DecimalField Value { get; private set; }

        public PaymentParameter() : this(Guid.Empty)
        {
        }

		public PaymentParameter(Guid id) : base(id)
        {
            Type = new ForeignKeyField<MembershipType, PaymentParameter>(this, "membershiptypeid", false, mt => mt.PaymentParameters);
            Key = new StringField(this, "key", 256);
            Value = new DecimalField(this, "value", 16, 4);
        }

        public override string ToString()
        {
            return Key.Value;
        }

        public override string GetText(Translator translator)
        {
            return Key.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

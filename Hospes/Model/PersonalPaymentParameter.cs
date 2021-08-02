﻿using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class PersonalPaymentParameter : DatabaseObject
    {
        public ForeignKeyField<Person, PersonalPaymentParameter> Person { get; private set; }
        public StringField Key { get; private set; }
        public DecimalField Value { get; private set; }
        public Field<DateTime> LastUpdate { get; private set; }

        public PersonalPaymentParameter() : this(Guid.Empty)
        {
        }

		public PersonalPaymentParameter(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, PersonalPaymentParameter>(this, "personid", false, m => m.PaymentParameters);
            Key = new StringField(this, "key", 256);
            Value = new DecimalField(this, "value", 16, 4);
            LastUpdate = new Field<DateTime>(this, "lastupdate", DateTime.UtcNow);
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

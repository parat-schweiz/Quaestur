using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class LoginLink : DatabaseObject
    {
        public ForeignKeyField<Person, LoginLink> Person { get; private set; }
        public ByteArrayField Secret { get; private set; }
        public Field<bool> CompleteAuth { get; private set; }
        public Field<bool> TwoFactorAuth { get; private set; }
        public FieldDateTime Expires { get; private set; }
        public StringNullField Verification { get; private set; }
        public Field<bool> Confirmed { get; private set; }

        public LoginLink() : this(Guid.Empty)
        {
        }

        public LoginLink(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, LoginLink>(this, "personid", false, null);
            Secret = new ByteArrayField(this, "secret", false);
            CompleteAuth = new Field<bool>(this, "completeauth", false);
            TwoFactorAuth = new Field<bool>(this, "twofactorauth", false);
            Expires = new FieldDateTime(this, "expires", DateTime.UtcNow.AddMinutes(5));
            Verification = new StringNullField(this, "verification", 256, AllowStringType.SimpleText);
            Confirmed = new Field<bool>(this, "confirmed", false);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return base.ToString();
        }
    }
}

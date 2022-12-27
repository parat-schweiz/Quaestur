using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class Oauth2Authorization : DatabaseObject
    {
		public ForeignKeyField<Oauth2Client, Oauth2Authorization> Client { get; private set; }
        public ForeignKeyField<Person, Oauth2Authorization> User { get; private set; }
        public FieldDateTime Moment { get; private set; }
        public FieldDateTime Expiry { get; private set; }

        public Oauth2Authorization() : this(Guid.Empty)
        {
        }

        public Oauth2Authorization(Guid id) : base(id)
        {
            Client = new ForeignKeyField<Oauth2Client, Oauth2Authorization>(this, "clientid", false, null);
            User = new ForeignKeyField<Person, Oauth2Authorization>(this, "userid", false, null);
            Moment = new FieldDateTime(this, "moment", new DateTime(1850, 1, 1));
            Expiry = new FieldDateTime(this, "expiry", new DateTime(1850, 1, 1));
        }

        public override string ToString()
        {
            return Client.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Client.GetText(translator);
        }
    }
}

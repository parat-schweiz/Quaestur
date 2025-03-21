﻿using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class Oauth2Session : DatabaseObject
    {
		public ForeignKeyField<Oauth2Client, Oauth2Session> Client { get; private set; }
        public ForeignKeyField<Person, Oauth2Session> User { get; private set; }
        public StringField AuthCode { get; private set; }
        public StringField Token { get; private set; }
        public DateTimeField Moment { get; private set; }
        public DateTimeField Expiry { get; private set; }
        public StringField Nonce { get; private set; }

        public Oauth2Session() : this(Guid.Empty)
        {
        }

        public Oauth2Session(Guid id) : base(id)
        {
            Client = new ForeignKeyField<Oauth2Client, Oauth2Session>(this, "clientid", false, null);
            User = new ForeignKeyField<Person, Oauth2Session>(this, "userid", false, null);
            AuthCode = new StringField(this, "authcode", 256);
            Token = new StringField(this, "token", 256);
            Moment = new DateTimeField(this, "moment", new DateTime(1850, 1, 1));
            Expiry = new DateTimeField(this, "expiry", new DateTime(1850, 1, 1));
            Nonce = new StringField(this, "nonce", 256);
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

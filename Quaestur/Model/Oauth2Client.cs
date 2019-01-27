using System;
using System.Collections.Generic;

namespace Quaestur
{
    public class Oauth2Client : DatabaseObject
    {
		public MultiLanguageStringField Name { get; private set; }
        public StringField Secret { get; private set; }
        public StringField RedirectUri { get; private set; }

        public Oauth2Client() : this(Guid.Empty)
        {
        }

        public Oauth2Client(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Secret = new StringField(this, "secret", 256);
            RedirectUri = new StringField(this, "redirecturi", 256, AllowStringType.UnsecureText);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var session in database.Query<Oauth2Session>(DC.Equal("clientid", Id.Value)))
            {
                session.Delete(database); 
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

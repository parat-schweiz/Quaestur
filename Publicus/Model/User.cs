using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;

namespace Publicus
{
    public class User : DatabaseObject
    {
        public StringField UserName { get; private set; }
        public EnumField<Language> Language { get; private set; }

        public User() : this(Guid.Empty)
        {
        }

        public User(Guid id) : base(id)
        {
            UserName = new StringField(this, "username", 32);
            Language = new EnumField<Language>(this, "language", Publicus.Language.English, LanguageExtensions.Translate);
        }

        public override string ToString()
        {
            return UserName.Value;
        }

        public override string GetText(Translator translator)
        {
            return UserName.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this); 
        }
    }
}

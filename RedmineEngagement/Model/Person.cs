using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace RedmineEngagement
{
    public class Person : DatabaseObject
    {
        public Field<int> UserId { get; private set; }
        public StringField UserName { get; private set; }
        public EnumField<Language> Language { get; private set; }

        public Person() : this(Guid.Empty)
        {
        }

        public Person(Guid id) : base(id)
        {
            UserId = new Field<int>(this, "userid", 0);
            UserName = new StringField(this, "username", 256, AllowStringType.SimpleText);
            Language = new EnumField<Language>(this, "language", SiteLibrary.Language.English, SiteLibrary.LanguageExtensions.Translate);
        }

        public override string ToString()
        {
            return "Person " + Id.ToString();
        }

        public override string GetText(Translator translator)
        {
            return Id.ToString();
        }

        public override void Delete(IDatabase database)
        {
            foreach (var issue in database.Query<Issue>(DC.Equal("assignedtoid", Id.Value)))
            {
                database.Delete(issue);
            }

            database.Delete(this);
        }
    }
}

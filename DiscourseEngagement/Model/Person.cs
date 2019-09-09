using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace DiscourseEngagement
{
    public class Person : DatabaseObject
    {
        public Field<int> UserId { get; private set; }
        public StringField DiscourseUserName { get; private set; }
        public StringField QuaesturUserName { get; private set; }
        public EnumField<Language> Language { get; private set; }

        public Person() : this(Guid.Empty)
        {
        }

        public Person(Guid id) : base(id)
        {
            UserId = new Field<int>(this, "userid", 0);
            DiscourseUserName = new StringField(this, "discourseusername", 256, AllowStringType.SimpleText);
            QuaesturUserName = new StringField(this, "quaesturusername", 256, AllowStringType.SimpleText);
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
            foreach (var like in database.Query<Like>(DC.Equal("personid", Id.Value)))
            {
                database.Delete(like);
            }

            foreach (var post in database.Query<Post>(DC.Equal("personid", Id.Value)))
            {
                database.Delete(post);
            }

            database.Delete(this);
        }
    }
}

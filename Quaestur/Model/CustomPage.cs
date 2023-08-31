using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class CustomPage : DatabaseObject
    {
		public MultiLanguageStringField Name { get; private set; }
        public MultiLanguageStringField Content { get; private set; }

        public CustomPage() : this(Guid.Empty)
        {
        }

        public CustomPage(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name", AllowStringType.SimpleText);
            Content = new MultiLanguageStringField(this, "content", AllowStringType.SafeHtml);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var page in database.Query<CustomMenuEntry>(DC.Equal("pageid", Id.Value)))
            {
                page.Delete(database);
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

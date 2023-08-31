using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class CustomMenuEntry : DatabaseObject
    {
		public MultiLanguageStringField Name { get; private set; }
        public MultiLanguageStringField LinkUrl { get; private set; }
        public ForeignKeyField<CustomMenuEntry, CustomMenuEntry> Parent { get; private set; }
        public ForeignKeyField<CustomPage, CustomMenuEntry> Page { get; private set; }
        public Field<int> Ordering { get; private set; }

        public CustomMenuEntry() : this(Guid.Empty)
        {
        }

        public CustomMenuEntry(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name", AllowStringType.SimpleText);
            LinkUrl = new MultiLanguageStringField(this, "linkurl", AllowStringType.SafeHtml);
            Parent = new ForeignKeyField<CustomMenuEntry, CustomMenuEntry>(this, "parentid", true, null);
            Page = new ForeignKeyField<CustomPage, CustomMenuEntry>(this, "pageid", true, null);
            Ordering = new Field<int>(this, "ordering", 0);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var child in database.Query<CustomMenuEntry>(DC.Equal("parentid", Id.Value)))
            {
                child.Delete(database);
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

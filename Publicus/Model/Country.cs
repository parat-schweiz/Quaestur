using System;
using System.Collections.Generic;

namespace Publicus
{
    public class Country : DatabaseObject
    {
		public MultiLanguageStringField Name { get; set; }

        public Country() : this(Guid.Empty)
        {
        }

        public Country(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var address in database.Query<PostalAddress>(DC.Equal("countryid", Id.Value)))
            {
                address.Delete(database);
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

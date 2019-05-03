using System;
using System.Collections.Generic;

namespace Publicus
{
    public class State : DatabaseObject
    {
        public MultiLanguageStringField Name { get; private set; }

        public State() : this(Guid.Empty)
        {
        }

        public State(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (PostalAddress address in database.Query<PostalAddress>(DC.Equal("stateid", Id.Value)))
            {
                address.State.Value = null;
                database.Save(address); 
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

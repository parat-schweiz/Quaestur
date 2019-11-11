using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public class Questionaire : DatabaseObject
    {
        public ForeignKeyField<Group, Questionaire> Owner { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public List<Section> Sections { get; private set; }

        public Questionaire() : this(Guid.Empty)
        {
        }

		public Questionaire(Guid id) : base(id)
        {
            Owner = new ForeignKeyField<Group, Questionaire>(this, "ownerid", false, null);
            Name = new MultiLanguageStringField(this, "name");
            Sections = new List<Section>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Section>("questionaireid", Id.Value, () => Sections);
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var question in database.Query<Section>(DC.Equal("questionaireid", Id.Value)))
            {
                question.Delete(database); 
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

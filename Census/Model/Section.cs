using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public class Section : DatabaseObject
    {
        public ForeignKeyField<Questionaire, Section> Questionaire { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public Field<int> Ordering { get; private set; }
        public List<Question> Questions { get; private set; }

        public Section() : this(Guid.Empty)
        {
        }

		public Section(Guid id) : base(id)
        {
            Questionaire = new ForeignKeyField<Questionaire, Section>(this, "questionaireid", false, q => q.Sections);
            Name = new MultiLanguageStringField(this, "name");
            Ordering = new Field<int>(this, "ordering", 0);
            Questions = new List<Question>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Question>("sectionid", Id.Value, () => Questions);
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var question in database.Query<Question>(DC.Equal("sectionid", Id.Value)))
            {
                question.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Questionaire.Value.ToString() + " / " + Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Questionaire.Value.GetText(translator) + " / " + Name.Value[translator.Language];
        }

        public Group Owner
        { 
            get
            {
                return Questionaire.Value.Owner.Value;
            }
        }
    }
}

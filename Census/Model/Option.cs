using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public class Option : DatabaseObject
    {
        public ForeignKeyField<Question, Option> Question { get; private set; }
        public MultiLanguageStringField Text { get; private set; }
        public Field<int> CheckedValue { get; private set; }
        public Field<int> UncheckedValue { get; private set; }

        public Option() : this(Guid.Empty)
        {
        }

		public Option(Guid id) : base(id)
        {
            Question = new ForeignKeyField<Question, Option>(this, "questionid", false, q => q.Options);
            Text = new MultiLanguageStringField(this, "text");
            CheckedValue = new Field<int>(this, "checkedvalue", 0);
            UncheckedValue = new Field<int>(this, "uncheckedvalue", 0);
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string ToString()
        {
            return Question.Value.ToString() + " / " + Text.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Question.Value.GetText(translator) + " / " + Text.Value[translator.Language];
        }

        public Group Owner
        {
            get
            {
                return Question.Value.Owner;
            }
        }
    }
}

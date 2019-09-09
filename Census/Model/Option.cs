using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public class Option : DatabaseObject
    {
        public ForeignKeyField<Question, Option> Question { get; private set; }
        public MultiLanguageStringField Text { get; private set; }
        public ForeignKeyField<Variable, Option> CheckedVariable { get; private set; }
        public EnumField<VariableModification> CheckedModification { get; private set; }
        public StringField CheckedValue { get; private set; }
        public ForeignKeyField<Variable, Option> UncheckedVariable { get; private set; }
        public EnumField<VariableModification> UncheckedModification { get; private set; }
        public StringField UncheckedValue { get; private set; }
        public Field<int> Ordering { get; private set; }

        public Option() : this(Guid.Empty)
        {
        }

		public Option(Guid id) : base(id)
        {
            Question = new ForeignKeyField<Question, Option>(this, "questionid", false, q => q.Options);
            Text = new MultiLanguageStringField(this, "text");
            CheckedVariable = new ForeignKeyField<Variable, Option>(this, "checkedvariable", true, null);
            CheckedModification = new EnumField<VariableModification>(this, "checkedmodification", VariableModification.None, VariableModificationExtensions.Translate);
            CheckedValue = new StringField(this, "checkedvalue", 256, AllowStringType.SimpleText);
            UncheckedVariable = new ForeignKeyField<Variable, Option>(this, "uncheckedvariable", true, null);
            UncheckedModification = new EnumField<VariableModification>(this, "uncheckedmodification", VariableModification.None, VariableModificationExtensions.Translate);
            UncheckedValue = new StringField(this, "uncheckedvalue", 256, AllowStringType.SimpleText);
            Ordering = new Field<int>(this, "ordering", 0);
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

using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public enum ConditionType
    {
        None = 0,
        Equal = 1,
        NotEqual = 2,
        Greater = 3,
        Lesser = 4,
        GreaterOrEqual = 5,
        LesserOrEqual = 6,
        Contains = 7,
        DoesNotContain = 8,
    }

    public static class ConditionTypeExtensions
    {
        public static string Translate(this ConditionType type, Translator translator)
        {
            switch (type)
            {
                case ConditionType.None:
                    return translator.Get("Enum.ConditionType.None", "None value in the condition type enum", "None");
                case ConditionType.Equal:
                    return translator.Get("Enum.ConditionType.Equal", "Equal value in the condition type enum", "Equal");
                case ConditionType.NotEqual:
                    return translator.Get("Enum.ConditionType.NotEqual", "Not equal value in the condition type enum", "Not equal");
                case ConditionType.Greater:
                    return translator.Get("Enum.ConditionType.Greater", "Greater value in the condition type enum", "Greater");
                case ConditionType.Lesser:
                    return translator.Get("Enum.ConditionType.Lesser", "Lesser value in the condition type enum", "Lesser");
                case ConditionType.GreaterOrEqual:
                    return translator.Get("Enum.ConditionType.GreaterOrEqual", "Greater or equal value in the condition type enum", "Greater or equal");
                case ConditionType.LesserOrEqual:
                    return translator.Get("Enum.ConditionType.LesserOrEqual", "Lesser or equal value in the condition type enum", "Lesser or equal");
                case ConditionType.Contains:
                    return translator.Get("Enum.ConditionType.Contains", "Contains value in the condition type enum", "Contains");
                case ConditionType.DoesNotContain:
                    return translator.Get("Enum.ConditionType.DoesNotContain", "Does not contain value in the condition type enum", "Does not contain");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Section : DatabaseObject
    {
        public ForeignKeyField<Questionaire, Section> Questionaire { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public Field<int> Ordering { get; private set; }
        public List<Question> Questions { get; private set; }
        public EnumField<ConditionType> ConditionType { get; private set; }
        public ForeignKeyField<Variable, Section> ConditionVariable { get; private set; }
        public StringField ConditionValue { get; private set; }

        public Section() : this(Guid.Empty)
        {
        }

		public Section(Guid id) : base(id)
        {
            Questionaire = new ForeignKeyField<Questionaire, Section>(this, "questionaireid", false, q => q.Sections);
            Name = new MultiLanguageStringField(this, "name");
            Ordering = new Field<int>(this, "ordering", 0);
            ConditionType = new EnumField<ConditionType>(this, "conditiontype", Census.ConditionType.None, ConditionTypeExtensions.Translate);
            ConditionVariable = new ForeignKeyField<Variable, Section>(this, "conditionvariableid", true, null);
            ConditionValue = new StringField(this, "conditionvalue", 256, AllowStringType.SimpleText);
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

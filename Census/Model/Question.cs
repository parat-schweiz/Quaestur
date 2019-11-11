using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public enum QuestionType
    {
        SelectOne = 0,
        SelectMany = 1,
    }

    public static class QuestionTypeExtensions
    {
        public static string Translate(this QuestionType type, Translator translator)
        {
            switch (type)
            {
                case QuestionType.SelectOne:
                    return translator.Get("Enum.QuestionType.SelectOne", "Select one value in the question type enum", "Select one");
                case QuestionType.SelectMany:
                    return translator.Get("Enum.QuestionType.SelectMany", "Select many value in the question type  enum", "Select many");
                default:
                    throw new NotSupportedException(); 
            }
        }
    }

    public class Question : DatabaseObject
    {
        public ForeignKeyField<Section, Question> Section { get; private set; }
        public MultiLanguageStringField Text { get; private set; }
        public EnumField<QuestionType> Type { get; private set; }
        public List<Option> Options { get; private set; }

        public Question() : this(Guid.Empty)
        {
        }

		public Question(Guid id) : base(id)
        {
            Section = new ForeignKeyField<Section, Question>(this, "sectionid", false, s => s.Questions);
            Text = new MultiLanguageStringField(this, "text");
            Type = new EnumField<QuestionType>(this, "type", QuestionType.SelectOne, QuestionTypeExtensions.Translate);
            Options = new List<Option>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Option>("questionid", Id.Value, () => Options);
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var option in database.Query<Option>(DC.Equal("questionid", Id.Value)))
            {
                option.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Section.ToString() + " / " + Text.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Section.GetText(translator) + " / " + Text.Value[translator.Language];
        }

        public Group Owner
        {
            get
            {
                return Section.Value.Owner;
            }
        }

    }
}

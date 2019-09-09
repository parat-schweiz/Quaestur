using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public class Questionaire : DatabaseObject
    {
        public ForeignKeyField<Group, Questionaire> Owner { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public List<Section> Sections { get; private set; }
        public List<Variable> Variables { get; private set; }

        public Questionaire() : this(Guid.Empty)
        {
        }

		public Questionaire(Guid id) : base(id)
        {
            Owner = new ForeignKeyField<Group, Questionaire>(this, "ownerid", false, null);
            Name = new MultiLanguageStringField(this, "name");
            Sections = new List<Section>();
            Variables = new List<Variable>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Section>("questionaireid", Id.Value, () => Sections);
                yield return new MultiCascade<Variable>("questionaireid", Id.Value, () => Variables);
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var variable in database.Query<Variable>(DC.Equal("questionaireid", Id.Value)))
            {
                variable.Delete(database);
            }

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

        public IEnumerable<Question> AllQuestions
        {
            get
            {
                return Sections
                    .OrderBy(s => s.Ordering.Value)
                    .SelectMany(s => s.Questions.OrderBy(q => q.Ordering.Value));
            }
        }

        public Question NextQuestion(Question question)
        {
            var questions = AllQuestions.ToList();
            var index = questions.IndexOf(question);

            if (index >= 0 && index + 1 < questions.Count)
            {
                return questions[index + 1];
            }
            else
            {
                return null;
            }
        }

        public Question LastQuestion(Question question)
        {
            var questions = AllQuestions.ToList();
            var index = questions.IndexOf(question);

            if (index - 1 >= 0)
            {
                return questions[index - 1];
            }
            else
            {
                return null; 
            }
        }
    }
}

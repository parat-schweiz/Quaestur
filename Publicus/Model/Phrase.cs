using System;
using System.Collections.Generic;

namespace Publicus
{
    public class Phrase : DatabaseObject
    {
        public StringField Key { get; set; }
        public StringField Technical { get; set; }
        public StringField Hint { get; set; }
        public List<PhraseTranslation> Translations { get; private set; }

        public Phrase() : this(Guid.Empty)
        {
        }

		public Phrase(Guid id) : base(id)
        {
            Key = new StringField(this, "key", 256);
            Technical = new StringField(this, "technical", 4096, AllowStringType.ParameterizedText);
            Hint = new StringField(this, "hint", 4096);
            Translations = new List<PhraseTranslation>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get 
            {
                yield return new MultiCascade<PhraseTranslation>("phraseid", Id.Value, () => Translations);
            } 
        }

        public override string ToString()
        {
            return Key.Value;
        }

        public override string GetText(Translator translator)
        {
            throw new NotSupportedException();
        }

        public override void Delete(IDatabase database)
        {
            foreach (var translation in database.Query<PhraseTranslation>(DC.Equal("phraseid", Id.Value)))
            {
                translation.Delete(database); 
            }

            database.Delete(this);
        }
    }
}

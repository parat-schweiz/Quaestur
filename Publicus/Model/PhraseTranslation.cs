using System;
using System.Collections.Generic;

namespace Publicus
{
    public enum Language
    {
        Technical = 0,
        English = 1,
        German = 2,
        French = 3,
        Italian = 4,
    }

    public static class LanguageExtensions
    {
        public static string Translate(this Language language, Translator translator)
        {
            switch (language)
            {
                case Language.Technical:
                    return translator.Get("Enum.Language.Technical", "Value 'Technical English' in enum language", "Technical English");
                case Language.English:
                    return translator.Get("Enum.Language.English", "Value 'English' in enum language", "English");
                case Language.German:
                    return translator.Get("Enum.Language.German", "Value 'German' in enum language", "German");
                case Language.French:
                    return translator.Get("Enum.Language.French", "Value 'French' in enum language", "French");
                case Language.Italian:
                    return translator.Get("Enum.Language.Italian", "Value 'Italian' in enum language", "Italian");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class PhraseTranslation : DatabaseObject
    {
        public ForeignKeyField<Phrase, PhraseTranslation> Phrase { get; set; }
        public EnumField<Language> Language { get; set; }
		public StringField Text { get; set; }

        public PhraseTranslation() : this(Guid.Empty)
        {
        }

		public PhraseTranslation(Guid id) : base(id)
        {
            Phrase = new ForeignKeyField<Phrase, PhraseTranslation>(this, "phraseid", false, p => p.Translations);
            Language = new EnumField<Language>(this, "language", Publicus.Language.English, LanguageExtensions.Translate);
            Text = new StringField(this, "text", 4096, AllowStringType.ParameterizedText);
        }

        public override string GetText(Translator translator)
        {
            throw new NotSupportedException();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

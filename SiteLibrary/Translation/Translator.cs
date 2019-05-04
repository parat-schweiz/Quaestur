using System;
using System.Collections.Generic;
using System.Globalization;

namespace SiteLibrary
{
    public class Translator
    {
        public Translation Translation { get; private set; }
        public Language Language { get; private set; }

        public CultureInfo Culture
        {
            get
            {
                switch (Language)
                {
                    case Language.German:
                        return new CultureInfo("de-CH");
                    case Language.French:
                        return new CultureInfo("fr-CH");
                    case Language.English:
                        return new CultureInfo("en-US");
                    case Language.Italian:
                        return new CultureInfo("it-IT");
                    default:
                        return new CultureInfo("en-US");
                }
            }
        }

        public string FormatShortDate(DateTime date)
        {
            return date.ToString("d", Culture);
        }

        public string FormatLongDate(DateTime date)
        {
            switch (Language)
            {
                case Language.German:
                    return date.ToString("d. MMMM yyyy", Culture);
                case Language.French:
                    return date.ToString("d MMMM yyyy", Culture);
                case Language.Technical:
                case Language.English:
                    return date.ToString("MMMM dd yyyy", Culture);
                case Language.Italian:
                    return date.ToString("d MMMM yyyy", Culture);
                default:
                    throw new NotSupportedException();
            }
        }

        public Translator(Translation translation, Language language)
        {
            Translation = translation;
            Language = language;
        }

        public string Get(string key, string hint, string technical, params object[] parameters)
        {
            return Translation.Get(Language, key, hint, technical, parameters);
        }

        public string Get(string key, string hint, string technical, IEnumerable<object> parameters)
        {
            return Translation.Get(Language, key, hint, technical, parameters);
        }

        public List<MultiItemViewModel> CreateLanguagesMultiItem(string key, string hint, string technical, MultiLanguageString values, EscapeMode valueEscapeMode = EscapeMode.Html)
        {
            var result = new List<MultiItemViewModel>();
            result.Add(new MultiItemViewModel(
                ((int)Language.English).ToString(),
                Get(key, hint, technical, Language.English.Translate(this)),
                values.GetValueOrEmpty(Language.English),
                valueEscapeMode));
            result.Add(new MultiItemViewModel(
                ((int)Language.German).ToString(),
                Get(key, hint, technical, Language.German.Translate(this)),
                values.GetValueOrEmpty(Language.German),
                valueEscapeMode));
            result.Add(new MultiItemViewModel(
                ((int)Language.French).ToString(),
                Get(key, hint, technical, Language.French.Translate(this)),
                values.GetValueOrEmpty(Language.French),
                valueEscapeMode));
            result.Add(new MultiItemViewModel(
                ((int)Language.Italian).ToString(),
                Get(key, hint, technical, Language.Italian.Translate(this)),
                values.GetValueOrEmpty(Language.Italian),
                valueEscapeMode));
            return result;
        }
    }

}

using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuaesturApi
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
        public static Language Parse(string value)
        {
            foreach (Language language in Language.English.Priorities())
            {
                if (value == language.ToString().ToLowerInvariant())
                {
                    return language;
                }
            }

            throw new NotSupportedException();
        }

        public static IEnumerable<Language> Priorities(this Language language)
        {
            switch (language)
            {
                case Language.English:
                    yield return Language.English;
                    yield return Language.German;
                    yield return Language.French;
                    yield return Language.Italian;
                    yield return Language.Technical;
                    break;
                case Language.German:
                    yield return Language.German;
                    yield return Language.English;
                    yield return Language.French;
                    yield return Language.Italian;
                    yield return Language.Technical;
                    break;
                case Language.French:
                    yield return Language.French;
                    yield return Language.English;
                    yield return Language.German;
                    yield return Language.Italian;
                    yield return Language.Technical;
                    break;
                case Language.Italian:
                    yield return Language.Italian;
                    yield return Language.French;
                    yield return Language.English;
                    yield return Language.German;
                    yield return Language.Technical;
                    break;
                default:
                    yield return Language.Technical;
                    yield return Language.English;
                    yield return Language.German;
                    yield return Language.French;
                    yield return Language.Italian;
                    break;
            }
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Publicus
{
    public class Translation
    {
        private IDatabase _db;

        public Translation(IDatabase db)
        {
            _db = db; 
        }

        private Guid GetKeyGuid(string key)
        {
            using (var sha = new SHA256Managed())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return new Guid(hash.Part(0, 16));
            }
        }

        public string Get(Language language, string key, string hint, string technical, params object[] parameters)
        {
            return Get(language, key, hint, technical, ((IEnumerable<object>)parameters));
        }

        private string[] ToStringArray(IEnumerable<object> parameters)
        {
            var list = new List<string>();

            foreach (object i in parameters)
            {
                list.Add(i.ToString()); 
            }

            return list.ToArray();
        }

        public string Get(Language language, string key, string hint, string technical, IEnumerable<object> parameters)
        {
            var parametersArray = ToStringArray(parameters);

            lock (_db)
            {
                var id = GetKeyGuid(key);
                var phrase = _db.Query<Phrase>(id);

                if (phrase != null)
                {
                    if (language == Language.Technical)
                    {
                        return string.Format(phrase.Technical.Value, parametersArray);
                    }
                    else
                    {
                        var desiredTranslation = phrase.Translations.FirstOrDefault(t => t.Language.Value == language);

                        if (desiredTranslation != null)
                        {
                            return string.Format(desiredTranslation.Text.Value, parametersArray);
                        }
                        else
                        {
                            var defaultTranslation = phrase.Translations.FirstOrDefault(t => t.Language.Value == Publicus.Language.English);

                            if (defaultTranslation != null)
                            {
                                return string.Format(defaultTranslation.Text.Value, parametersArray);
                            }
                            else
                            {
                                return string.Format(phrase.Technical.Value, parametersArray);
                            }
                        }
                    }
                }
                else
                {
                    phrase = new Phrase(id);
                    phrase.Key.Value = key;
                    phrase.Technical.Value = technical;
                    phrase.Hint.Value = hint;
                    _db.Save(phrase);

                    return string.Format(technical, parametersArray);
                }
            }
        }
    }
}

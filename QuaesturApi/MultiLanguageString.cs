using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuaesturApi
{
    public class MultiLanguageString
    {
        private Dictionary<Language, string> _values;

        public MultiLanguageString(JObject obj)
        {
            _values = new Dictionary<Language, string>();

            foreach (var property in obj.Properties())
            {
                var language = LanguageExtensions.Parse(property.Name);
                _values.Add(language, property.Value.Value<string>());
            }
        }

        public string this[Language language]
        {
            get
            {
                foreach (var l in language.Priorities())
                {
                    if (_values.ContainsKey(l))
                    {
                        return _values[l]; 
                    } 
                }

                return string.Empty;
            }
            set
            {
                if (_values.ContainsKey(language))
                {
                    _values[language] = value;
                }
                else
                {
                    _values.Add(language, value); 
                }
            }
        }
    }
}

using System;

namespace SiteLibrary
{
    public class MultiItemViewModel
    {
        public string Key;
        public string Phrase;
        public string Value;

        public MultiItemViewModel()
        {
        }

        public MultiItemViewModel(string key, string phrase, string value, EscapeMode valueEscapeMode)
        {
            Key = key.EscapeHtml();
            Phrase = phrase.EscapeHtml();
            Value = value.Escape(valueEscapeMode);
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

namespace SiteLibrary
{
    public interface IContentProvider
    { 
        string Prefix { get; }
        string GetContent(string variable);
    }

    public class Templator
    {
        private readonly IEnumerable<IContentProvider> _contentProviders;

        public Templator(params IContentProvider[] contentProviders)
        {
            _contentProviders = contentProviders;
        }

        private string GetContent(string variable)
        {
            var prefix = variable.Split(new string[] { "." }, StringSplitOptions.None)[0];
            var provider = _contentProviders.SingleOrDefault(p => p.Prefix == prefix);

            if (provider == null)
            {
                throw new InvalidOperationException(
                    "No provider for variable " + variable +
                    "; providers: " + string.Join(", ", _contentProviders.Select(p => p.Prefix)));
            }
            else
            {
                return provider.GetContent(variable);
            }
        }

        public string Apply(string text)
        {
            const string pattern = @"§§§([a-zA-Z0-9_\.]+?)§§§";
            var match = Regex.Match(text, pattern);

            while (match.Success)
            {
                var variable = match.Groups[1].Value;
                var content = GetContent(variable);
                text = Regex.Replace(text, "§§§" + variable + "§§§", content);
                match = Regex.Match(text, pattern);
            }

            return text;
        }
    }
}

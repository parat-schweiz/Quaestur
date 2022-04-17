using System;

namespace SiteLibrary
{
    public static class Html
    {
        public static string LinkIfNotEmpty(string text, string link, bool newTab)
        { 
            if (string.IsNullOrEmpty(link))
            {
                return text;
            }
            else
            {
                return Link(text, link, newTab);
            }
        }

        public static string Link(string text, string link, bool newTab)
        {
            if (newTab)
            {
                return string.Format("<a target=\"_blank\" href=\"{0}\">{1}</a>", link, text);
            }
            else
            {
                return string.Format("<a href=\"{0}\">{1}</a>", link, text);
            }
        }

        public static string LinkScript(string text, string script)
        {
            return string.Format("<a href=\"#\" onclick=\"{0}\">{1}</a>", script, text);
        }
    }
}

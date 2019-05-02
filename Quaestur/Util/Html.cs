﻿using System;

namespace Quaestur
{
    public static class Html
    {
        public static string Link(string text, string link)
        {
            return string.Format("<a href=\"{0}\">{1}</a>", link, text);
        }

        public static string LinkScript(string text, string script)
        {
            return string.Format("<a onclick=\"{0}\">{1}</a>", script, text);
        }
    }
}

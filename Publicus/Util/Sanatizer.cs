using System;
using System.Text.RegularExpressions;
using Ganss.XSS;

namespace Publicus
{
    public static class Sanatizer
    {
        public static bool IsImageDataUrl(string value)
        {
            return Regex.IsMatch(value, @"^data:image\/((png)|(svg)|(jpeg)|(gif));base64,[a-zA-Z0-9+/]+={0,2}$");
        }

        public static string SafeHtml(this string value)
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedSchemes.Add("mailto");
            sanitizer.RemovingAttribute += (s, e) =>
                {
                    e.Cancel = (e.Reason == RemoveReason.NotAllowedUrlValue) &&
                        e.Tag.NodeName.ToLowerInvariant() == "img" &&
                        e.Attribute.Name.ToLowerInvariant() == "src" &&
                        IsImageDataUrl(e.Attribute.Value);
                };
            return sanitizer.Sanitize(value);
        }

        public static string SafeLatex(this string value)
        {
            return value;
        }

        public static string RemoveParameters(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            else
            {
                return value
                    .Replace("{", "(")
                    .Replace("}", ")");
            }
        }

        public static string RemoveHtml(this string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            else
            {
                return value
                    .Replace("<", string.Empty)
                    .Replace(">", string.Empty)
                    .Replace("\"", string.Empty);
            }
        }

		public static string EscapeHtml(this string value, bool allowNewLine = false)
		{
            string result = string.Empty;

            foreach (var c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result += c;
                }
                else if (char.IsPunctuation(c))
                {
                    switch (c)
                    {
                        case '&':
                            result += "&amp;";
                            break;
                        case '\"':
                            result += "&quot;";
                            break;
                        case '\'':
                            result += "&#x27;";
                            break;
                        case '/':
                            result += "&#x2F;";
                            break;
                        default:
                            result += c;
                            break;
                    }
                }
                else if (char.IsSymbol(c))
                {
                    switch (c)
                    {
                        case '<':
                            result += "&lt;";
                            break;
                        case '>':
                            result += "&gt;";
                            break;
                        default:
                            result += c;
                            break;
                    }
                }
                else if (char.IsWhiteSpace(c))
                {
                    switch (c)
                    {
                        case '\r':
                            result += '\n';
                            break;
                        case ' ':
                        case '\t':
                            result += ' ';
                            break;
                        default:
                            result += c;
                            break;
                    }
                }
                else
                {
                    switch ((int)c)
                    {
                        case 178:
                        case 179:
                        case 185:
                        case 188:
                        case 189:
                        case 190:
                            result += c;
                            break;
                    }
                }
            }

            if (allowNewLine)
            {
                result = result.Replace("\n", "<br/>");
            }
            else
            {
                result = result.Replace("\n", " ");
            }

            return result;
		}
    }
}

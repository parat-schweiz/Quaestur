using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.IO;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.Html.Dom;
using System.Drawing;
using System.Drawing.Imaging;

namespace SiteLibrary
{
    public class HtmlText
    {
        public string CleanHtml;
        public string PlainText;

        public HtmlText(string dirtyHtml)
        {
            MakeCleanHtml(dirtyHtml);
            MakePlainText(dirtyHtml);
        }

        private void MakePlainText(string dirtyHtml)
        {
            var parser = new HtmlParser();
            var document = parser.ParseDocument(dirtyHtml);
            PlainText = document.DocumentElement.TextContent ?? string.Empty;
        }

        private void MakeCleanHtml(string dirtyHtml)
        {
            CleanHtml = HtmlImage.Inline(dirtyHtml);
        }

        public static string ConcatHtml(string a, string b)
        {
            var p1 = new HtmlParser();
            var d1 = p1.ParseDocument(a);
            var b1 = d1.QuerySelector("body");

            var p2 = new HtmlParser();
            var d2 = p2.ParseDocument(b);
            var b2 = d2.QuerySelector("body");

            foreach (var child in b2.Children)
            {
                b1.AppendChild(child);
            }

            return d1.DocumentElement.OuterHtml;
        }
    }
}

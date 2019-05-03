using System;
using System.Linq;
using System.Collections.Generic;
using System.Net;
using System.IO;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;

namespace Publicus
{
    public class HtmlWorker
    {
        public string CleanHtml;
        public string PlainText;

        private byte[] TryDownload(string source)
        {
            var client = new WebClient();
            try
            {
                return client.DownloadData(source);
            }
            catch
            {
                return null;
            }
        }

        private string ImageType(string source)
        {
            if (source.EndsWith(".png", StringComparison.Ordinal))
            {
                return "image/png";
            }
            else if (source.EndsWith(".jpg", StringComparison.Ordinal) ||
                     source.EndsWith(".jpeg", StringComparison.Ordinal))
            {
                return "image/jpeg";
            }
            else if (source.EndsWith(".gif", StringComparison.Ordinal))
            {
                return "image/gif";
            }
            else if (source.EndsWith(".svg", StringComparison.Ordinal))
            {
                return "image/svg";
            }
            else
            {
                return null;
            }
        }

        public HtmlWorker(string dirtyHtml)
        {
            MakeCleanHtml(dirtyHtml);
            MakePlainText(dirtyHtml);
        }

        private void MakePlainText(string dirtyHtml)
        {
            var parser = new HtmlParser();
            var document = parser.Parse(dirtyHtml);
            PlainText = document.DocumentElement.TextContent ?? string.Empty;
        }

        private void MakeCleanHtml(string dirtyHtml)
        {
            var parser = new HtmlParser();
            var document = parser.Parse(dirtyHtml);

            foreach (IHtmlImageElement image in document.QuerySelectorAll("img"))
            {
                var source = image.Source;

                if (!source.StartsWith("data:", StringComparison.Ordinal))
                {
                    var imageType = ImageType(source);

                    if (imageType != null)
                    {
                        var data = TryDownload(source);

                        if (data != null)
                        {
                            image.Source = string.Format("data:{0};base64,{1}", imageType, Convert.ToBase64String(data));
                        }
                        else
                        {
                            image.Remove();
                        }
                    }
                    else
                    {
                        image.Remove();
                    }
                }
            }

            CleanHtml = document.DocumentElement.OuterHtml;
        }

        public static string ConcatHtml(string a, string b)
        {
            var p1 = new HtmlParser();
            var d1 = p1.Parse(a);
            var b1 = d1.QuerySelector("body");

            var p2 = new HtmlParser();
            var d2 = p2.Parse(b);
            var b2 = d2.QuerySelector("body");

            foreach (var child in b2.Children)
            {
                b1.AppendChild(child);
            }

            return d1.DocumentElement.OuterHtml;
        }
    }
}

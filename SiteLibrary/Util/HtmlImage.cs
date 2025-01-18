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
    public static class HtmlImage
    {
        private static byte[] TryDownload(string source)
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

        private static string ImageType(string source)
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

        private static Image LoadImage(byte[] data)
        {
            using (var memory = new MemoryStream(data))
            {
                return Image.FromStream(memory);
            }
        }

        private static byte[] SaveImage(Image image, ImageFormat format)
        {
            using (var memory = new MemoryStream())
            {
                image.Save(memory, format);
                return memory.ToArray();
            }
        }

        private static byte[] ResizeImage(byte[] data)
        {
            const int MaxSize = 1024;
            var image = LoadImage(data);
            if ((image.Width > MaxSize) || (image.Height > MaxSize))
            {
                int width = image.Width;
                int height = image.Height;
                while ((width > MaxSize) || (height > MaxSize))
                {
                    width /= 2;
                    height /= 2;
                }
                image = image.GetThumbnailImage(width, height, () => false, IntPtr.Zero);
                if (image != null)
                {
                    return SaveImage(image, image.RawFormat);
                }
            }
            return data;
        }

        private static Cache<string, string> _inlinedHtmlCache
            = new Cache<string, string>(TimeSpan.FromHours(4), TimeSpan.FromHours(1));

        public static string Inline(string html)
        {
            if (_inlinedHtmlCache.TryGetValue(html, out string cached))
            {
                return cached;
            }
            else
            {
                _inlinedHtmlCache.Purge();
                var parser = new HtmlParser();
                var document = parser.ParseDocument(html);
                InlineImages(document);
                var inlined = document.DocumentElement.OuterHtml;
                _inlinedHtmlCache.Add(html, inlined);
                return inlined;
            }
        }

        private static void InlineImages(IHtmlDocument document)
        {
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
                            data = ResizeImage(data);
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

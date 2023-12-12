using System;
using System.IO;
using System.Text;
using MimeKit;

namespace BaseLibrary
{
    public abstract class Attachement
    {
        public abstract MimePart Create();
    }

    public class PdfAttachement : Attachement
    {
        public byte[] Data { get; private set; }
        public string FileName { get; private set; }

        public PdfAttachement(byte[] data, string fileName)
        {
            Data = data;
            FileName = fileName + ".pdf";
        }

        public override MimePart Create()
        {
            var documentStream = new MemoryStream(Data);
            var documentPart = new MimePart("application", "pdf");
            documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
            documentPart.ContentType.Name = FileName;
            documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
            documentPart.ContentDisposition.FileName = FileName;
            documentPart.ContentTransferEncoding = ContentEncoding.Base64;
            return documentPart;
        }
    }

    public class TextAttachement : Attachement
    {
        public string Text { get; private set; }
        public string FileName { get; private set; }

        public TextAttachement(string text, string fileName)
        {
            Text = text;
            FileName = fileName;
        }

        public override MimePart Create()
        {
            var documentStream = new MemoryStream(Encoding.UTF8.GetBytes(Text));
            var documentPart = new MimePart("text", "plain");
            documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
            documentPart.ContentType.Name = FileName;
            documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
            documentPart.ContentDisposition.FileName = FileName;
            documentPart.ContentTransferEncoding = ContentEncoding.Base64;
            return documentPart;
        }
    }

    public static class MultipartExtensions
    {
        public static void AddDocument(this Multipart content, Attachement attachement)
        {
            content.Add(attachement.Create());
        }
    }
}

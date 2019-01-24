using System;
using System.IO;
using System.Text;
using MailKit;
using MailKit.Net;
using MailKit.Net.Smtp;
using MimeKit;

namespace Quaestur
{
    public class GpgPublicKeyInfo
    {
        public string Id { get; private set; }

        public GpgPublicKeyInfo(string id)
        {
            Id = id;
        }
    }

    public class GpgPrivateKeyInfo : GpgPublicKeyInfo
    {
        public string Passphrase { get; private set; }

        public GpgPrivateKeyInfo(string id, string passphrase)
            : base(id)
        {
            Passphrase = passphrase;
        }
    }

    public class Mailer
    {
        private const string ErrorSubject = "Error in O2A";
        private const string WarningSubject = "Warning in O2A";
        private Logger _log;
        private Config _config;
        private GpgWrapper _gpg;

        public Mailer(Logger log, Config config)
        {
            _log = log;
            _config = config;
            _gpg = new GpgWrapper(GpgWrapper.LinuxGpgBinaryPath, config.GpgHomedir);
        }

        public void SendError(Exception exception)
        {
            Send(_config.AdminMailAddress, ErrorSubject, exception.ToString());
        }

        public void SendWarning(string body)
        {
            Send(_config.AdminMailAddress, WarningSubject, body);
        }

        public void SendAdmin(string subject, string body)
        {
            Send(_config.AdminMailAddress, subject, body);
        }

        public void Send(string to, string subject, string plainBody)
        {
            _log.Verbose("Sending message to {0}", to);

            try
            {
                var client = new SmtpClient();
                client.SslProtocols = System.Security.Authentication.SslProtocols.None;
                client.Connect(_config.MailServerHost, _config.MailServerPort);
                client.Authenticate(_config.MailAccountName, _config.MailAccountPassword);
                _log.Verbose("Connected to mail server {0}:{1}", _config.MailServerHost, _config.MailServerPort);

                var text = new TextPart("plain") { Text = plainBody };
                text.ContentTransferEncoding = ContentEncoding.QuotedPrintable;

                var message = new MimeMessage();
                message.From.Add(InternetAddress.Parse(_config.SystemMailAddress));
                message.To.Add(InternetAddress.Parse(to));
                message.Subject = subject;
                message.Body = text;
                client.Send(message);

                _log.Info("Message sent to {0}", to);
            }
            catch (Exception exception)
            {
                _log.Error("Error sending mail to {0}", to);
                _log.Error(exception.ToString());
            }
        }

        private Multipart Encrypt(Multipart input, GpgPublicKeyInfo recipientKey)
        {
            var gpgMultipart = new Multipart("encrypted");
            gpgMultipart.ContentType.Parameters.Add("protocol", "application/pgp-encrypted");

            var versionPart = new TextPart("pgp-encrypted");
            versionPart.ContentType.MediaType = "application";
            versionPart.Headers.Add(new Header("Content-Description", "PGP/MIME version identification"));
            versionPart.Text = "Version: 1";
            gpgMultipart.Add(input);

            var multipartStream = new MemoryStream();
            input.WriteTo(multipartStream);
            multipartStream.Position = 0;
            var plainText = Encoding.UTF8.GetString(multipartStream.ToArray()).Replace("\n", "\r\n");

            _gpg.Encrypt(plainText, out string cipherText, recipientKey.Id, true);
            var encryptedPart = new TextPart("octet-stream");
            encryptedPart.ContentType.MediaType = "application";
            encryptedPart.ContentType.Name = "encrypted.asc";
            encryptedPart.ContentDisposition = new ContentDisposition("inline");
            encryptedPart.ContentDisposition.FileName = "encrypted.asc";
            encryptedPart.Text = cipherText;
            encryptedPart.ContentTransferEncoding = ContentEncoding.SevenBit;
            encryptedPart.Headers.Remove(HeaderId.ContentTransferEncoding);
            gpgMultipart.Add(encryptedPart);

            return gpgMultipart;
        }

        private Multipart EncryptAndSign(Multipart input, GpgPrivateKeyInfo senderKey, GpgPublicKeyInfo recipientKey)
        {
            var gpgMultipart = new Multipart("encrypted");
            gpgMultipart.ContentType.Parameters.Add("protocol", "application/pgp-encrypted");

            var versionPart = new TextPart("pgp-encrypted");
            versionPart.ContentType.MediaType = "application";
            versionPart.Headers.Add(new Header("Content-Description", "PGP/MIME version identification"));
            versionPart.Text = "Version: 1";
            gpgMultipart.Add(input);

            var multipartStream = new MemoryStream();
            input.WriteTo(multipartStream);
            multipartStream.Position = 0;
            var plainText = Encoding.UTF8.GetString(multipartStream.ToArray()).Replace("\n", "\r\n");

            _gpg.EncryptAndSign(plainText, out string cipherText, recipientKey.Id, senderKey.Id, true, senderKey.Passphrase);
            var encryptedPart = new TextPart("octet-stream");
            encryptedPart.ContentType.MediaType = "application";
            encryptedPart.ContentType.Name = "encrypted.asc";
            encryptedPart.ContentDisposition = new ContentDisposition("inline");
            encryptedPart.ContentDisposition.FileName = "encrypted.asc";
            encryptedPart.Text = cipherText;
            encryptedPart.ContentTransferEncoding = ContentEncoding.SevenBit;
            encryptedPart.Headers.Remove(HeaderId.ContentTransferEncoding);
            gpgMultipart.Add(encryptedPart);

            return gpgMultipart;
        }

        private Multipart Sign(Multipart input, GpgPrivateKeyInfo senderKey)
        {
            var gpgMultipart = new Multipart("signed");
            gpgMultipart.ContentType.Parameters.Add("micalg", "pgp-sha256");
            gpgMultipart.ContentType.Parameters.Add("protocol", "application/pgp-signature");
            gpgMultipart.Add(input);

            var multipartStream = new MemoryStream();
            input.WriteTo(multipartStream);
            var signedText = Encoding.UTF8.GetString(multipartStream.ToArray()).Replace("\n", "\r\n");

            _gpg.Sign(signedText, out string signatureText, senderKey.Id, SignatureType.DetachSign, true, senderKey.Passphrase);
            var signaturePart = new TextPart("pgp-signature");
            signaturePart.ContentType.MediaType = "application";
            signaturePart.ContentType.Name = "signature.asc";
            signaturePart.ContentDisposition = new ContentDisposition("attachment");
            signaturePart.ContentDisposition.FileName = "signature.asc";
            signaturePart.Text = signatureText;
            signaturePart.ContentTransferEncoding = ContentEncoding.SevenBit;
            signaturePart.Headers.Remove(HeaderId.ContentTransferEncoding);

            gpgMultipart.Add(signaturePart);

            return gpgMultipart;
        }

        public void Send(InternetAddress to, string subject, Multipart content)
        {
            Send(to, null, null, subject, content);
        }

        public void Send(InternetAddress to, GpgPrivateKeyInfo senderKey, GpgPublicKeyInfo recipientKey, string subject, Multipart content)
        {
            Send(new MailboxAddress(Global.Config.SystemMailAddress), to, senderKey, recipientKey, subject, content);
        }

        public void Send(InternetAddress from, InternetAddress to, GpgPrivateKeyInfo senderKey, GpgPublicKeyInfo recipientKey, string subject, Multipart content)
        {
            _log.Verbose("Sending message to {0}", to);

            try
            {
                var client = new SmtpClient();
                client.SslProtocols = System.Security.Authentication.SslProtocols.None;
                client.Connect(_config.MailServerHost, _config.MailServerPort);
                client.Authenticate(_config.MailAccountName, _config.MailAccountPassword);
                _log.Verbose("Connected to mail server {0}:{1}", _config.MailServerHost, _config.MailServerPort);

                if (senderKey != null && recipientKey != null)
                {
                    content = EncryptAndSign(content, senderKey, recipientKey);
                }
                else if (recipientKey != null)
                {
                    content = Encrypt(content, senderKey); 
                }
                else if (senderKey != null)
                {
                    content = Sign(content, senderKey); 
                }

                var message = new MimeMessage();
                message.From.Add(from);
                message.To.Add(to);
                message.Subject = subject;
                message.Body = content;
                client.Send(message);

                _log.Info("Message sent to {0}", to);
            }
            catch (Exception exception)
            {
                _log.Error("Error sending mail to {0}", to);
                _log.Error(exception.ToString());
            }
        }
    }
}

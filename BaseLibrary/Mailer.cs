﻿using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using MailKit;
using MailKit.Net;
using MailKit.Net.Smtp;
using MimeKit;
using BaseLibrary;

namespace BaseLibrary
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
        public const string SystemMailKeyId = "SYSTEM_MAIL_KEY_ID";

        public string Passphrase { get; private set; }

        public GpgPrivateKeyInfo(string id, string passphrase)
            : base(id)
        {
            Passphrase = passphrase;
        }

        public static GpgPrivateKeyInfo SystemMailKey
        {
            get { return new GpgPrivateKeyInfo(SystemMailKeyId, string.Empty); }
        }
    }

    public class Mailer
    {
        private const string ErrorSubject = "Error in O2A";
        private const string WarningSubject = "Warning in O2A";
        private Logger _log;
        private ConfigSectionMail _config;
        private Gpg _gpg;

        public Mailer(Logger log, ConfigSectionMail config, Gpg gpg)
        {
            _log = log;
            _config = config;
            _gpg = gpg;
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

        public void Send(InternetAddress to, string subject, string plainBody)
        {
            _log.Verbose("Sending message to {0}", to.ToString());

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
                message.To.Add(to);
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

        public void Send(string to, string subject, string plainBody)
        {
            Send(InternetAddress.Parse(to), subject, plainBody);
        }

        private MailboxAddress AdminMailAddress
        {
            get { return new MailboxAddress(_config.AdminMailName, _config.AdminMailAddress); }
        }

        private MailboxAddress SystemMailAddress
        {
            get { return new MailboxAddress(_config.SystemMailName, _config.SystemMailAddress); }
        }

        private GpgPublicKeyInfo AdminKeyInfo
        {
            get { return string.IsNullOrEmpty(_config.AdminMailKeyId) ? null : new GpgPublicKeyInfo(_config.AdminMailKeyId); } 
        }

        public void SendAdminEncrypted(string subject, string plainBody, params Attachement[] attachements)
        {
            var content = new Multipart("mixed");
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainBody };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            content.Add(alternative);

            foreach (var attachement in attachements)
            {
                content.AddDocument(attachement); 
            }

            try
            {
                Send(SystemMailAddress, AdminMailAddress, null, AdminKeyInfo, subject, content);
            }
            catch (GpgException)
            {
                // the admin is informed before we get here.
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
            gpgMultipart.Add(versionPart);

            var multipartStream = new MemoryStream();
            input.WriteTo(multipartStream);
            multipartStream.Position = 0;
            var plainText = Encoding.UTF8.GetString(multipartStream.ToArray()).Replace("\n", "\r\n");

            var result = _gpg.Encrypt(plainText, out string cipherText, recipientKey.Id, true);
            result.ThrowOnError();
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
            gpgMultipart.Add(versionPart);

            var multipartStream = new MemoryStream();
            input.WriteTo(multipartStream);
            multipartStream.Position = 0;
            var plainText = Encoding.UTF8.GetString(multipartStream.ToArray()).Replace("\n", "\r\n");

            var result = _gpg.EncryptAndSign(plainText, out string cipherText, recipientKey.Id, senderKey.Id, true, senderKey.Passphrase);
            result.ThrowOnError();

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

            var result = _gpg.Sign(signedText, out string signatureText, senderKey.Id, SignatureType.DetachSign, true, senderKey.Passphrase);
            result.ThrowOnError();

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
            Send(MailboxAddress.Parse(_config.SystemMailAddress), to, senderKey, recipientKey, subject, content);
        }

        public void Send(InternetAddress from, InternetAddress to, GpgPrivateKeyInfo senderKey, GpgPublicKeyInfo recipientKey, string subject, Multipart content)
        {
            Send(Create(from, to, senderKey, recipientKey, subject, content));
        }

        public void Send(MimeMessage message)
        {
            _log.Verbose("Sending message to {0}", message.To[0]);

            try
            {
                var client = new SmtpClient();
                client.SslProtocols = System.Security.Authentication.SslProtocols.None;
                client.Connect(_config.MailServerHost, _config.MailServerPort);
                client.Authenticate(_config.MailAccountName, _config.MailAccountPassword);
                _log.Verbose("Connected to mail server {0}:{1}", _config.MailServerHost, _config.MailServerPort);

                client.Send(message);
                _log.Info("Message sent to {0}", message.To[0]);
            }
            catch (Exception exception)
            {
                _log.Error("Error sending mail to {0}", message.To[0]);
                _log.Error(exception.ToString());
                throw exception;
            }
        }

        public MimeMessage Create(InternetAddress from, InternetAddress to, GpgPrivateKeyInfo senderKey, GpgPublicKeyInfo recipientKey, string subject, Multipart content)
        {
            try
            {
                if (senderKey != null && recipientKey != null)
                {
                    content = EncryptAndSign(content, senderKey, recipientKey);
                }
                else if (recipientKey != null)
                {
                    content = Encrypt(content, recipientKey);
                }
                else if (senderKey != null)
                {
                    content = Sign(content, senderKey);
                }
            }
            catch (GpgException gpgException)
            {
                _log.Error("Gpg operation failed for mail from {0} to {1} sender key {2} recipient key {3} status {4}",
                           from, to, senderKey?.Id ?? "none", recipientKey?.Id ?? "none", gpgException.Message);
                var body = new StringBuilder();
                body.AppendLine("Subject:        " + subject);
                body.AppendLine("From:           " + from);
                body.AppendLine("To:             " + to);
                body.AppendLine("Sender Key:     " + senderKey?.Id ?? "none");
                body.AppendLine("Recipeient Key: " + recipientKey?.Id ?? "none");
                body.AppendLine("Status:         " + gpgException.Message);
                SendAdmin("gpg operation failed", body.ToString());
                throw;
            }

            var message = new MimeMessage();
            message.From.Add(from);
            message.To.Add(to);
            message.Subject = subject;
            message.Body = content;

            return message;
        }
    }
}

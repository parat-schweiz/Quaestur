using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using Nancy.Authentication.Forms;
using Nancy.Security;
using Nancy.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using BaseLibrary;
using QRCoder;
using SiteLibrary;

namespace Hospes
{
    public class LoginLinkViewModel : MasterViewModel
    {
        public LoginLinkViewModel(IDatabase database, Translator translator, Session session)
            : base(translator, 
                translator.Get("LoginLink.Title", "Title of the link login page", "Get login link"), 
                session)
        {
        }
    }

    public class LoginLinkCurrentViewModel
    {
        public string Id;
        public string QrcodeUrl;
        public string LinkUrl;
        public string Status;
        public string Expiry;
        public string Verification;
        public bool CanVerify;
        public string PhraseExpires;
        public string PhraseAuthLevel;
        public string PhraseVerification;
        public string PhraseButtonAccept;
        public string PhraseButtonReject;

        public LoginLinkCurrentViewModel(IDatabase database, Translator translator, Session session, LoginLink loginLink, string status)
        {
            Id = loginLink.Id.ToString();
            QrcodeUrl = "/loginlink/qrcode/" + loginLink.Id.Value;
            LinkUrl = string.Format("{0}/linklogin/{1}/{2}", Global.Config.WebSiteAddress, loginLink.Id.Value, loginLink.Secret.Value.ToHexString());
            Expiry = loginLink.Expires.Value.Subtract(DateTime.UtcNow).TotalSeconds.ToString();
            Verification = loginLink.Verification.Value ?? string.Empty;
            CanVerify = loginLink.Verification.Value != null;
            Status = status;
            PhraseExpires = translator.Get("LoginLink.Expires", "Expires on the login link page", "This login code expires in ");
            PhraseVerification = translator.Get("LoginLink.Verification", "Verification on the login link page", "Check verification: ");
            PhraseButtonAccept = translator.Get("LoginLink.Button.Accept", "Accept button on the login link page", "Accept");
            PhraseButtonReject = translator.Get("LoginLink.Button.Reject", "Reject button on the login link page", "Reject");

            if (session.CompleteAuth)
            {
                PhraseAuthLevel = translator.Get("LoginLink.AuthLevel.Complete", "Complete auth level on the login link page", "Fully authenticated.");
            }
            else
            {
                PhraseAuthLevel = translator.Get("LoginLink.AuthLevel.Partial", "Partial auth level on the login link page", "Partially authenticated.");
            }
        }
    }

    public class LoginLinkModule : QuaesturModule
    {
        public LoginLinkModule()
        {
            this.RequiresAuthentication();

            Get("/loginlink", parameters =>
            {
                return View["View/loginlink.sshtml", new LoginLinkViewModel(Database, Translator, CurrentSession)];
            });
            Get("/loginlink/current/{status}", parameters =>
            {
                string statusString = parameters.status;
                string status = GetLinkStatus();
                var end = DateTime.UtcNow.AddSeconds(10);

                while (statusString == status &&
                       DateTime.UtcNow <= end)
                {
                    Thread.Sleep(250);
                    status = GetLinkStatus();
                }

                return View["View/loginlinkcurrent.sshtml", new LoginLinkCurrentViewModel(Database, Translator, CurrentSession, GetCurrentLoginLink(), status)];
            });
            Get("/loginlink/qrcode/{id}", parameters =>
            {
                string idString = parameters.id;
                var loginLink = Database.Query<LoginLink>(idString);

                if (loginLink != null &&
                    loginLink.Person.Value == CurrentSession.User)
                {
                    var link = string.Format("{0}/linklogin/{1}/{2}", Global.Config.WebSiteAddress, loginLink.Id.Value, loginLink.Secret.Value.ToHexString());
                    QRCodeGenerator qrGenerator = new QRCodeGenerator();
                    QRCodeData qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
                    QRCode qrCode = new QRCode(qrCodeData);
                    Bitmap qrCodeImage = qrCode.GetGraphic(20);

                    var stream = new MemoryStream();
                    qrCodeImage.Save(stream, ImageFormat.Png);
                    stream.Position = 0;

                    return new StreamResponse(() => stream, "image/png");
                }
                else
                {
                    return string.Empty;
                }
            });
            Get("/loginlink/accept/{id}", parameters =>
            {
                string idString = parameters.id;
                var loginLink = Database.Query<LoginLink>(idString);

                if (loginLink != null &&
                    loginLink.Person.Value == CurrentSession.User)
                {
                    loginLink.Confirmed.Value = true;
                    Database.Save(loginLink);
                    return Translate("LoginLink.Accept", "Accept login link response", "Authentication accepted.");
                }

                return string.Empty;
            });
            Get("/loginlink/reject/{id}", parameters =>
            {
                string idString = parameters.id;
                var loginLink = Database.Query<LoginLink>(idString);

                if (loginLink != null &&
                    loginLink.Person.Value == CurrentSession.User)
                {
                    using (var transaction = Database.BeginTransaction())
                    {
                        loginLink.Delete(Database);
                        transaction.Commit();
                    }

                    return Translate("LoginLink.Reject", "Reject login link response", "Authentication rejected.");
                }

                return string.Empty;
            });
        }

        private string GetLinkStatus()
        {
            var loginLink = GetCurrentLoginLink();

            using (var sha = new SHA256Managed())
            {
                using (var serializer = new Serializer())
                {
                    serializer.Write(loginLink.Id.Value);

                    if (loginLink.Verification.Value == null)
                    {
                        serializer.Write(0xdeadbeef);
                    }
                    else
                    {
                        serializer.Write(0x13371337);
                        serializer.WritePrefixed(loginLink.Verification.Value);
                    }

                    return sha.ComputeHash(serializer.Data).ToHexString();
                }
            }
        }

        private LoginLink GetCurrentLoginLink()
        {
            var loginLink = Database
                .Query<LoginLink>(DC.Equal("personid", CurrentSession.User.Id.Value))
                .FirstOrDefault();

            if (loginLink != null &&
                DateTime.UtcNow > loginLink.Expires.Value)
            {
                using (var transaction = Database.BeginTransaction())
                {
                    loginLink.Delete(Database);
                    transaction.Commit();
                }

                loginLink = null; 
            }

            if (loginLink == null)
            {
                loginLink = new LoginLink(Guid.NewGuid());
                loginLink.Person.Value = CurrentSession.User;
                loginLink.CompleteAuth.Value = CurrentSession.CompleteAuth;
                loginLink.TwoFactorAuth.Value = CurrentSession.TwoFactorAuth;
                loginLink.Expires.Value = DateTime.UtcNow.AddMinutes(5);
                loginLink.Secret.Value = Rng.Get(32);
                Database.Save(loginLink);
            }

            return loginLink;
        }
    }
}

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
using SiteLibrary;
using QRCoder;

namespace Quaestur
{
    public class LinkLoginViewModel : MasterViewModel
    {
        public string Id;
        public string Verification;
        public string PhraseWaitingMessage;
        public string PhraseVerification;

        public LinkLoginViewModel(IDatabase database, Translator translator, Session session, LoginLink loginLink)
            : base(database, translator, 
                translator.Get("LinkLogin.Title", "Title of the link login page", "Login device"), 
                session)
        {
            Id = loginLink.Id.ToString();
            Verification = loginLink.Verification.Value;
            PhraseWaitingMessage = translator.Get("LinkLogin.WaitingMessage", "Waiting message on the link login page", "Waiting for confirmation of authentication on other device.");
            PhraseVerification = translator.Get("LinkLogin.Verification", "Verification on the link login page", "Verification is: ");
        }
    }

    public class LinkLoginModule : QuaesturModule
    {
        public LinkLoginModule()
        {
            Get("/linklogin/{id}/{code}", parameters =>
            {
                string idString = parameters.id;
                string codeString = parameters.code;
                var loginLink = Database.Query<LoginLink>(idString);
                var code = codeString.TryParseHexBytes();

                if (loginLink != null &&
                    loginLink.Verification.Value == null &&
                    code != null &&
                    code.AreEqual(loginLink.Secret.Value))
                {
                    loginLink.Verification.Value = Rng.Get(8).ToHexStringGroupFour();
                    Database.Save(loginLink);

                    return View["View/linklogin.sshtml", new LinkLoginViewModel(Database, Translator, CurrentSession, loginLink)];
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Database, Translator,
                        Translate("LinkLogin.Error.Title", "Title in link login error page", "Error"),
                        Translate("LinkLogin.Error.Text", "Text on link login error page", "Invalid authentication link."),
                        Translate("LinkLogin.Error.BackLink", "Back link on link login error page", "Back"),
                        "/")];
                }
            });
            Get("/linklogin/wait/{id}", parameters =>
            {
                string idString = parameters.id;
                var loginLink = Database.Query<LoginLink>(idString);
                var end = DateTime.UtcNow.AddSeconds(10);

                while (loginLink != null &&
                       !loginLink.Confirmed.Value &&
                       DateTime.UtcNow <= end)
                {
                    Thread.Sleep(250);
                    loginLink = Database.Query<LoginLink>(idString);
                }

                if (loginLink != null &&
                    loginLink.Confirmed.Value)
                {
                    Journal(Translate(
                        "Password.Journal.Auth.Process",
                        "Journal entry subject on authentication",
                        "Login Process"),
                        loginLink.Person.Value,
                        "Password.Journal.Auth.Success",
                        "Journal entry when authentication with login link",
                        "Login with password succeeded");

                    using (var transaction = Database.BeginTransaction())
                    {
                        loginLink.Delete(Database);

                        var session = Global.Sessions.Add(loginLink.Person.Value);
                        session.CompleteAuth = loginLink.CompleteAuth.Value;
                        session.TwoFactorAuth = loginLink.TwoFactorAuth.Value;

                        transaction.Commit();

                        return this.LoginAndRedirect(session.Id, DateTime.Now.AddDays(1), "/");
                    }
                }

                return string.Empty;
            });
        }
    }
}

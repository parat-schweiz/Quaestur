﻿using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using SiteLibrary;

namespace Quaestur
{
    public class LoginViewModel : MasterViewModel
    {
        public string Password = string.Empty;
        public string Problems = string.Empty;
        public string Valid = string.Empty;
        public string ReturnUrl = string.Empty;

        public string PhraseFieldUsername;
        public string PhraseFieldPassword;
        public string PhraseButtonLogin;
        public string PhrasePasswordReset;

        public LoginViewModel()
        { 
        }

        public LoginViewModel(IDatabase database, Translator translator, string returnUrl)
            : base(database,
                translator, 
                translator.Get("Login.Title", "Title of the login page", "Login"), 
                null)
        {
            PhraseFieldUsername = translator.Get("Login.Field.Username", "Username field on login page", "Username").EscapeHtml();
            PhraseFieldPassword = translator.Get("Login.Field.Password", "Password field on login page", "Password").EscapeHtml();
            PhraseButtonLogin = translator.Get("Login.Button.Login", "Login button on login page", "Login").EscapeHtml();
            PhrasePasswordReset = translator.Get("Login.Link.PasswordReset", "Password reset link on login page", "Reset password").EscapeHtml();
            ReturnUrl = returnUrl;
        }
    }

    public class LoginModule : QuaesturModule
    {
        private string ValidateReturnUrl(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) &&
                returnUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return returnUrl;
            }
            else
            {
                return string.Empty;
            }
        }

        public LoginModule()
        {
            Get("/login", parameters =>
            {
                var returnUrl = ValidateReturnUrl(Request.Query["returnUrl"]);
                return View["View/login.sshtml", new LoginViewModel(Database, Translator, returnUrl)];
            });
            Post("/login", parameters =>
            {
                var login = this.Bind<LoginViewModel>();
                Global.Throttle.Check(login.UserName, false);
                var returnUrl = ValidateReturnUrl(login.ReturnUrl);
                var result = UserController.Login(Database, Translation, login.UserName, login.Password);
                var newLogin = new LoginViewModel(Database, Translator, returnUrl);

                switch (result.Item2)
                {
                    case LoginResult.WrongLogin:
                        newLogin.Problems = Translate("Login.Result.Wrong", "Wrong username of password message after try at login page", "Username or password wrong.");
                        newLogin.Valid = "is-invalid";
                        Global.Log.Notice("Wrong login with username {0}", login.UserName);
                        Global.Throttle.Fail(login.UserName, false);
                        break;
                    case LoginResult.Locked:
                        newLogin.Problems = Translate("Login.Result.Locket", "User locked message after try at login page", "This account is locked.");
                        newLogin.Valid = "is-invalid";
                        Global.Log.Notice("Login denied due to locked user {0}", result.Item1.UserName);
                        break;
                    case LoginResult.Success:
                        var session = Global.Sessions.Add(result.Item1, Request.Headers.UserAgent);
                        Journal(Translate(
                            "Password.Journal.Auth.Process",
                            "Journal entry subject on authentication",
                            "Login Process"),
                            result.Item1,
                            "Password.Journal.Auth.Success",
                            "Journal entry when authentication with password succeeded",
                            "Login with password succeeded");
                        var sessionExpiry = DateTime.Now.AddSeconds(Global.Config.SessionExpiryAbsoluteSeconds).AddDays(1);

                        session.ReturnUrl = returnUrl;
                        return this.LoginAndRedirect(session.Id, sessionExpiry, "/twofactor/auth");
                }

                return View["View/login.sshtml", newLogin];
            });
            Get("/logout", parameters =>
            {
                if (CurrentSession != null)
                {
                    Global.Sessions.Remove(CurrentSession);
                }

                return Response.AsRedirect("/");
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using QRCoder;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class TwoFactorAuthViewModel : MasterViewModel
    {
        public string Problems;
        public string Valid;
        public string Code;
        public string PhraseFieldCode;
        public string PhraseButtonLogin;

        public TwoFactorAuthViewModel()
            : base()
        { 
        }

        public TwoFactorAuthViewModel(Translator translator, Session session)
            : base(translator,
                   translator.Get("TwoFactor.Auth.Title", "Title of the two-factor authentication page", "Two-factor authentication"),
                   session)
        {
            PhraseFieldCode = translator.Get("TwoFactor.Auth.Field.Code", "Code field on the two-factor authentication page", "Code").EscapeHtml();
            PhraseButtonLogin = translator.Get("TwoFactor.Auth.Button.Login", "Login button on the two-factor authentication page", "Login").EscapeHtml();
        }
    }

    public class TwoFactorEditViewModel : DialogViewModel
    {
        public string Id;
        public string Secret;
        public string Code;
        public string PhraseFieldSecret;
        public string PhraseFieldCode;
        public string PhraseExplaination;
        public bool ShowSecret;

        public TwoFactorEditViewModel()
        { 
        }

        public TwoFactorEditViewModel(Translator translator, Person person)
            : base(translator, 
                   translator.Get("TwoFactor.Edit.Title", "Title of the set two factor dialog", "Set two factor authentication"),
                   "editDialog")
        {
            Id = person.Id.Value.ToString();
            PhraseFieldSecret = translator.Get("TwoFactor.Edit.Field.Secret", "Field 'Secret' in the set two factor dialog", "Secret").EscapeHtml();
            PhraseFieldCode = translator.Get("TwoFactor.Edit.Field.Code", "Field 'Code' in the set two factor dialog", "Code").EscapeHtml();
        }
    }

    public class TwoFactorDisableViewModel
    {
        public string Id;
        public string PhraseConfirmationTitle;
        public string PhraseConfirmationQuestion;

        public TwoFactorDisableViewModel(Translator translator, Person person)
        {
            Id = person.Id.Value.ToString();
            PhraseConfirmationTitle = translator.Get("TwoFactor.Disable.Confirmation.Title", "Title of the disable 2FA confirmation", "Disable 2FA?").EscapeHtml();
            PhraseConfirmationQuestion = translator.Get("TwoFactor.Disable.Confirmation.Question", "Message of the disable 2FA confirmation", "Do you really want to disable two-factor authentication for this user? This might pose a serious security risk!").EscapeHtml();
        }
    }

    public class TwoFactorModule : QuaesturModule
    {
        public TwoFactorModule()
        {
            RequireCompleteLogin();

            Get("/twofactor/disable/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null &&
                    HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                    HasAllAccessOf(person))
                {
                    return View["View/twofactordisable.sshtml",
                        new TwoFactorDisableViewModel(Translator, person)];
                }

                return null;
            });
            Post("/twofactor/disable/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null &&
                    HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                    HasAllAccessOf(person))
                {
                    person.TwoFactorSecret.Value = null;
                    Database.Save(person);
                }

                return null;
            });
            Get("/twofactor/set/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person == CurrentSession.User)
                {
                    if (person.TwoFactorSecret.Value == null)
                    {
                        var model = new TwoFactorEditViewModel(Translator, person);
                        model.Secret = Rng.Get(16).ToBase32String();
                        model.ShowSecret = true;
                        model.PhraseExplaination = Translate(
                            "TwoFactor.Edit.Explaination.Set",
                            "Explaination when setting 2FA",
                            "To enable two-factor authentication, scan the QR code with a TOTP compatible mobile app like <a href=\"https://github.com/freeotp\">FreeOTP</a> and type the time-based authentication code below.");
                        return View["View/twofactorset.sshtml", model];
                    }
                    else
                    {
                        var model = new TwoFactorEditViewModel(Translator, person);
                        model.ShowSecret = false;
                        model.PhraseExplaination = Translate(
                            "TwoFactor.Edit.Explaination.Verify",
                            "Explaination when verifying 2FA",
                            "To enable two-factor authentication on another device, you must first verify your current second factor by typing the code below.");
                        return View["View/twofactorset.sshtml", model];
                    }
                }

                return null;
            });
            Post("/twofactor/set/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<TwoFactorEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (person == CurrentSession.User)
                {
                    if (person.TwoFactorSecret.Value == null)
                    {
                        var key = model.Secret.DecodeBase32();

                        if (key.Length != 16)
                        {
                            status.SetErrorAccessDenied();
                        }

                        var totpData = Global.Security.SecureTotp(key);

                        if (!Global.Security.VerifyTotp(totpData, model.Code))
                        {
                            status.SetValidationError(
                                "Code",
                                "TwoFactor.Edit.Validation.CodeMismatch",
                                "When code does not match when setting TOTP 2FA",
                                "Code incorrect");
                        }

                        if (status.IsSuccess)
                        {
                            person.TwoFactorSecret.Value = totpData;
                            Database.Save(person);
                            Journal(person,
                                "TwoFactor.Journal.Edit",
                                "Journal entry set TOTP 2FA",
                                "Set two-factor authentication");
                        }
                    }
                    else
                    {
                        if (!Global.Security.VerifyTotp(person.TwoFactorSecret.Value, model.Code))
                        {
                            status.SetValidationError(
                                "Code",
                                "TwoFactor.Edit.Validation.CodeMismatch",
                                "When code does not match when setting TOTP 2FA",
                                "Code incorrect");
                        }
                    }
                }
                else
                {
                    status.SetErrorAccessDenied();
                }

                return status.CreateJsonData();
            });
            Post("/twofactor/verify/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<TwoFactorEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);

                if (person == CurrentSession.User &&
                    person.TwoFactorSecret.Value != null)
                {
                    if (Global.Security.VerifyTotp(person.TwoFactorSecret.Value, model.Code))
                    {
                        Journal(person,
                            "TwoFactor.Journal.Show",
                            "Journal entry show TOTP 2FA secret",
                            "Showed two-factor authentication secret");
                        var newModel = new TwoFactorEditViewModel(Translator, person);
                        newModel.Secret = person.TwoFactorSecret.Value.ToBase32String();
                        newModel.ShowSecret = true;
                        newModel.PhraseExplaination = Translate(
                            "TwoFactor.Edit.Explaination.Set",
                            "Explaination when setting 2FA",
                            "To enable two-factor authentication on another device, scan the QR code with your app on the new device. You may verify by entering the code below, but this is not required.");
                        return View["View/twofactorset.sshtml", newModel];
                    }
                }

                return null;
            });
            Get("/twofactor/qr/{key}", parameters =>
            {
                string keyString = parameters.key;

                string totpString = string.Format(
                    "otpauth://totp/{0}?secret={2}&issuer={1}",
                    Global.Config.SiteName,
                    Global.Config.SiteName,
                    keyString);

                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(totpString, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);
                Bitmap qrCodeImage = qrCode.GetGraphic(20);

                var stream = new MemoryStream();
                qrCodeImage.Save(stream, ImageFormat.Png);
                stream.Position = 0;

                return new StreamResponse(() => stream, "image/png");
            });
        }
    }

    public class TwoFactorAuthModule : QuaesturModule
    { 
        public TwoFactorAuthModule()
        {
            this.RequiresAuthentication();

            Get("/twofactor/auth", parameters =>
            {
                if (CurrentSession.User.TwoFactorSecret.Value == null)
                {
                    CurrentSession.CompleteAuth = true;
                    Journal(CurrentSession.User,
                        "TwoFactor.Journal.Auth.Null",
                        "Journal entry login without 2FA",
                        "Logged in without two-factor authentication");

                    foreach (var loginLink in Database
                        .Query<LoginLink>(DC.Equal("personid", CurrentSession.User.Id.Value)))
                    {
                        loginLink.Delete(Database);
                    }

                    if (string.IsNullOrEmpty(CurrentSession.ReturnUrl))
                    {
                        return Response.AsRedirect("/");
                    }
                    else
                    {
                        return Response.AsRedirect(CurrentSession.ReturnUrl);
                    }
                }
                else
                {
                    return View["View/twofactorauth.sshtml",
                        new TwoFactorAuthViewModel(Translator, CurrentSession)];
                }
            });
            Post("/twofactor/auth", parameters =>
            {
                Global.Throttle.Check(CurrentSession.UserName, true);
                var model = this.Bind<TwoFactorAuthViewModel>();

                if (Global.Security.VerifyTotp(CurrentSession.User.TwoFactorSecret.Value, model.Code))
                {
                    CurrentSession.CompleteAuth = true;
                    CurrentSession.TwoFactorAuth = true;
                    Journal(CurrentSession.User,
                        "TwoFactor.Journal.Auth.2FA.Success",
                        "Journal entry login with 2FA",
                        "Logged in with two-factor authentication");
                    if (string.IsNullOrEmpty(CurrentSession.ReturnUrl))
                    {
                        return Response.AsRedirect("/");
                    }
                    else
                    {
                        return Response.AsRedirect(CurrentSession.ReturnUrl);
                    }
                }
                else
                {
                    var newModel = new TwoFactorAuthViewModel(Translator, CurrentSession);
                    newModel.Problems = Translate(
                        "TwoFactor.Auth.Validation.CodeMismatch",
                        "When code does not match when authentication using TOTP 2FA",
                        "Code incorrect");
                    newModel.Valid = "is-invalid";
                    Global.Throttle.Fail(CurrentSession.UserName, true);
                    Journal(CurrentSession.User,
                        "TwoFactor.Journal.Auth.2FA.Failed",
                        "Journal entry login with 2FA failed",
                        "Two-factor authentication failed");

                    return View["View/twofactorauth.sshtml", newModel];
                }
            });
        }
    }
}

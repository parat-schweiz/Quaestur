using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Responses.Negotiation;
using Nancy.Authentication.Forms;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QRCoder;
using OtpNet;
using MimeKit;
using System.Security.Cryptography;

namespace Quaestur
{
    public class PasswordResetRequestViewModel : MasterViewModel
    {
        public string Problems;
        public string Valid;
        public string Email;
        public string PhraseFieldEmail;
        public string PhraseButtonReset;

        public PasswordResetRequestViewModel()
            : base()
        {
        }

        public PasswordResetRequestViewModel(Translator translator)
            : base(translator,
                   translator.Get("PasswordReset.Request.Title", "Title of the password reset request page", "Rest Password"),
                   null)
        {
            PhraseFieldEmail = translator.Get("PasswordReset.Request.Field.Email", "Email field on the password reset page", "Email").EscapeHtml();
            PhraseButtonReset = translator.Get("PasswordReset.Request.Button.Reset", "Reset button on the password reset page", "Reset").EscapeHtml();
        }
    }

    public class PasswordResetChangeViewModel : MasterViewModel
    {
        public string Id;
        public string Time;
        public string Mac;
        public string NewPassword1;
        public string NewPassword2;
        public string PhraseFieldNewPassword1;
        public string PhraseFieldNewPassword2;
        public string PhraseButtonBack;
        public string PhraseButtonChange;

        public PasswordResetChangeViewModel()
        {
        }

        public PasswordResetChangeViewModel(Translator translator, Person person, string time, string mac)
            : base(translator,
                   translator.Get("PasswordReset.Change.Title", "Title of the password reset change dialog", "Reset password"),
                   null)
        {
            Id = person.Id.Value.ToString();
            Time = time;
            Mac = mac;
            PhraseFieldNewPassword1 = translator.Get("PasswordReset.Change.Field.NewPassword1", "Field 'New password' in the reset password change dialog", "New password").EscapeHtml();
            PhraseFieldNewPassword2 = translator.Get("PasswordReset.Change.Field.NewPassword2", "Field 'Repeat password' in the reset password change dialog", "Repeat password").EscapeHtml();
            PhraseButtonBack = translator.Get("PasswordReset.Change.Buttton.Back", "Back button in the reset password change dialog", "Back").EscapeHtml();
            PhraseButtonChange = translator.Get("PasswordReset.Change.Button.Change", "Change password button in the reset password change dialog", "Change").EscapeHtml();
            NewPassword1 = string.Empty;
            NewPassword2 = string.Empty;
        }
    }


    public class PasswordResetModule : QuaesturModule
    {
        private const string PasswordResetTag = "PasswordReset";

        private byte[] ComputeHmac(Guid id, DateTime moment)
        {
            using (var hmac = new HMACSHA256())
            {
                hmac.Key = Global.Config.LinkKey;

                using (var serializer = new Serializer())
                {
                    serializer.WritePrefixed(PasswordResetTag);
                    serializer.Write(id);
                    serializer.Write(moment);
                    return hmac.ComputeHash(serializer.Data);
                }
            }
        }

        public PasswordResetModule()
        {
            Get["/password/reset/request"] = parameters =>
            {
                return View["View/passwordresetrequest.sshtml",
                    new PasswordResetRequestViewModel(Translator)];
            };
            Post["/password/reset/request"] = parameters =>
            {
                var model = this.Bind<PasswordResetRequestViewModel>();

                if (!string.IsNullOrEmpty(model.Email) &&
                    UserController.ValidateMailAddress(model.Email))
                {
                    var address = Database
                        .Query<ServiceAddress>(DC.Equal("address", model.Email))
                        .FirstOrDefault();

                    if (address == null ||
                        address.Person.Value.UserStatus.Value == UserStatus.Locked)
                    {
                        System.Threading.Thread.Sleep(700);
                    }
                    else
                    {
                        var from = new MailboxAddress(
                            Global.Config.SiteName,
                            Global.Config.SystemMailAddress);
                        var to = new MailboxAddress(
                            address.Person.Value.ShortHand,
                            address.Address.Value);
                        var senderKey = new GpgPrivateKeyInfo(
                            Global.Config.SystemMailGpgKeyId,
                            Global.Config.SystemMailGpgKeyPassphrase);
                        var recipientKey = address.GetPublicKey();
                        var content = new Multipart("mixed");
                        var id = address.Person.Value.Id.Value;
                        var time = DateTime.UtcNow;
                        var url = string.Format("{0}/password/reset/change/{1}/{2}/{3}",
                            Global.Config.WebSiteAddress,
                            id.ToString(),
                            time.Ticks.ToString(),
                            ComputeHmac(id, time).ToHexString());
                        var subject = Translate("PasswordReset.Request.Mail.Subject", "Subject in the password reset request mail", "Quaestur Password Reset");
                        var greeting = Translate("PasswordReset.Request.Mail.Greeting", "Greeting in the password reset request mail", "Hello {0},", address.Person.Value.FirstOrUserName);
                        var message = Translate("PasswordReset.Request.Mail.Message", "Message in the password reset request mail", "you can reset your password using the following link:");
                        var regards = Translate("PasswordReset.Request.Mail.Regards", "Regards in the password reset request mail", "Best regards");
                        var plainText = string.Format("{0}\n\n{1}\n{2}\n\n{3}\n{4}", greeting, message, url, regards, Global.Config.SiteName);
                        var alternative = new Multipart("alternative");
                        var plainPart = new TextPart("plain") { Text = plainText };
                        plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                        alternative.Add(plainPart);

                        try
                        {
                            Global.Mail.Send(from, to, senderKey, recipientKey, subject, alternative);
                            Journal(
                                address.Person.Value, 
                                address.Person.Value,
                                "PasswordReset.Request.Journal.LinkSent",
                                "Sent password reset e-mail",
                                "Sent password reset link to {0}",
                                t => address.Address.Value);
                        }
                        catch (Exception exception)
                        {
                            Journal(
                                address.Person.Value,
                                address.Person.Value,
                                "PasswordReset.Request.Journal.LinkSendFail",
                                "Could not sent password reset e-mail",
                                "Could not sent password reset link to {0}",
                                t => address.Address.Value);
                            Global.Log.Error(exception.ToString());
                        }
                    }

                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("PasswordReset.Request.Title", "Title of the message when a password reset is requested", "Password reset"),
                        Translate("PasswordReset.Request.Message", "Text of the message when a password reset is requested", "If your mail address is in our database we have sent a password reset link to that address. Please check your inbox."),
                        Translate("PasswordReset.Request.BackLink", "Link text of the message when a password reset is requested", "Back"),
                        "/")];
                }
                else
                {
                    var newModel = new PasswordResetRequestViewModel(Translator);
                    newModel.Problems = Translate(
                        "PasswordReset.Request.Validation.Invalid",
                        "When e-mail is invalid on password reset",
                        "Address invalid");
                    newModel.Valid = "is-invalid";
                    return View["View/passwordresetrequest.sshtml", newModel];
                }
            };
            Get["/password/reset/change/{id}/{time}/{mac}"] = parameters =>
            {
                string idString = parameters.id;
                string timeString = parameters.time;
                string macString = parameters.mac;

                var person = Database.Query<Person>(idString);
                var mac = macString.TryParseHexBytes();

                if (person != null &&
                    mac != null &&
                    long.TryParse(timeString, out long timeTicks))
                {
                    var time = new DateTime(timeTicks);

                    if (DateTime.UtcNow.Subtract(time).TotalHours < 1d &&
                        ComputeHmac(person.Id.Value, time).AreEqual(mac))
                    {
                        return View["View/passwordresetchange.sshtml",
                            new PasswordResetChangeViewModel(Translator, person, timeString, macString)];
                    }
                }

                return ChangeInvalid();
            };
            Post["/password/reset/change/{id}/{time}/{mac}"] = parameters =>
            {
                string idString = parameters.id;
                string timeString = parameters.time;
                string macString = parameters.mac;

                var person = Database.Query<Person>(idString);
                var mac = macString.TryParseHexBytes();

                if (person != null &&
                    mac != null &&
                    long.TryParse(timeString, out long timeTicks))
                {
                    var time = new DateTime(timeTicks);

                    if (DateTime.UtcNow.Subtract(time).TotalHours < 1d &&
                        ComputeHmac(person.Id.Value, time).AreEqual(mac))
                    {
                        var body = ReadBody();
                        var model = JObject.Parse(body);
                        var newPassword1 = (string)model.Property("NewPassword1").Value;
                        var newPassword2 = (string)model.Property("NewPassword2").Value;
                        var status = CreateStatus();

                        if (newPassword1 != newPassword2)
                        {
                            status.SetValidationError("NewPassword2", "PasswordReset.Change.Validation.NotEqual", "Message when new passwords are not equal at password reset", "New passwords are not equal");
                        }
                        else if (newPassword1.Length < 12)
                        {
                            status.SetValidationError("NewPassword1", "PasswordReset.Change.Validation.TooShort", "Message when new password is to short at password reset", "New password must be at least 12 characters long");
                        }

                        if (status.IsSuccess)
                        {
                            person.PasswordHash.Value = UserController.CreateHash(newPassword1);
                            Database.Save(person);
                            Journal(
                                person,
                                person,
                                "PasswordReset.Change.Journal.Edit",
                                "Journal entry password reset",
                                "Password reset");
                            status.SetSuccess(
                                "PasswordReset.Change.Success",
                                "Success message after successful password reset",
                                "Password changed");
                        }

                        return status.CreateJsonData();
                    };
                }

                return ChangeInvalid();
            };
        }

        private Negotiator ChangeInvalid()
        {
            return View["View/info.sshtml", new InfoViewModel(Translator,
                Translate("PasswordReset.Change.Invalid.Title", "Title of the message when a password reset link is invalid", "Invalid link"),
                Translate("PasswordReset.Change.Invalid.Message", "Text of the message when a password reset link is invalid", "This password reset link is not valid."),
                Translate("PasswordReset.Change.Invalid.BackLink", "Link text of the message when a password reset link is invalid", "Back"),
                "/")];
        }
    }
}

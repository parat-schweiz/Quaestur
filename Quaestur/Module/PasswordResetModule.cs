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
using OtpNet;
using MimeKit;
using System.Security.Cryptography;

namespace Quaestur
{
    public class PasswordResetViewModel : MasterViewModel
    {
        public string Problems;
        public string Valid;
        public string Email;
        public string PhraseFieldEmail;
        public string PhraseButtonReset;

        public PasswordResetViewModel()
            : base()
        { 
        }

        public PasswordResetViewModel(Translator translator, Session session)
            : base(translator,
                   translator.Get("PasswordReset.Title", "Title of the password reset page", "Password Reset"),
                   session)
        {
            PhraseFieldEmail = translator.Get("PasswordReset.Field.Email", "Email field on the password reset page", "Email").EscapeHtml();
            PhraseButtonReset = translator.Get("PasswordReset.Button.Reset", "Reset button on the password reset page", "Reset").EscapeHtml();
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
                    new PasswordResetViewModel(Translator, CurrentSession)];
            };
            Post["/password/reset/request"] = parameters =>
            {
                var model = this.Bind<PasswordResetViewModel>();

                if (UserController.ValidateMailAddress(model.Email))
                {
                    var address = Database
                        .Query<ServiceAddress>(DC.Equal("address", model.Email))
                        .FirstOrDefault();

                    if (address == null)
                    {
                        System.Threading.Thread.Sleep(500);
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
                        var subject = Translate("PasswordReset.Request.Subject", "Subject in the password reset request mail", "Quaestur Password Reset");
                        var greeting = Translate("PasswordReset.Request.Greeting", "Greeting in the password reset request mail", "Hello,");
                        var message = Translate("PasswordReset.Request.Message", "Message in the password reset request mail", "you can reset your password using the following link:");
                        var regards = Translate("PasswordReset.Request.Regards", "Regards in the password reset request mail", "Best regards");
                        var plainText = string.Format("{0}\n\n{1}\n{2}\n\n{3}", greeting, message, url, regards);
                        var alternative = new Multipart("alternative");
                        var plainPart = new TextPart("plain") { Text = plainText };
                        plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                        alternative.Add(plainPart);

                        try
                        {
                            Global.Mail.Send(from, to, senderKey, recipientKey, subject, alternative);
                            Journal(address.Person.Value,
                                "PasswordReset.Journal.LinkSent",
                                "Sent password reset e-mail",
                                "Sent password reset link to {0}",
                                t => address.Address.Value);
                        }
                        catch (Exception exception)
                        {
                            Journal(address.Person.Value,
                                "PasswordReset.Journal.LinkSendFail",
                                "Could not sent password reset e-mail",
                                "Could not sent password reset link to {0}",
                                t => address.Address.Value);
                            Global.Log.Error(exception.ToString());
                        }
                    }

                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("PasswordReset.Requested.Title", "Title of the message when a password reset is requested", "Password reset"),
                        Translate("PasswordReset.Requested.Message", "Text of the message when a password reset is requested", "If your mail address is in our database we have sent a password reset link to that address. Please check your inbox."),
                        Translate("PasswordReset.Requested.BackLink", "Link text of the message when a password reset is requested", "Back"),
                        "/")];
                }
                else
                {
                    var newModel = new PasswordResetViewModel(Translator, CurrentSession);
                    newModel.Problems = Translate(
                        "PasswordReset.Validation.Invalid",
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

                    if (DateTime.UtcNow.Subtract(time).TotalHours < 2d &&
                        ComputeHmac(person.Id.Value, time).AreEqual(mac))
                    {
                        return View["View/passwordresetchange.sshtml",
                            new PasswordResetViewModel(Translator, CurrentSession)];
                    }
                }

                return View["View/info.sshtml", new InfoViewModel(Translator,
                    Translate("PasswordReset.Invalid.Title", "Title of the message when a password reset link is invalid", "Invalid link"),
                    Translate("PasswordReset.Invalid.Message", "Text of the message when a password reset link is invalid", "This password reset link is not valid."),
                    Translate("PasswordReset.Invalid.BackLink", "Link text of the message when a password reset link is invalid", "Back"),
                    "/")];
            };
        }
    }
}

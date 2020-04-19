using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BaseLibrary;
using SiteLibrary;

namespace Publicus
{
    public class UnsubcribeModule : PublicusModule
    {
        private const string UnsubscribeTag = "unsubscribe";

        private static byte[] Mac(DateTime time, Guid id)
        {
            using (var serializer = new Serializer())
            {
                serializer.WritePrefixed(UnsubscribeTag);
                serializer.Write(time);
                serializer.Write(id.ToByteArray());

                using (var hmac = new HMACSHA256())
                {
                    hmac.Key = Global.Config.LinkKey;
                    return hmac.ComputeHash(serializer.Data);
                }
            }
        }

        public static string CreateUnsubscribeLink(Guid id)
        {
            var time = DateTime.UtcNow;
            return string.Format("{0}/unsubscribe/{1}/{2}/{3}", 
                Global.Config.WebSiteAddress,
                id.ToString(),
                time.Ticks.ToString(),
                Mac(time, id).ToHexString());
        }

        private static bool TryParseTicks(string timeString, out DateTime value)
        {
            if (long.TryParse(timeString, out long ticks))
            {
                value = new DateTime(ticks);
                return value > new DateTime(2020, 1, 1);
            }
            else
            {
                value = new DateTime(2020, 1, 1);
                return false;
            }
        }

        public static bool ValidateUnsubscribeLink(string idString, string timeString, string code)
        {
            if (Guid.TryParse(idString, out Guid id) &&
                TryParseTicks(timeString, out DateTime time) &&
                DateTime.UtcNow.Subtract(time).TotalDays <= 30d)
            {
                var expectedMac = Mac(time, id);
                var actualMac = code.TryParseHexBytes();
                return expectedMac.AreEqual(actualMac);
            }
            else
            {
                return false; 
            }
        }

        public UnsubcribeModule()
        {
            Get("/unsubscribe/{id}/{time}/{code}", parameters =>
            {
                string idString = parameters.id;
                string timeString = parameters.time;
                string code = parameters.code;

                if (ValidateUnsubscribeLink(idString, timeString, code))
                {
                    var contact = Database.Query<Contact>(idString);

                    if (contact != null)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            foreach (var tagAssignment in contact.TagAssignments.ToList())
                            {
                                if (tagAssignment.Tag.Value.Usage.Value.HasFlag(TagUsage.Mailing))
                                {
                                    tagAssignment.Delete(Database);
                                }
                            }

                            Journal(contact.FullName,
                                    contact,
                                    "Unsubscribe.Journal",
                                    "Journal entry when unsubscribe from newsletters",
                                    "Unsubscribed from newsletters");
                            transaction.Commit();
                        }

                        return View["View/info.sshtml", new InfoViewModel(Translator,
                            Translate("c.Success.Title", "Title of the message when unsubscribed from newsletters", "Unsubscribed"),
                            Translate("Unsubscribe.Success.Message", "Text of the message when unsubscribed from newsletters", "You have unsubscribed from our newsletters."),
                            Translate("Unsubscribe.Success.BackLink", "Link text of the message when unsubscribed from newsletters", "Back"),
                            "/")];
                    }
                    else
                    {
                        return View["View/info.sshtml", new InfoViewModel(Translator,
                            Translate("Unsubscribe.NotFound.Title", "Title of the message when unsubscribe contact is not found", "Record not found"),
                            Translate("Unsubscribe.NotFound.Message", "Text of the message when unsubscribe contact is not found", "The requested record was not found."),
                            Translate("Unsubscribe.NotFound.BackLink", "Link text of the message when unsubscribe contact is not found", "Back"),
                            "/")];
                    }
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Unsubscribe.InvalidLink.Title", "Title of the message when unsubscribe link is invalid", "Invalid link"),
                        Translate("Unsubscribe.InvalidLink.Message", "Text of the message when unsubscribe link is invalid", "Your unsubscribe link is invalid."),
                        Translate("Unsubscribe.InvalidLink.BackLink", "Link text of the message when unsubscribe link is invalid", "Back"),
                        "/")];
                }
            });
        }
    }
}

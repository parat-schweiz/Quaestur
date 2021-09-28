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

namespace Census
{
    public class LoginModule : CensusModule
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
                string returnUrl = ValidateReturnUrl(Request.Query["returnUrl"]);
                string serviceId = Global.Config.Oauth2Services.First().OAuth2ServiceId;
                return Response.AsRedirect("/oauth2login/" + serviceId + "?returnUrl=" + returnUrl);
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

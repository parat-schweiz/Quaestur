using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class ApiModule : QuaesturModule
    {
        public ApiModule()
        {
            Get("/api/v2/person/list", parameters =>
            {
                return string.Empty;
            });
        }

        private Oauth2Session FindSession()
        {
            string authorization = Request.Headers.Authorization;

            if (!string.IsNullOrEmpty(authorization))
            {
            }

            return null;
        }
    }
}

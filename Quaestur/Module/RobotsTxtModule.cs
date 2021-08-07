using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class RobotsTxtModule : NancyModule
    {
        public RobotsTxtModule()
        {
            Get("/robots.txt", parameters =>
            {
                return "User-agent: *\nDisallow: /";
            });
        }
    }
}

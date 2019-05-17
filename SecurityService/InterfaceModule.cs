using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SecurityService
{
    public class InterfaceModule : NancyModule, IDisposable
    {
        public InterfaceModule()
        {
            Post("/agree", parameters =>
            {
                return Global.Service.Agree(ReadBody());
            });
            Post("/request", parameters =>
            {
                return Global.Service.Request(ReadBody());
            });
        }

        protected string ReadBody()
        {
            using (var reader = new StreamReader(Context.Request.Body))
            {
                return reader.ReadToEnd();
            }
        }

        public void Dispose()
        {
        }
    }
}

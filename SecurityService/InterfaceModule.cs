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
using SecureChannel;

namespace SecurityService
{
    public class InterfaceModule : NancyModule, IDisposable
    {
        public InterfaceModule()
        {
            Post("/agree", parameters =>
            {
                try
                {
                    return Global.Service.Agree(ReadBody());
                }
                catch (SecureChannelException)
                {
                    return new TextResponse(HttpStatusCode.BadRequest, "Malformed request");
                }
                catch (JsonException)
                {
                    return new TextResponse(HttpStatusCode.BadRequest, "Malformed request");
                }
                catch (Exception)
                {
                    return new TextResponse(HttpStatusCode.InternalServerError, "Internal error");
                }
            });
            Post("/request", parameters =>
            {
                try
                {
                    return Global.Service.Request(ReadBody());
                }
                catch (SecureChannelException)
                {
                    return new TextResponse(HttpStatusCode.BadRequest, "Malformed request");
                }
                catch (JsonException)
                {
                    return new TextResponse(HttpStatusCode.BadRequest, "Malformed request");
                }
                catch (Exception)
                {
                    return new TextResponse(HttpStatusCode.InternalServerError, "Internal error");
                }
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

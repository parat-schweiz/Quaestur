using System;
using System.IO;
using Nancy;
using Nancy.Responses;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;
using Nancy.ViewEngines.SuperSimpleViewEngine;
using SiteLibrary;

namespace Quaestur
{
    public class CustomStatusPage : IStatusCodeHandler
    {
        public CustomStatusPage()
        {
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            switch ((int)statusCode / 100)
            {
                case 4:
                case 5:
                    return true;
                default:
                    return false;
            }
        }

        private InfoViewModel GetInfo(HttpStatusCode statusCode, Translator translator)
        { 
            switch (statusCode)
            {
                case HttpStatusCode.NotFound:
                    return new InfoViewModel(translator,
                           translator.Get("CustomStatusPage.NotFound.Title", "Title on the custom status page when status is 404", "Page not found"),
                           translator.Get("CustomStatusPage.NotFound.Text", "Text on the custom status page when status is 404", "This page does not exist."),
                           translator.Get("CustomStatusPage.NotFound.BackLink", "Back link text on the custom status page when status is 404", "Back"),
                           "/");
                default:
                    return new InfoViewModel(translator,
                           translator.Get("CustomStatusPage.Error.Title", "Title on the custom status page on error", "Opps! Error {0}", (int)statusCode),
                           translator.Get("CustomStatusPage.Error.Text", "Text on the custom status page on error", "Something went wrong. If this problem persists, please contact the administrator of this page at {0}.", Global.Config.Mail.AdminMailAddress),
                           translator.Get("CustomStatusPage.Error.BackLink", "Back link text on the custom status page on error", "Back"),
                           "/");
            }
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            Global.Log.Error("Custom status page called with status code {0}", (int)statusCode);

            try
            {
                using (var database = Global.CreateDatabase())
                {
                    var translation = new Translation(database);
                    var translator = new Translator(translation, Language.English);
                    //var response = RenderView(context, "View/info.sshtml", GetInfo(statusCode, translator));
                    context.Response.StatusCode = statusCode;
                }
            }
            catch (Exception exception)
            {
                Global.Log.Error("Custom status page threw exception: {0}", exception.Message);
                context.Response = new TextResponse("Error in error handling");
                context.Response.StatusCode = statusCode;
            }
        }
    }
}

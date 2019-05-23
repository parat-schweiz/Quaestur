using System;
using Nancy;
using Nancy.Responses;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;
using SiteLibrary;

namespace Publicus
{
    public class CustomStatusPage : IStatusCodeHandler
    {
        private readonly IViewFactory _factory;

        public CustomStatusPage(IViewFactory factory)
        {
            _factory = factory;
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
                case HttpStatusCode.Unauthorized:
                    return new InfoViewModel(translator,
                           translator.Get("CustomStatusPage.Unauthorized.Title", "Title on the custom status page when status is 401", "Unauthorized"),
                           translator.Get("CustomStatusPage.Unauthorized.Text", "Text on the custom status page when status is 401", "Access to this page is not authorized."),
                           translator.Get("CustomStatusPage.Unauthorized.BackLink", "Back link text on the custom status page when status is 401", "Back"),
                           "/");
                case HttpStatusCode.Forbidden:
                    return new InfoViewModel(translator,
                           translator.Get("CustomStatusPage.Forbidden.Title", "Title on the custom status page when status is 403", "Forbidden"),
                           translator.Get("CustomStatusPage.Forbidden.Text", "Text on the custom status page when status is 403", "Access to this page is forbidden."),
                           translator.Get("CustomStatusPage.Forbidden.BackLink", "Back link text on the custom status page when status is 403", "Back"),
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
            Global.Log.Error(
                "Custom status page called for request {0} {1} with status code {2}",
                context.Request.Method, context.Request.Url, (int)statusCode);

            try
            {
                using (var database = Global.CreateDatabase())
                {
                    var translation = new Translation(database);
                    var translator = new Translator(translation, Language.English);
                    var viewContext = new ViewLocationContext { Context = context };
                    context.Response = _factory.RenderView("View/info.sshtml", GetInfo(statusCode, translator), viewContext);
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

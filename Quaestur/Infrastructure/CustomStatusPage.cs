using System;
using System.IO;
using Nancy;
using Nancy.Responses;
using Nancy.ErrorHandling;
using Nancy.ViewEngines;
using SiteLibrary;

namespace Quaestur
{
    public class CustomStatusPage : IStatusCodeHandler
    {
        private readonly IViewRenderer _viewRenderer;

        public CustomStatusPage(IViewRenderer viewRenderer)
        {
            _viewRenderer = viewRenderer;
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
                    var response = _viewRenderer.RenderView(context, "View/info.sshtml", GetInfo(statusCode, translator));
                    response.StatusCode = statusCode;
                    context.Response = response;
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

    public class ErrorHtmlPageResponse : HtmlResponse
    {
        public string Title { get; private set; }
        public string Text { get; private set; }

        public ErrorHtmlPageResponse(HttpStatusCode statusCode, string title, string text)
        {
            StatusCode = statusCode;
            ContentType = "text/html; charset=utf-8";
            Contents = Render;
            Title = title;
            Text = text;
        }

        void Render(Stream stream)
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine("<html>");
                writer.WriteLine("<head>");
                writer.WriteLine("<meta charset=\"UTF-8\"/>");
                writer.WriteLine("<title>{0}</title>", Global.Config.SiteName);
                writer.WriteLine("<link rel=\"stylesheet\" href=\"/assets/main.css\"/>");
                writer.WriteLine("</head>");
                writer.WriteLine("<body>");
                writer.WriteLine("<h1>" + Title + "</h1>");
                writer.WriteLine("<p>" + Text + "</p>");
                writer.WriteLine("</body>");
                writer.WriteLine("</html>");
                writer.Flush();
            }
        }
    }
}

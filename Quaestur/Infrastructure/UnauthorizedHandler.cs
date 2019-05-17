using System;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Responses;
using Nancy.ViewEngines;
using Nancy.ViewEngines.SuperSimpleViewEngine;
using SiteLibrary;

namespace Quaestur
{
    public class ForbiddenHandler : IStatusCodeHandler
    {
        public ForbiddenHandler()
        {
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            return statusCode == HttpStatusCode.Forbidden;
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            using (var database = Global.CreateDatabase())
            {
                var translation = new Translation(database);

                if (context.CurrentUser is Session session)
                {
                    if (!session.CompleteAuth)
                    {
                        context.Response = new RedirectResponse("/twofactor/auth");
                    }
                    else
                    {
                        var translator = new Translator(translation, session.User.Language.Value);
                        Render(context, translator);
                    }
                }
                else
                {
                    var translator = new Translator(translation, Language.English);
                    Render(context, translator);
                }
            }
        }

        private void Render(NancyContext context, Translator translator)
        {
            var model = new InfoViewModel(translator,
                translator.Get("Forbidden.Title", "Title of the message on the unauthorized page", "Forbidden"),
                translator.Get("Forbidden.Message", "Text of the message on the unauthorized page", "You are forbidden from viewing this page."),
                translator.Get("Forbidden.BackLink", "Link text of the message on the unauthorized page", "Back"),
                "/");
            context.Response = HttpStatusCode.Forbidden;
        }
    }
}

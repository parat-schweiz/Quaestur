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
    public class SurveyModule : CensusModule
    {
        private const string SurveySessionKey = "surveysession";

        public SurveySession GetSession()
        {
            SurveySession session = null;

            if (Session[SurveySessionKey] != null)
            {
                Guid sessionId = Guid.Parse(Session[SurveySessionKey] as string);
                session = Global.SurveySessions.Get(sessionId);
            }

            if (session != null)
            {
                return session;
            }
            else
            {
                session = new SurveySession();
                Global.SurveySessions.Add(session);
                Session[SurveySessionKey] = session.Id.ToString();
                return session;
            }
        }

        public SurveyModule()
        {
            Get("/q/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);
                var firstQuestion = questionaire
                    .Sections.OrderBy(s => s.Ordering).First()
                    .Questions.OrderBy(q => q.Ordering).First();

                var session = GetSession();
                session.CurrentQuestionId = firstQuestion.Id;
                return Response.AsRedirect("/q");
            });
            Get("/q", parameters =>
            {
                var session = GetSession();
                return session.Id.ToString();
            });
        }
    }
}

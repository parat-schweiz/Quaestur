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
    public class SurveyOptionViewModel
    {
        public string Id;
        public string Text;

        public SurveyOptionViewModel(Translator translator, Option option)
        {
            Id = option.Id.Value.ToString();
            Text = option.Text.Value[translator.Language];
        }
    }

    public class SurveyQuestionViewModel
    {
        public string Title;
        public string Id;
        public string Text;
        public List<SurveyOptionViewModel> Options;

        public SurveyQuestionViewModel(Translator translator, Question question)
        {
            Title = question.Section.Value.Questionaire.Value.Name.Value[translator.Language];
            Id = question.Id.Value.ToString();
            Text = question.Text.Value[translator.Language];
            Options = new List<SurveyOptionViewModel>(question
                .Options.OrderBy(o => o.Ordering.Value)
                .Select(o => new SurveyOptionViewModel(translator, o)));
        }
    }

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
                session.Language = Language.German;
                return Response.AsRedirect("/q");
            });
            Get("/q", parameters =>
            {
                var session = GetSession();
                var question = Database.Query<Question>(session.CurrentQuestionId);

                if (question != null)
                {
                    switch (question.Type.Value)
                    {
                        case QuestionType.SelectOne:
                            return View["View/survery_question_selectone.sshtml",
                                new SurveyQuestionViewModel(Translator, question)];
                        case QuestionType.SelectMany:
                            return View["View/survery_question_selectmany.sshtml",
                                new SurveyQuestionViewModel(Translator, question)];
                        default:
                            throw new NotSupportedException(); 
                    }
                }

                return string.Empty;
            });
        }
    }
}

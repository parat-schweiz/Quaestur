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
        public bool ShowNext;
        public bool ShowBack;
        public string PhraseButtonBack;
        public string PhraseButtonNext;

        public SurveyQuestionViewModel(Translator translator, Question question)
        {
            Title = question.Section.Value.Questionaire.Value.Name.Value[translator.Language];
            Id = question.Id.Value.ToString();
            Text = question.Text.Value[translator.Language];
            Options = new List<SurveyOptionViewModel>(question
                .Options.OrderBy(o => o.Ordering.Value)
                .Select(o => new SurveyOptionViewModel(translator, o)));
            ShowNext = question.Questionaire.NextQuestion(question) != null;
            ShowBack = question.Questionaire.LastQuestion(question) != null;
            PhraseButtonBack = translator.Get("Survey.Question.Button.Back", "Back button the survey question page", "Back").EscapeHtml();
            PhraseButtonNext = translator.Get("Survey.Question.Button.Next", "Next button the survey question page", "Next").EscapeHtml();
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

        private void NoteQuestionResult(PostStatus status, Question question, JObject result)
        { 
        }

        public SurveyModule()
        {
            Get("/q/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);
                var firstQuestion = questionaire
                    .AllQuestions.First();
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
            Post("/q/next", parameters =>
            {
                var session = GetSession();
                var question = Database.Query<Question>(session.CurrentQuestionId);
                var status = CreateStatus();

                if (question != null)
                {
                    var result = JObject.Parse(ReadBody());
                    NoteQuestionResult(status, question, result);

                    if (status.IsSuccess)
                    {
                        var nextQuestion = question.Questionaire.NextQuestion(question);

                        if (nextQuestion != null)
                        {
                            session.CurrentQuestionId = nextQuestion.Id; 
                        }
                        else
                        {
                            status.SetError("Survey.Question.NoNextQuestion", "No next question survey page", "Cannot go beyond last question");
                        }
                    }
                }
                else
                {
                    status.SetError("Survey.Question.NotFound", "Question not found on survey page", "Question not found");
                }

                return status.CreateJsonData();
            });
            Post("/q/back", parameters =>
            {
                var session = GetSession();
                var question = Database.Query<Question>(session.CurrentQuestionId);
                var status = CreateStatus();

                if (question != null)
                {
                    var result = JObject.Parse(ReadBody());
                    NoteQuestionResult(status, question, result);

                    if (status.IsSuccess)
                    {
                        var lastQuestion = question.Questionaire.LastQuestion(question);

                        if (lastQuestion != null)
                        {
                            session.CurrentQuestionId = lastQuestion.Id;
                        }
                        else
                        {
                            status.SetError("Survey.Question.NoLastQuestion", "No last question survey page", "Cannot go back beyond frist question");
                        }
                    }
                }
                else
                {
                    status.SetError("Survey.Question.NotFound", "Question not found on survey page", "Question not found");
                }

                return status.CreateJsonData();
            });
        }
    }
}

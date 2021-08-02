using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Hospes
{
    public class PersonDetailPointsItemViewModel
    {
        public string Id;
        public string Budget;
        public string Moment;
        public string Amount;
        public string Running;
        public string Reason;
        public string Editable;
        public string Bold;
        public string DeleteVisible;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailPointsItemViewModel(Translator translator, long running)
        {
            Id = string.Empty;
            Budget = string.Empty;
            Moment = string.Empty;
            Amount = string.Empty;
            Running = running.FormatThousands();
            Reason = translator.Get("Person.Detail.Points.Title.Balance", "Title of the current balancy in points tab of person details", "Balance");
            Bold = "bold";
            Editable = string.Empty;
            DeleteVisible = "invisible";
            PhraseDeleteConfirmationQuestion = string.Empty;
        }

        public PersonDetailPointsItemViewModel(Translator translator, PointsTally tally)
        {
            Id = string.Empty;
            Budget = string.Empty;
            Moment = tally.FromDate.Value.FormatSwissDateDay() + " - " + 
                     tally.UntilDate.Value.FormatSwissDateDay();
            Amount = tally.Considered.Value.FormatThousands();
            Running = tally.ForwardBalance.Value.FormatThousands();
            Reason = translator.Get("Person.Detail.Points.Title.Tally", "Title of a tally in points tab of person details", "Tally");
            Bold = "bold";
            Editable = string.Empty;
            DeleteVisible = "invisible";
            PhraseDeleteConfirmationQuestion = string.Empty;
        }

        public PersonDetailPointsItemViewModel(Translator translator, Points points, long running, bool allowEdit)
        {
            Id = points.Id.Value.ToString();
            Budget = points.Budget.Value.GetText(translator);
            Moment = points.Moment.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            Amount = points.Amount.Value.FormatThousands();
            Running = running.FormatThousands();
            Bold = string.Empty;
            Editable = allowEdit ? "editable" : "accessdenied";
            DeleteVisible = !allowEdit ? "invisible" : string.Empty;

            if (!string.IsNullOrEmpty(points.Url))
            {
                Reason = string.Format("<a target=\"_blank\" href=\"{0}\">{1}</a>",
                    points.Url.Value.Escape(),
                    points.Reason.Value.Escape());
            }
            else
            {
                Reason = points.Reason.Value.Escape();
            }

            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.Points.Delete.Confirm.Question", "Delete points confirmation question", "Do you really wish to delete points {0}?", points.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonDetailPointsViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailPointsItemViewModel> List;
        public string PhraseHeaderBudget;
        public string PhraseHeaderMoment;
        public string PhraseHeaderAmount;
        public string PhraseHeaderRunning;
        public string PhraseHeaderReason;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailPointsViewModel(IDatabase database, Translator translator, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailPointsItemViewModel>();

            var editAccess = session.HasAccess(person, PartAccess.Points, AccessRight.Write);
            var tallyQueue = new Queue<PointsTally>(database
                .Query<PointsTally>(DC.Equal("personid", person.Id.Value))
                .OrderBy(t => t.UntilDate.Value));
            var pointsQueue = new Queue<Points>(database
                .Query<Points>(DC.Equal("ownerid", person.Id.Value))
                .OrderBy(p => p.Moment.Value));
            long running = 0;

            while (pointsQueue.Count > 0)
            {
                if ((tallyQueue.Count > 0) &&
                    (pointsQueue.Peek().Moment.Value >
                    tallyQueue.Peek().UntilDate.Value.Date.AddDays(1)))
                {
                    var tally = tallyQueue.Dequeue();
                    running = tally.ForwardBalance.Value;
                    List.Add(new PersonDetailPointsItemViewModel(translator, tally));
                }
                else
                {
                    var allowEdit = editAccess && (tallyQueue.Count < 1);
                    var points = pointsQueue.Dequeue();
                    running += points.Amount;
                    List.Add(new PersonDetailPointsItemViewModel(translator, points, running, allowEdit));
                }
            }

            while (tallyQueue.Count > 0)
            {
                var tally = tallyQueue.Dequeue();
                running = tally.ForwardBalance.Value;
                List.Add(new PersonDetailPointsItemViewModel(translator, tally));
            }

            List.Add(new PersonDetailPointsItemViewModel(translator, running));
            List.Reverse();

            PhraseHeaderBudget = translator.Get("Person.Detail.Points.Header.Budget", "Column 'Budget' on the points tab of the person detail page", "Budget");
            PhraseHeaderMoment = translator.Get("Person.Detail.Points.Header.Moment", "Column 'Moment' on the points tab of the person detail page", "Moment");
            PhraseHeaderAmount = translator.Get("Person.Detail.Points.Header.Amount", "Column 'Amount' on the points tab of the person detail page", "Amount");
            PhraseHeaderRunning = translator.Get("Person.Detail.Points.Header.Running", "Column 'Running' on the points tab of the person detail page", "Running");
            PhraseHeaderReason = translator.Get("Person.Detail.Points.Header.Reason", "Column 'Reason' on the points tab of the person detail page", "Reason");
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.Points.Delete.Confirm.Title", "Delete points confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class PersonDetailPointsModule : QuaesturModule
    {
        public PersonDetailPointsModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/points/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Points, AccessRight.Read))
                    {
                        return View["View/persondetail_points.sshtml",
                            new PersonDetailPointsViewModel(Database, Translator, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDetailPointsItemViewModel
    {
        public string Id;
        public string Budget;
        public string Moment;
        public string Amount;
        public string Reason;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailPointsItemViewModel(IDatabase database, Translator translator, Points points)
        {
            Id = points.Id.Value.ToString();
            Budget = points.Budget.Value.GetText(translator);
            Moment = points.Moment.Value.ToString("dd.MM.yyyy");
            Amount = points.Amount.Value.ToString();

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
        public string PhraseHeaderReason;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailPointsViewModel(IDatabase database, Translator translator, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailPointsItemViewModel>(database
                .Query<Points>(DC.Equal("ownerid", person.Id.Value))
                .OrderByDescending(p => p.Moment.Value)
                .Select(p => new PersonDetailPointsItemViewModel(database, translator, p)));
            Editable =
                session.HasAccess(person, PartAccess.Points, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderBudget = translator.Get("Person.Detail.Points.Header.Budget", "Column 'Budget' on the points tab of the person detail page", "Budget");
            PhraseHeaderMoment = translator.Get("Person.Detail.Points.Header.Moment", "Column 'Moment' on the points tab of the person detail page", "Moment");
            PhraseHeaderAmount = translator.Get("Person.Detail.Points.Header.Amount", "Column 'Amount' on the points tab of the person detail page", "Amount");
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

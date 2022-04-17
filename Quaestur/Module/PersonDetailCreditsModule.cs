using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class PersonDetailCreditsItemViewModel
    {
        public string Id;
        public string Moment;
        public string Amount;
        public string Running;
        public string Reason;
        public string Editable;
        public string Bold;
        public string DeleteVisible;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailCreditsItemViewModel(Translator translator, long running)
        {
            Id = string.Empty;
            Moment = string.Empty;
            Amount = string.Empty;
            Running = running.FormatThousands();
            Reason = translator.Get("Person.Detail.Credits.Title.Balance", "Title of the current balancy in credits tab of person details", "Balance");
            Bold = "bold";
            Editable = string.Empty;
            DeleteVisible = "invisible";
            PhraseDeleteConfirmationQuestion = string.Empty;
        }

        public PersonDetailCreditsItemViewModel(Translator translator, Credits credits, long running, bool allowEdit)
        {
            Id = credits.Id.Value.ToString();
            Moment = credits.Moment.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            Amount = credits.Amount.Value.FormatThousands();
            Running = running.FormatThousands();
            Bold = string.Empty;
            Editable = allowEdit ? "editable" : "accessdenied";
            DeleteVisible = !allowEdit ? "invisible" : string.Empty;

            if (!string.IsNullOrEmpty(credits.Url))
            {
                Reason = string.Format("<a target=\"_blank\" href=\"{0}\">{1}</a>",
                    credits.Url.Value.Escape(),
                    credits.Reason.Value.Escape());
            }
            else
            {
                Reason = credits.Reason.Value.Escape();
            }

            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.Credits.Delete.Confirm.Question", "Delete credits confirmation question", "Do you really wish to delete credits {0}?", credits.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonDetailCreditsViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailCreditsItemViewModel> List;
        public string PhraseHeaderMoment;
        public string PhraseHeaderAmount;
        public string PhraseHeaderRunning;
        public string PhraseHeaderReason;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailCreditsViewModel(IDatabase database, Translator translator, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailCreditsItemViewModel>();

            var editAccess = session.HasAccess(person, PartAccess.Credits, AccessRight.Write);
            var creditsQueue = new Queue<Credits>(database
                .Query<Credits>(DC.Equal("ownerid", person.Id.Value))
                .OrderBy(p => p.Moment.Value));
            long running = 0;

            while (creditsQueue.Count > 0)
            {
                var credits = creditsQueue.Dequeue();
                var allowEdit = editAccess && (DateTime.UtcNow.Subtract(credits.Moment).TotalDays <= 30d);
                    running += credits.Amount;
                List.Add(new PersonDetailCreditsItemViewModel(translator, credits, running, allowEdit));
            }

            List.Add(new PersonDetailCreditsItemViewModel(translator, running));
            List.Reverse();

            PhraseHeaderMoment = translator.Get("Person.Detail.Credits.Header.Moment", "Column 'Moment' on the credits tab of the person detail page", "Moment");
            PhraseHeaderAmount = translator.Get("Person.Detail.Credits.Header.Amount", "Column 'Amount' on the credits tab of the person detail page", "Amount");
            PhraseHeaderRunning = translator.Get("Person.Detail.Credits.Header.Running", "Column 'Running' on the credits tab of the person detail page", "Running");
            PhraseHeaderReason = translator.Get("Person.Detail.Credits.Header.Reason", "Column 'Reason' on the credits tab of the person detail page", "Reason");
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.Credits.Delete.Confirm.Title", "Delete credits confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class PersonDetailCreditsModule : QuaesturModule
    {
        public PersonDetailCreditsModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/credits/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Credits, AccessRight.Read))
                    {
                        return View["View/persondetail_credits.sshtml",
                            new PersonDetailCreditsViewModel(Database, Translator, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

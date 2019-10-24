using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class PersonDetailPointsTallyItemViewModel
    {
        public string Id;
        public string FromDate;
        public string UntilDate;
        public string CreatedDate;
        public string Considered;
        public string ForwardBalance;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailPointsTallyItemViewModel(Translator translator, PointsTally pointsTally)
        {
            Id = pointsTally.Id.Value.ToString();
            FromDate = pointsTally.FromDate.Value.ToString("dd.MM.yyyy");
            UntilDate = pointsTally.UntilDate.Value.ToString("dd.MM.yyyy");
            CreatedDate = pointsTally.CreatedDate.Value.ToString("dd.MM.yyyy");
            Considered = pointsTally.Considered.Value.FormatThousands();
            ForwardBalance = pointsTally.ForwardBalance.Value.ToString();
            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.PointsTallys.Delete.Confirm.Question", "Delete pointsTally confirmation question", "Do you really wish to delete pointsTally {0}?", pointsTally.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonDetailPointsTallyingViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailPointsTallyItemViewModel> List;
        public string PhraseHeaderFromDate;
        public string PhraseHeaderUntilDate;
        public string PhraseHeaderCreatedDate;
        public string PhraseHeaderConsidered;
        public string PhraseHeaderForwardBalance;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailPointsTallyingViewModel(Translator translator, IDatabase database, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailPointsTallyItemViewModel>(database
                .Query<PointsTally>(DC.Equal("personid", person.Id.Value))
                .OrderBy(t => t.FromDate.Value)
                .Select(t => new PersonDetailPointsTallyItemViewModel(translator, t)));
            Editable =
                session.HasAccess(person, PartAccess.Billing, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderFromDate = translator.Get("Person.Detail.PointsTally.Header.FromDate", "Column 'From date' on the points tally tab of the person detail page", "From date").EscapeHtml();
            PhraseHeaderUntilDate = translator.Get("Person.Detail.PointsTally.Header.UntilDate", "Column 'Until date' on the points tally tab of the person detail page", "Until date").EscapeHtml();
            PhraseHeaderCreatedDate = translator.Get("Person.Detail.PointsTally.Header.CreatedDate", "Column 'CreatedDate' on the points tally tab of the person detail page", "Created").EscapeHtml();
            PhraseHeaderConsidered = translator.Get("Person.Detail.PointsTally.Header.Considered", "Column 'Considered' on the points tally tab of the person detail page", "Considered").EscapeHtml();
            PhraseHeaderForwardBalance = translator.Get("Person.Detail.PointsTally.Header.ForwardBalance", "Column 'Forward balance' on the points tally tab of the person detail page", "Forward balance").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.PointsTally.Delete.Confirm.Title", "Delete points tally confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class PersonDetailPointsTallyingModule : QuaesturModule
    {
        public PersonDetailPointsTallyingModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/pointstally/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Billing, AccessRight.Read))
                    {
                        return View["View/persondetail_pointstally.sshtml", 
                            new PersonDetailPointsTallyingViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

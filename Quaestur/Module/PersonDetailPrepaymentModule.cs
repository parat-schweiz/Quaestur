using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class PersonDetailPrepaymentItemViewModel
    {
        public string Id;
        public string Reason;
        public string Moment;
        public string Amount;
        public string Balance;
        public string Type;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailPrepaymentItemViewModel(Translator translator, Prepayment prepayment, decimal balance)
        {
            Id = prepayment.Id.Value.ToString();
            var reason = prepayment.Reason.Value.EscapeHtml();
            var url = prepayment.Url.Value.EscapeHtml();
            Reason = Html.LinkIfNotEmpty(reason, url, true);
            Moment = prepayment.Moment.Value.FormatSwissDateDay();
            Amount = prepayment.Amount.Value.FormatMoney();
            Type = prepayment.ReferenceType.Value.Translate(translator);
            Balance = balance.FormatMoney();
            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.Prepayments.Delete.Confirm.Question", "Delete prepayment confirmation question", "Do you really wish to delete prepayment {0}?", prepayment.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonDetailPrepaymentViewModel
    {
        public string Id;
        public string Enabled;
        public string ShowId;
        public List<PersonDetailPrepaymentItemViewModel> List;
        public string PhraseHeaderReason;
        public string PhraseHeaderMoment;
        public string PhraseHeaderAmount;
        public string PhraseHeaderBalance;
        public string PhraseHeaderType;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailPrepaymentViewModel(Translator translator, IDatabase database, Session session, Person person, string showId)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailPrepaymentItemViewModel>();
            var balance = 0m;

            foreach (var prepayment in database
                .Query<Prepayment>(DC.Equal("personid", person.Id.Value))
                .OrderBy(p => p.Moment.Value))
            {
                balance += prepayment.Amount.Value;
                var view = new PersonDetailPrepaymentItemViewModel(translator, prepayment, balance);
                List.Add(view);
            }

            List.Reverse();
            Enabled =
                session.HasAccess(person, PartAccess.Billing, AccessRight.Write) ?
                "fa-enabled" : "fa-disabled";
            PhraseHeaderReason = translator.Get("Person.Detail.Prepayment.Header.Reason", "Column 'Reason' on the prepayment tab of the person detail page", "Reason").EscapeHtml();
            PhraseHeaderMoment = translator.Get("Person.Detail.Prepayment.Header.Moment", "Column 'Moment' on the prepayment tab of the person detail page", "Date").EscapeHtml();
            PhraseHeaderAmount = translator.Get("Person.Detail.Prepayment.Header.Amount", "Column 'Amount' on the prepayment tab of the person detail page", "Amount").EscapeHtml();
            PhraseHeaderBalance = translator.Get("Person.Detail.Prepayment.Header.Balance", "Column 'Balance' on the prepayment tab of the person detail page", "Balance").EscapeHtml();
            PhraseHeaderType = translator.Get("Person.Detail.Prepayment.Header.Type", "Column 'Type' on the prepayment tab of the person detail page", "Type").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.Prepayment.Delete.Confirm.Title", "Delete prepayment confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
            ShowId = showId;
        }
    }

    public class PersonDetailPrepaymentModule : QuaesturModule
    {
        public PersonDetailPrepaymentModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/prepayment/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);
                var showId = Request.Query.row ?? string.Empty;

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Billing, AccessRight.Read))
                    {
                        return View["View/persondetail_prepayment.sshtml", 
                            new PersonDetailPrepaymentViewModel(Translator, Database, CurrentSession, person, showId)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

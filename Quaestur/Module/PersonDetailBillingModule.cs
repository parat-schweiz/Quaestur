using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class PersonDetailBillItemViewModel
    {
        public string Id;
        public string Number;
        public string FromDate;
        public string UntilDate;
        public string Amount;
        public string Status;
        public string CreatedDate;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailBillItemViewModel(Translator translator, Bill bill)
        {
            Id = bill.Id.Value.ToString();
            Number = bill.Number.Value.EscapeHtml();
            FromDate = bill.FromDate.Value.FormatSwissDateDay();
            UntilDate = bill.UntilDate.Value.FormatSwissDateDay();
            Amount = bill.Amount.Value.FormatMoney();
            Status = bill.Status.Value.Translate(translator).EscapeHtml();
            CreatedDate = bill.CreatedDate.Value.FormatSwissDateDay();
            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.Bills.Delete.Confirm.Question", "Delete bill confirmation question", "Do you really wish to delete bill {0}?", bill.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonDetailBillingViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailBillItemViewModel> List;
        public string PhraseHeaderNumber;
        public string PhraseHeaderFromDate;
        public string PhraseHeaderUntilDate;
        public string PhraseHeaderAmount;
        public string PhraseHeaderStatus;
        public string PhraseHeaderCreatedDate;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailBillingViewModel(Translator translator, IDatabase database, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailBillItemViewModel>(person.Memberships
                .SelectMany(m => database.Query<Bill>(DC.Equal("membershipid", m.Id.Value)))
                .OrderBy(d => d.CreatedDate.Value)
                .Select(d => new PersonDetailBillItemViewModel(translator, d)));
            Editable =
                session.HasAccess(person, PartAccess.Billing, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderNumber = translator.Get("Person.Detail.Bill.Header.Number", "Column 'Number' on the bill tab of the person detail page", "Number").EscapeHtml();
            PhraseHeaderFromDate = translator.Get("Person.Detail.Bill.Header.FromDate", "Column 'From' on the bill tab of the person detail page", "From").EscapeHtml();
            PhraseHeaderUntilDate = translator.Get("Person.Detail.Bill.Header.UntilDate", "Column 'Until' on the bill tab of the person detail page", "Until").EscapeHtml();
            PhraseHeaderAmount = translator.Get("Person.Detail.Bill.Header.Amount", "Column 'Amount' on the bill tab of the person detail page", "Amount").EscapeHtml();
            PhraseHeaderStatus = translator.Get("Person.Detail.Bill.Header.Status", "Column 'Status' on the bill tab of the person detail page", "Status").EscapeHtml();
            PhraseHeaderCreatedDate = translator.Get("Person.Detail.Bill.Header.CreatedDate", "Column 'CreatedDate' on the bill tab of the person detail page", "Created").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.Bill.Delete.Confirm.Title", "Delete bill confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class PersonDetailBillingModule : QuaesturModule
    {
        public PersonDetailBillingModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/billing/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Billing, AccessRight.Read))
                    {
                        return View["View/persondetail_billing.sshtml", 
                            new PersonDetailBillingViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

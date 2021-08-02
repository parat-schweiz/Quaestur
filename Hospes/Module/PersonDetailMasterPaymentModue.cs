using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;
using BaseLibrary;

namespace Hospes
{
    public class PersonDetailPaymentItemViewModel
    {
        public string Id;
        public string Phrase;
        public string Text;
        public string Url;

        public PersonDetailPaymentItemViewModel(string id, string phrase, string text, string url)
        {
            Id = id;
            Phrase = phrase.EscapeHtml();
            Text = text.EscapeHtml();
            Url = url;
        }
    }

    public class PersonDetailPaymentViewModel
    {
        public string Title;
        public string Id;
        public string Editable;
        public List<PersonDetailPaymentItemViewModel> List;

        public PersonDetailPaymentViewModel(Translator translator, IDatabase database, Session session, Person person)
        {
            Title = translator.Get("Person.Detail.Payment.Title", "Title of the payment part of the person detail page", "Payment").EscapeHtml();
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailPaymentItemViewModel>();
            var parameterTypes = person.ActiveMemberships
                .Where(m => m.Type.Value.Payment.Value != PaymentModel.None)
                .SelectMany(m => m.Type.Value.CreatePaymentModel(database).PersonalParameterTypes);

            var usesFederalTax = parameterTypes.Any(p => p.Key == PaymentModelFederalTax.FullTaxKey);
            if (usesFederalTax)
            {
                var fullTax = PaymentModelFederalTax.GetFullTax(person);
                List.Add(new PersonDetailPaymentItemViewModel(
                    "FederalTax",
                    translator.Get("Person.Detail.Payment.FederalTax", "Federal tax item in payment part of the person detail page", "Federal tax"),
                    fullTax != null ? fullTax.Value.FormatMoney() : "-",
                    "/income/" + person.Id.ToString()));
            }

            Editable =
                session.HasAccess(person, PartAccess.Billing, AccessRight.Write) ?
                "editable" : "accessdenied";
        }
    }

    public class PersonDetailPaymentModule : QuaesturModule
    {
        public PersonDetailPaymentModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/master/payment/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Billing, AccessRight.Read))
                    {
                        return View["View/persondetail_master_payment.sshtml", 
                            new PersonDetailPaymentViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

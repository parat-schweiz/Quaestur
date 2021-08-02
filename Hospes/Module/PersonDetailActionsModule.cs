using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Hospes
{
    public class PersonDetailActionsView
    {
        public List<NamedIdViewModel> Memberships;

        public string PhraseFieldMembership;
        public string PhraseButtonSendParameterUpdate;
        public string PhraseButtonCreatePointTally;
        public string PhraseButtonCreateBill;
        public string PhraseButtonSendSettlementOrReminder;

        public PersonDetailActionsView(Translator translator, Person person)
        {
            PhraseFieldMembership = translator.Get("Person.Detail.Master.Actions.Field.Membership", "Membership field on actions tab in person detail page", "Membership");
            PhraseButtonSendParameterUpdate = translator.Get("Person.Detail.Master.Actions.Button.SendParameterUpdate", "Button to Send parameter update on actions tab in person detail page", "Send parameter update");
            PhraseButtonCreatePointTally = translator.Get("Person.Detail.Master.Actions.Button.CreatePointTally", "Button to create point tally on actions tab in person detail page", "Create point tally");
            PhraseButtonCreateBill = translator.Get("Person.Detail.Master.Actions.Button.CreateBill", "Button to create bill on actions tab in person detail page", "Create bill");
            PhraseButtonSendSettlementOrReminder = translator.Get("Person.Detail.Master.Actions.Button.SendSettlementOrReminder", "Button to send settlement or reminder on actions tab in person detail page", "Send settlement or reminder");
            Memberships = new List<NamedIdViewModel>(
                person.Memberships.Select(m => new NamedIdViewModel(translator, m, false)));
        }
    }

    public class PersonDetailActionsModule : QuaesturModule
    {
        private bool HasAccess(Membership membership)
        {
            return HasAccess(membership.Person.Value, PartAccess.Billing, AccessRight.Write) &&
                   HasAccess(membership.Organization.Value, PartAccess.Billing, AccessRight.Write);
        }

        private bool HasAccess(Person person)
        {
            return HasAccess(person, PartAccess.Billing, AccessRight.Extended);
        }

        public PersonDetailActionsModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/actions/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null &&
                    HasAccess(person))
                {
                    return View["View/persondetail_actions.sshtml", new PersonDetailActionsView(Translator, person)];
                }

                return string.Empty;
            });
            Get("/person/detail/actions/sendparameterupdate/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);

                if (membership != null &&
                    HasAccess(membership))
                {
                    PaymentParameterUpdateReminderTask.Send(Database, membership.Person.Value, true);
                }

                return string.Empty;
            });
            Get("/person/detail/actions/createpointtally/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);

                if (membership != null &&
                    HasAccess(membership))
                {
                    PointsTallyTask.CreatePointsTallyAndSend(Database, Translation, membership);
                }

                return string.Empty;
            });
            Get("/person/detail/actions/createbill/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);

                if (membership != null &&
                    HasAccess(membership))
                {
                    BillingTask.CreateBill(Database, Translation, membership);
                }

                return string.Empty;
            });
            Get("/person/detail/actions/sendsettlementorreminder/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);

                if (membership != null &&
                    HasAccess(membership))
                {
                    BillingReminderTask.RemindOrSettle(Database, Translation, membership, true);
                }

                return string.Empty;
            });
        }
    }
}

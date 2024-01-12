using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDetailActionsView
    {
        public List<NamedIdViewModel> Memberships;

        public string PhraseFieldMembership;
        public string PhraseButtonSendParameterUpdate;
        public string PhraseButtonCreatePointTally;
        public string PhraseButtonCreateBill;
        public string PhraseButtonSendSettlementOrReminder;
        public string PhraseButtonCreateBallotPaper;
        public string PhraseDownloadWait;

        public PersonDetailActionsView(Translator translator, Person person)
        {
            PhraseFieldMembership = translator.Get("Person.Detail.Master.Actions.Field.Membership", "Membership field on actions tab in person detail page", "Membership");
            PhraseButtonSendParameterUpdate = translator.Get("Person.Detail.Master.Actions.Button.SendParameterUpdate", "Button to Send parameter update on actions tab in person detail page", "Send parameter update");
            PhraseButtonCreatePointTally = translator.Get("Person.Detail.Master.Actions.Button.CreatePointTally", "Button to create point tally on actions tab in person detail page", "Create point tally");
            PhraseButtonCreateBill = translator.Get("Person.Detail.Master.Actions.Button.CreateBill", "Button to create bill on actions tab in person detail page", "Create bill");
            PhraseButtonSendSettlementOrReminder = translator.Get("Person.Detail.Master.Actions.Button.SendSettlementOrReminder", "Button to send settlement or reminder on actions tab in person detail page", "Send settlement or reminder");
            PhraseButtonCreateBallotPaper = translator.Get("Person.Detail.Master.Actions.Button.CreateBallotPaper", "Button to create ballot paper on actions tab in person detail page", "Create Ballot Paper");
            PhraseDownloadWait = translator.Get("Person.Detail.Master.Actions.Wait.Download", "Wait for download message on actions tab in person detail page", "Downloading...");
            Memberships = new List<NamedIdViewModel>(
                person.Memberships.Select(m => new NamedIdViewModel(translator, m, false)));
        }
    }

    public class PersonDetailActionsModule : QuaesturModule
    {
        private bool HasAccessBilling(Membership membership)
        {
            return HasAccess(membership.Person.Value, PartAccess.Billing, AccessRight.Write) &&
                   HasAccess(membership.Organization.Value, PartAccess.Billing, AccessRight.Write);
        }

        private bool HasAccessBallot(Membership membership)
        {
            return HasAccess(membership.Person.Value, PartAccess.Ballot, AccessRight.Write) &&
                   HasAccess(membership.Organization.Value, PartAccess.Ballot, AccessRight.Write);
        }

        private bool HasAccess(Person person)
        {
            return HasAccess(person, PartAccess.Billing, AccessRight.Extended);
        }

        private bool BallotPaperCreateAllowed(BallotPaper ballotPaper)
        { 
            switch (ballotPaper.Status.Value)
            {
                case BallotPaperStatus.Canceled:
                case BallotPaperStatus.Voted:
                    return false;
            }

            switch (ballotPaper.Ballot.Value.Status.Value)
            {
                case BallotStatus.Canceled:
                case BallotStatus.Finished:
                    return false;
            }

            return true;
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
                    HasAccessBilling(membership))
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
                    HasAccessBilling(membership))
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
                    HasAccessBilling(membership))
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
                    HasAccessBilling(membership))
                {
                    BillingReminderTask.RemindOrSettle(Database, Translation, membership, true);
                }

                return string.Empty;
            });
            Get("/person/detail/actions/createballotpaper/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);
                var status = CreateStatus();

                if (membership != null &&
                    HasAccessBallot(membership))
                {
                    var ballotPaper = Database.Query<BallotPaper>(DC.Equal("memberid", membership.Id.Value))
                        .Where(BallotPaperCreateAllowed)
                        .OrderByDescending(b => b.Ballot.Value.EndDate.Value)
                        .FirstOrDefault();
                    if (ballotPaper != null)
                    {
                        var document = new BallotPaperDocument(Translator, Database, ballotPaper);
                        var pdf = document.Compile();

                        if (pdf != null)
                        {
                            var filename = Translate(
                                "BallotPaper.Download.FileName",
                                "Filename when ballot paper is downloaded",
                                "Ballotpaper.pdf");

                            status.SetDataSuccess(Convert.ToBase64String(pdf), filename);
                            Journal(
                                CurrentSession.User,
                                "BallotPaper.Journal.Download.Success",
                                "Journal entry when downloaded ballot paper",
                                "Downloaded ballot paper for {0}",
                                t => ballotPaper.Ballot.Value.GetText(t));
                        }
                        else
                        {
                            status.SetError(
                                "BallotPaper.Download.Status.Error.Compile",
                                "Status message when downloading ballot paper fails due to document creation error",
                                "Could not ballot paper fails due to document creation error");
                            Journal(
                                CurrentSession.User,
                                "BallotPaper.Journal.Download.Error.Compile",
                                "Journal entry when failed to download ballot paper due to document creation error",
                                "Could not download ballot paper for {0} due to document creation error",
                                t => ballotPaper.Ballot.Value.GetText(t));
                            Warning("Compile error in ballot paper document\n{0}", document.ErrorText);
                        }
                    }
                    else
                    {
                        status.SetError(
                            "BallotPaper.Download.Status.Error.NotFound",
                            "Status message when downloading ballot paper fails because not current ballot paper was found.",
                            "Could not create ballot papier because no currently valid ballot was found.");
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public string PhraseButtonDownloadSettlement;
        public string PhraseButtonCreateBallotPaper;
        public string PhraseButtonDownloadBillData;
        public string PhraseDownloadWait;

        public PersonDetailActionsView(Translator translator, Person person)
        {
            PhraseFieldMembership = translator.Get("Person.Detail.Master.Actions.Field.Membership", "Membership field on actions tab in person detail page", "Membership");
            PhraseButtonSendParameterUpdate = translator.Get("Person.Detail.Master.Actions.Button.SendParameterUpdate", "Button to Send parameter update on actions tab in person detail page", "Send parameter update");
            PhraseButtonCreatePointTally = translator.Get("Person.Detail.Master.Actions.Button.CreatePointTally", "Button to create point tally on actions tab in person detail page", "Create point tally");
            PhraseButtonCreateBill = translator.Get("Person.Detail.Master.Actions.Button.CreateBill", "Button to create bill on actions tab in person detail page", "Create bill");
            PhraseButtonSendSettlementOrReminder = translator.Get("Person.Detail.Master.Actions.Button.SendSettlementOrReminder", "Button to send settlement or reminder on actions tab in person detail page", "Send settlement or reminder");
            PhraseButtonDownloadSettlement = translator.Get("Person.Detail.Master.Actions.Button.DownloadSettlement", "Button to download settlement on actions tab in person detail page", "Download settlement");
            PhraseButtonCreateBallotPaper = translator.Get("Person.Detail.Master.Actions.Button.CreateBallotPaper", "Button to create ballot paper on actions tab in person detail page", "Create Ballot Paper");
            PhraseButtonDownloadBillData = translator.Get("Person.Detail.Master.Actions.Button.DownloadBillData", "Button to download bill data on actions tab in person detail page", "Download Bill Data");
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
            Get("/person/detail/actions/createsettlement/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);
                var status = CreateStatus();

                if (membership != null &&
                    HasAccessBilling(membership))
                {
                    var pdf = BillingReminderTask.CreateSettlementDocument(Database, Translation, membership);
                    status.SetDataSuccess(Convert.ToBase64String(pdf.Item1), pdf.Item2);
                }
                else
                {
                    status.SetError(
                        "Settlement.Download.Status.Error.NotFound",
                        "Status message when downloading settlement fails because not current ballot paper was found.",
                        "Could not create settlement because no valid membership was found.");
                }

                return status.CreateJsonData();
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
            Get("/person/detail/actions/assignmembernumber/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);

                if ((membership != null) &&
                    HasAccess(membership.Person.Value, PartAccess.Demography, AccessRight.Write) &&
                    (membership.Person.Value.Number.Value < 1))
                {
                    using (var transaction = Database.BeginTransaction())
                    {
                        var highNumber = Database.Query<Person>().MaxOrDefault(p => p.Number.Value, 1);
                        var person = membership.Person.Value;
                        person.Number.Value = highNumber + 1;
                        Database.Save(person);
                        Journal(
                            person,
                            "Person.Journal.MemberNumber.Assigned",
                            "Journal entry when member number assigned",
                            "Assigned member number {0}",
                            t => person.Number.Value.ToString());
                        transaction.Commit();
                    }
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
                                membership.Person.Value,
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
                                membership.Person.Value,
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
            Get("/person/detail/actions/downloadbilldata/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);
                var status = CreateStatus();

                if (membership != null &&
                    HasAccessBilling(membership))
                {
                    var csv = new StringBuilder();
                    foreach (var bill in Database
                        .Query<Bill>(DC.Equal("membershipid", membership.Id.Value))
                        .Where(x => (x.CreatedDate.Value.Year == DateTime.UtcNow.Year) || (x.CreatedDate.Value.Year == (DateTime.UtcNow.Year - 1))) 
                        .OrderBy(x => x.CreatedDate.Value))
                    {
                        var fields = new List<string>();
                        fields.Add(bill.Membership.Value.Organization.Value.Name.Value.AnyValue);
                        fields.Add(bill.Membership.Value.Person.Value.Number.Value.ToString());
                        fields.Add(bill.Number.Value);
                        fields.Add(bill.FromDate.Value.ToShortDateString());
                        fields.Add(bill.UntilDate.Value.ToShortDateString());
                        fields.Add(bill.CreatedDate.Value.ToShortDateString());
                        fields.Add(string.Format("Beitrag {0} von {1} bis {2}",
                            bill.Number.Value,
                            bill.FromDate.Value.ToShortDateString(),
                            bill.UntilDate.Value.ToShortDateString()));
                        fields.Add(bill.Amount.Value.ToString());
                        fields.Add(bill.Status.ToString());
                        fields.Add(bill.PayedDate.Value?.ToString() ?? string.Empty);
                        csv.AppendLine(string.Join(";", fields.Select(y => "\"" + y + "\"")));
                    }
                    var b = new Bill();
                    var csvBytes = Encoding.UTF8.GetBytes(csv.ToString());
                    status.SetDataSuccess(Convert.ToBase64String(csvBytes), membership.Person.Value.Number.Value + ".csv");
                    Journal(
                        membership.Person.Value,
                        "BillData.Journal.Download.Success",
                        "Journal entry when downloaded bill data",
                        "Downloaded bill data");
                }

                return status.CreateJsonData();
            });
        }
    }
}

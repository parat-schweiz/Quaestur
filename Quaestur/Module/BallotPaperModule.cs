using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Nancy.Responses;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class BallotPaperListItemViewModel
    {
        public string Id;
        public string Organization;
        public string AnnouncementDate;
        public string StartDate;
        public string EndDate;
        public string Status;
        public string BallotPaperText;

        public BallotPaperListItemViewModel(IDatabase database, Translator translator, Session session, Ballot ballot)
        {
            Id = ballot.Id.Value.ToString();
            Organization = ballot.Template.Value.Organizer.Value.Organization.Value.GetText(translator);
            AnnouncementDate = ballot.EndDate.Value.AddDays(1 - ballot.Template.Value.VotingDays.Value - ballot.Template.Value.PreparationDays.Value).ToString("dd.MM.yyyy");
            StartDate = ballot.EndDate.Value.AddDays(1 - ballot.Template.Value.VotingDays.Value).ToString("dd.MM.yyyy");
            EndDate = ballot.EndDate.Value.ToString("dd.MM.yyyy");
            Status = ballot.Status.Value.Translate(translator);

            if (ballot.Status.Value == BallotStatus.Voting)
            {
                var membership = session.User.Memberships
                    .Single(m => m.Organization.Value == ballot.Template.Value.Organizer.Value.Organization.Value);
                var ballotPaper = database.Query<BallotPaper>(
                    DC.Equal("memberid", membership.Id.Value)
                    .And(DC.Equal("ballotid", ballot.Id.Value)))
                    .FirstOrDefault();

                if (membership.HasVotingRight.Value.Value)
                {
                    BallotPaperText = Html.LinkScript(
                        translator.Get("BallotPaper.List.Download", "Link text to download ballot paper", "Dowload"),
                        string.Format("downloadUrlWait('/ballotpaper/download/{0}');", ballotPaper.Id.Value.ToString()));
                }
                else
                {
                    BallotPaperText =
                        translator.Get("BallotPaper.List.NoVotingRight", "When no voting right to download ballot paper", "No voting right");
                }
            }
            else
            {
                BallotPaperText = string.Empty;
            }
        }
    }

    public class BallotPaperViewModel : MasterViewModel
    {
        public string PhraseHeaderOrganization;
        public string PhraseHeaderAnnouncementDate;
        public string PhraseHeaderStartDate;
        public string PhraseHeaderEndDate;
        public string PhraseHeaderStatus;
        public string PhraseDownloadWait;
        public List<BallotPaperListItemViewModel> List;

        public BallotPaperViewModel(Translator translator, IDatabase database, Session session)
            : base(translator,
            translator.Get("BallotPaper.List.Title", "Title of the ballot paper page", "Ballots"),
            session)
        {
            PhraseHeaderOrganization = translator.Get("BallotPaper.List.Header.Organization", "Header part 'Organization' in the ballot list", "Organization").EscapeHtml();
            PhraseHeaderAnnouncementDate = translator.Get("BallotPaper.List.Header.AnnouncementDate", "Header part 'AnnouncementDate' in the ballot list", "Announcement date").EscapeHtml();
            PhraseHeaderStartDate = translator.Get("BallotPaper.List.Header.StartDate", "Header part 'StartDate' in the ballot list", "Start date").EscapeHtml();
            PhraseHeaderEndDate = translator.Get("BallotPaper.List.Header.EndDate", "Link 'EndDate' caption in the ballot list", "End date").EscapeHtml();
            PhraseHeaderStatus = translator.Get("BallotPaper.List.Header.Status", "Link 'Status' caption in the ballot list", "Status").EscapeHtml();
            PhraseDownloadWait = translator.Get("BallotPaper.List.Download.Wait", "Message while waiting for download", "Creating document...").EscapeHtml();
            session.ReloadUser(database);
            session.User.UpdateAllVotingRights(database);
            List = new List<BallotPaperListItemViewModel>(database
                .Query<Ballot>()
                .Where(bt => session.User.Memberships.Any(m => bt.Template.Value.Organizer.Value.Organization.Value == m.Organization.Value))
                .Select(bt => new BallotPaperListItemViewModel(database, translator, session, bt))
                .OrderBy(bt => bt.EndDate));
        }
    }

    public class BallotPaperVerifyItemViewModel
    {
        public string Label;
        public string Value;

        public BallotPaperVerifyItemViewModel(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }

    public class BallotPaperVerifyViewModel : MasterViewModel
    {
        public List<BallotPaperVerifyItemViewModel> List;

        public BallotPaperVerifyViewModel(Translator translator, IDatabase database, Session session, BallotPaper ballotPaper, byte[] code, bool marked)
            : base(translator,
            translator.Get("BallotPaper.Verify.Title", "Title of the ballot paper verify page", "Ballot paper verification"),
            session)
        {
            List = new List<BallotPaperVerifyItemViewModel>();
            List.Add(new BallotPaperVerifyItemViewModel(
                translator.Get("BallotPaper.Verify.Organization", "Organization on the ballot paper verify page", "Organization"),
                ballotPaper.Ballot.Value.Template.Value.Organizer.Value.Organization.Value.GetText(translator)));
            List.Add(new BallotPaperVerifyItemViewModel(
                translator.Get("BallotPaper.Verify.Ballot.Name", "Ballot name on the ballot paper verify page", "Ballot"),
                ballotPaper.Ballot.Value.GetText(translator)));
            List.Add(new BallotPaperVerifyItemViewModel(
                translator.Get("BallotPaper.Verify.Ballot.Status", "Ballot status on the ballot paper verify page", "Ballot"),
                ballotPaper.Ballot.Value.Status.Value.Translate(translator)));
            List.Add(new BallotPaperVerifyItemViewModel(
                translator.Get("BallotPaper.Verify.Person.Number", "Person number on the ballot paper verify page", "Member No"),
                ballotPaper.Member.Value.Person.Value.Number.ToString()));
            List.Add(new BallotPaperVerifyItemViewModel(
                translator.Get("BallotPaper.Verify.Person.Name", "Person name on the ballot paper verify page", "Name"),
                ballotPaper.Member.Value.Person.Value.ShortHand));

            if (ballotPaper.ComputeCode().AreEqual(code))
            {
                List.Add(new BallotPaperVerifyItemViewModel(
                    translator.Get("BallotPaper.Verify.Verification.Good", "Good verifcation on the ballot paper verify page", "Correct verification"),
                    code.ToHexStringGroupFour()));
            }
            else
            {
                List.Add(new BallotPaperVerifyItemViewModel(
                    translator.Get("BallotPaper.Verify.Verification.Bad", "Good verifcation on the ballot paper verify page", "Wrong verification"),
                    code.ToHexStringGroupFour()));
            }

            switch (ballotPaper.Status.Value)
            {
                case BallotPaperStatus.NoRight:
                    List.Add(new BallotPaperVerifyItemViewModel(
                        translator.Get("BallotPaper.Verify.BallotPaper.Status", "Ballotpaper status on the ballot paper verify page", "Status"),
                        translator.Get("BallotPaper.Verify.BallotPaper.Status.NoRight", "Ballotpaper status no right on the ballot paper verify page", "This person had not voting right at the end date of this voting.")));
                    break;
                case BallotPaperStatus.RightVerified:
                    List.Add(new BallotPaperVerifyItemViewModel(
                        translator.Get("BallotPaper.Verify.BallotPaper.Status", "Ballotpaper status on the ballot paper verify page", "Status"),
                        translator.Get("BallotPaper.Verify.BallotPaper.Status.RightVerified", "Ballotpaper status right verified on the ballot paper verify page", "This ballot paper is fully valid and must be counted.")));
                    break;
                case BallotPaperStatus.Voted:
                    List.Add(new BallotPaperVerifyItemViewModel(
                        translator.Get("BallotPaper.Verify.BallotPaper.Status", "Ballotpaper status on the ballot paper verify page", "Status"),
                        translator.Get("BallotPaper.Verify.BallotPaper.Status.Voted", "Ballotpaper status voted on the ballot paper verify page", "This ballot paper was already counted!")));
                    break;
                case BallotPaperStatus.Canceled:
                    List.Add(new BallotPaperVerifyItemViewModel(
                        translator.Get("BallotPaper.Verify.BallotPaper.Status", "Ballotpaper status on the ballot paper verify page", "Status"),
                        translator.Get("BallotPaper.Verify.BallotPaper.Status.Canceled", "Ballotpaper status canceled on the ballot paper verify page", "This ballot paper is no longer valid.")));
                    break;
                default:
                    List.Add(new BallotPaperVerifyItemViewModel(
                        translator.Get("BallotPaper.Verify.BallotPaper.Status", "Ballotpaper status on the ballot paper verify page", "Status"),
                        translator.Get("BallotPaper.Verify.BallotPaper.Status.Default", "Ballotpaper status default on the ballot paper verify page", "This ballot cannot be counted yet.")));
                    break;
            }

            if (marked)
            {
                List.Add(new BallotPaperVerifyItemViewModel(
                    translator.Get("BallotPaper.Verify.BallotPaper.Action", "Ballotpaper action on the ballot paper verify page", "Action"),
                    translator.Get("BallotPaper.Verify.BallotPaper.Marked", "Ballotpaper marked on the ballot paper verify page", "This ballot paper is now marked counted.")));
            }
        }
    }

    public class BallotPaperModule : QuaesturModule
    {
        public BallotPaperModule()
        {
            RequireCompleteLogin();

            Get["/ballotpaper"] = parameters =>
            {
                return View["View/ballotpaper.sshtml",
                    new BallotPaperViewModel(Translator, Database, CurrentSession)];
            };
            Get["/ballotpaper/download/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballotPaper = Database.Query<BallotPaper>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(ballotPaper))
                {
                    if (ballotPaper.Member.Value.Person.Value == CurrentSession.User)
                    {
                        if (ballotPaper.Member.Value.HasVotingRight.Value.Value)
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
                                "BallotPaper.Download.Status.Error.NoVotingRight",
                                "Status message when downloading ballot paper fails due to lack of voting right",
                                "Could not ballot paper fails due to lack of voting right");
                            Journal(
                                CurrentSession.User,
                                "BallotPaper.Journal.Download.Error.NoVotingRight",
                                "Journal entry when failed to download ballot paper due to lack of voting right",
                                "Could not download ballot paper for {0} due to lack of voting right",
                                t => ballotPaper.Ballot.Value.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/ballotpaper/verify/{id}/{code}"] = parameters =>
            {
                string idString = parameters.id;
                string codeString = parameters.code;
                var ballotPaper = Database.Query<BallotPaper>(idString);
                var code = codeString.TryParseHexBytes();

                if (ballotPaper != null &&
                    code != null)
                {
                    if (HasAccess(ballotPaper.Ballot.Value.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Read) ||
                        ballotPaper.Member.Value.Person.Value == CurrentSession.User)
                    {
                        bool marked = false;

                        if (ballotPaper.Status.Value == BallotPaperStatus.RightVerified &&
                            ballotPaper.ComputeCode().AreEqual(code) &&
                            HasAccess(ballotPaper.Ballot.Value.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                ballotPaper.Status.Value = BallotPaperStatus.Voted;
                                Database.Save(ballotPaper);

                                Journal(ballotPaper.Member.Value.Person.Value,
                                    "BallotPaper.Verify.Marked",
                                    "Ballot paper marked as counted",
                                    "Ballot paper for {0} marked as counted",
                                    t => ballotPaper.Ballot.Value.GetText(t));

                                transaction.Commit();
                            }

                            marked = true;
                        }

                        return View["View/ballotpaperverify.sshtml",
                            new BallotPaperVerifyViewModel(Translator, Database, CurrentSession, ballotPaper, code, marked)];
                    }
                    else
                    {
                        return AccessDenied(); 
                    }
                }
                else
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("BallotPaper.Verify.Error.Title", "Error title in ballot paper verfiy", "Error"),
                        Translate("BallotPaper.Verify.Error.Text", "Back link on ballot paper verfiy error", "Invalid ballot paper verification link."),
                        Translate("BallotPaper.Verify.Error.BackLink", "Back link on ballot paper verfiy error", "Back"),
                        "/")];
                }
            };
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

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
        public string PhraseDeleteConfirmationQuestion;

        public BallotPaperListItemViewModel(Translator translator, Session session, Ballot ballot)
        {
            Id = ballot.Id.Value.ToString();
            Organization = ballot.Template.Value.Organizer.Value.Organization.Value.GetText(translator);
            AnnouncementDate = ballot.EndDate.Value.AddDays(1 - ballot.Template.Value.VotingDays.Value - ballot.Template.Value.PreparationDays.Value).ToString("dd.MM.yyyy");
            StartDate = ballot.EndDate.Value.AddDays(1 - ballot.Template.Value.VotingDays.Value).ToString("dd.MM.yyyy");
            EndDate = ballot.EndDate.Value.ToString("dd.MM.yyyy");
            Status = ballot.Status.Value.Translate(translator);
            PhraseDeleteConfirmationQuestion = translator.Get("Ballot.List.Delete.Confirm.Question", "Delete ballot confirmation question", "Do you really wish to delete ballot {0}?", ballot.GetText(translator));
            var membership = session.User.Memberships
                .Single(m => m.Organization.Value == ballot.Template.Value.Organizer.Value.Organization.Value);

            if (ballot.Status.Value == BallotStatus.Voting)
            {
                if (membership.HasVotingRight.Value.Value)
                {
                    BallotPaperText = Html.Link(
                        translator.Get("BallotPaper.List.Download", "Link text to download ballot paper", "Dowload"),
                        "/ballotpaper/download/" + ballot.Id.Value.ToString());
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
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
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
            PhraseDeleteConfirmationTitle = translator.Get("BallotPaper.List.Delete.Confirm.Title", "Delete ballot confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
            session.User.UpdateAllVotingRights(database);
            List = new List<BallotPaperListItemViewModel>(database
                .Query<Ballot>()
                .Where(bt => session.HasAccess(bt.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Read))
                .Select(bt => new BallotPaperListItemViewModel(translator, session, bt))
                .OrderBy(bt => bt.EndDate));
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
        }
    }
}

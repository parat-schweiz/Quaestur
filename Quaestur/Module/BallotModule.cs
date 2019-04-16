
using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class BallotEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public string Template;
        public string EndDate;
        public List<MultiItemViewModel> AnnouncementText;
        public List<MultiItemViewModel> Questions;
        public List<NamedIdViewModel> Templates;
        public string PhraseFieldTemplate;
        public string PhraseFieldEndDate;
        public string PhraseButtonSave;
        public string PhraseButtonCancel;

        public BallotEditViewModel()
        { 
        }

        public BallotEditViewModel(Translator translator, Session session)
            : base(translator,
            translator.Get("Ballot.Edit.Title", "Title of the ballot edit dialog", "Edit ballot"),
            session)
        {
            PhraseFieldTemplate = translator.Get("Ballot.Edit.Field.Template", "Template field in the ballot edit page", "Template").EscapeHtml();
            PhraseFieldEndDate = translator.Get("Ballot.Edit.Field.EndDate", "EndDate field in the ballot edit page", "EndDate").EscapeHtml();
            PhraseButtonSave = translator.Get("Ballot.Edit.Button.Save", "Save button in the ballot edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("Ballot.Edit.Button.Cancel", "Cancel button in the ballot edit page", "Cancel").EscapeHtml();
        }

        public BallotEditViewModel(Translator translator, IDatabase db, Session session)
            : this(translator, session)
        {
            Method = "add";
            Id = "new";
            Template = string.Empty;
            EndDate = DateTime.Now.AddDays(21).Date.ToString("dd.MM.yyyy");
            AnnouncementText = translator.CreateLanguagesMultiItem("Ballot.Edit.Field.AnnouncementText", "Announcement text field in the ballot edit page", "Announcement text ({0})", new MultiLanguageString());
            Questions =  translator.CreateLanguagesMultiItem("Ballot.Edit.Field.Questions", "Questions field in the ballot edit page", "Questions ({0})", new MultiLanguageString());
            Templates = new List<NamedIdViewModel>(db
                .Query<BallotTemplate>()
                .Where(bt => session.HasAccess(bt.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                .Select(bt => new NamedIdViewModel(translator, bt, false)));
        }

        public BallotEditViewModel(Translator translator, IDatabase db, Session session, Ballot ballot)
            : this(translator, session)
        {
            Method = "edit";
            Id = ballot.Id.ToString();
            Template = string.Empty;
            EndDate = ballot.EndDate.Value.ToString("dd.MM.yyyy");
            AnnouncementText = translator.CreateLanguagesMultiItem("Ballot.Edit.Field.AnnouncementText", "Announcement text field in the ballot edit page", "Announcement text ({0})", ballot.AnnouncementText.Value);
            Questions = translator.CreateLanguagesMultiItem("Ballot.Edit.Field.Questions", "Questions field in the ballot edit page", "Questions ({0})", ballot.Questions.Value);
            Templates = new List<NamedIdViewModel>(db
                .Query<BallotTemplate>()
                .Where(bt => session.HasAccess(bt.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                .Select(bt => new NamedIdViewModel(translator, bt, ballot.Template.Value == bt)));
        }
    }

    public class BallotViewModel : MasterViewModel
    {
        public BallotViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("Ballot.List.Title", "Title of the ballot list page", "Ballots"), 
            session)
        {
        }
    }

    public class BallotListItemViewModel
    {
        public string Id;
        public string Organizer;
        public string AnnouncementDate;
        public string StartDate;
        public string EndDate;
        public string Status;
        public string PhraseDeleteConfirmationQuestion;
        public string Editable;

        public BallotListItemViewModel(Translator translator, Session session, Ballot ballot)
        {
            Id = ballot.Id.Value.ToString();
            Organizer = ballot.Template.Value.Organizer.Value.GetText(translator);
            AnnouncementDate = ballot.EndDate.Value.AddDays(1 - ballot.Template.Value.VotingDays.Value - ballot.Template.Value.PreparationDays.Value).ToString("dd.MM.yyyy");
            StartDate = ballot.EndDate.Value.AddDays(1 - ballot.Template.Value.VotingDays.Value).ToString("dd.MM.yyyy");
            EndDate = ballot.EndDate.Value.ToString("dd.MM.yyyy");
            Status = ballot.Status.Value.Translate(translator);
            PhraseDeleteConfirmationQuestion = translator.Get("Ballot.List.Delete.Confirm.Question", "Delete ballot confirmation question", "Do you really wish to delete ballot {0}?", ballot.GetText(translator));
            var writeAccess = session.HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write);
            Editable = (writeAccess && (ballot.Status.Value == BallotStatus.New)) ? "editable" : string.Empty;
        }
    }

    public class BallotListViewModel
    {
        public string PhraseHeaderOrganizer;
        public string PhraseHeaderAnnouncementDate;
        public string PhraseHeaderStartDate;
        public string PhraseHeaderEndDate;
        public string PhraseHeaderStatus;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<BallotListItemViewModel> List;
        public bool AddAccess;

        public BallotListViewModel(Translator translator, IDatabase database, Session session)
        {
            PhraseHeaderOrganizer = translator.Get("Ballot.List.Header.Organizer", "Header part 'Organizer' in the ballot list", "Organizer").EscapeHtml();
            PhraseHeaderAnnouncementDate = translator.Get("Ballot.List.Header.AnnouncementDate", "Header part 'AnnouncementDate' in the ballot list", "Announcement date").EscapeHtml();
            PhraseHeaderStartDate = translator.Get("Ballot.List.Header.StartDate", "Header part 'StartDate' in the ballot list", "Start date").EscapeHtml();
            PhraseHeaderEndDate = translator.Get("Ballot.List.Header.EndDate", "Link 'EndDate' caption in the ballot list", "End date").EscapeHtml();
            PhraseHeaderStatus = translator.Get("Ballot.List.Header.Status", "Link 'Status' caption in the ballot list", "Status").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Ballot.List.Delete.Confirm.Title", "Delete ballot confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
            List = new List<BallotListItemViewModel>(database
                .Query<Ballot>()
                .Where(bt => session.HasAccess(bt.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Read))
                .Select(bt => new BallotListItemViewModel(translator, session, bt))
                .OrderBy(bt => bt.EndDate));
            AddAccess = database.Query<BallotTemplate>()
                .Any(bt => session.HasAccess(bt.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write));
        }
    }

    public class BallotModule : QuaesturModule
    {
        public BallotModule()
        {
            RequireCompleteLogin();

            Get["/ballot"] = parameters =>
            {
                return View["View/ballot.sshtml",
                    new BallotViewModel(Translator, CurrentSession)];
            };
            Get["/ballot/list"] = parameters =>
            {
                return View["View/ballotlist.sshtml",
                    new BallotListViewModel(Translator, Database, CurrentSession)];
            };
            Get["/ballot/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballot = Database.Query<Ballot>(idString);

                if (ballot != null)
                {
                    if (HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        return View["View/ballotedit.sshtml",
                            new BallotEditViewModel(Translator, Database, CurrentSession, ballot)];
                    }
                }

                return null;
            };
            Get["/ballot/copy/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballot = Database.Query<Ballot>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(ballot))
                {
                    if (status.HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        var newBallot = new Ballot(Guid.NewGuid());
                        newBallot.Template.Value = ballot.Template.Value;
                        newBallot.Status.Value = BallotStatus.New;
                        newBallot.AnnouncementText.Value = ballot.AnnouncementText.Value;
                        newBallot.Questions.Value = ballot.Questions.Value;
                        newBallot.EndDate.Value = DateTime.Now.AddDays(21).Date;
                        Database.Save(newBallot);
                    }
                }

                return status.CreateJsonData();
            };
            Post["/ballot/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<BallotEditViewModel>(ReadBody());
                var ballot = Database.Query<Ballot>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(ballot))
                {
                    if (status.HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        if (ballot.Status.Value == BallotStatus.New)
                        {
                            status.AssignObjectIdString("Template", ballot.Template, model.Template);
                            status.AssignDateString("EndDate", ballot.EndDate, model.EndDate);
                            status.AssignMultiLanguageFree("AnnouncementText", ballot.AnnouncementText, model.AnnouncementText);
                            status.AssignMultiLanguageFree("Questions", ballot.Questions, model.Questions);

                            if (status.IsSuccess &&
                                status.HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                            {
                                if (DateTime.Now.Date <= ballot.AnnouncementDate)
                                {
                                    Database.Save(ballot);
                                    Notice("{0} changed ballot {1}", CurrentSession.User.ShortHand, ballot);
                                }
                                else
                                {
                                    status.SetError("Ballot.Edit.DatePassed", "When end date is so that invitation date already passed", "This end date does not allow sufficient time to invite to this ballot.");
                                }
                            }
                        }
                        else
                        {
                            status.SetError("Ballot.Edit.OnlyNew", "When a ballot that is not new is edited", "Only new ballots may be edited.");
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/ballot/add"] = parameters =>
            {
                return View["View/ballotedit.sshtml",
                    new BallotEditViewModel(Translator, Database, CurrentSession)];
            };
            Post["/ballot/add/new"] = parameters =>
            {
                var status = CreateStatus();

                var model = JsonConvert.DeserializeObject<BallotEditViewModel>(ReadBody());
                var ballot = new Ballot(Guid.NewGuid());
                status.AssignObjectIdString("Template", ballot.Template, model.Template);
                status.AssignDateString("EndDate", ballot.EndDate, model.EndDate);
                status.AssignMultiLanguageFree("AnnouncementText", ballot.AnnouncementText, model.AnnouncementText);
                status.AssignMultiLanguageFree("Questions", ballot.Questions, model.Questions);

                if (status.IsSuccess &&
                    status.HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                {
                    if (DateTime.Now.Date <= ballot.AnnouncementDate)
                    {
                        Database.Save(ballot);
                        Notice("{0} added ballot {1}", CurrentSession.User.ShortHand, ballot);
                    }
                    else
                    {
                        status.SetError("Ballot.Edit.DatePassed", "When end date is so that invitation date already passed", "This end date does not allow sufficient time to invite to this ballot.");
                    }
                }

                return status.CreateJsonData();
            };
            Get["/ballot/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballot = Database.Query<Ballot>(idString);

                if (ballot != null)
                {
                    if (HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            ballot.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted ballot template {1}", CurrentSession.User.ShortHand, ballot);
                        }
                    }
                }

                return null;
            };
        }
    }
}

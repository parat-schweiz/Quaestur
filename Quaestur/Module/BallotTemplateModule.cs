using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class BallotTemplateEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Organizer;
        public string ParticipantTag;
        public string PreparationDays;
        public string VotingDays;
        public List<MultiItemViewModel> AnnouncementMailSubject;
        public List<MultiItemViewModel> AnnouncementMailText;
        public List<MultiItemViewModel> AnnouncementLetter;
        public List<MultiItemViewModel> InvitationMailSubject;
        public List<MultiItemViewModel> InvitationMailText;
        public List<MultiItemViewModel> InvitationLetter;
        public List<MultiItemViewModel> VoterCard;
        public List<MultiItemViewModel> BallotPaper;
        public List<NamedIdViewModel> Organizers;
        public List<NamedIdViewModel> ParticipantTags;
        public string PhraseFieldOrganizer;
        public string PhraseFieldParticipantTag;
        public string PhraseFieldPreparationDays;
        public string PhraseFieldVotingDays;
        public string PhraseButtonSave;
        public string PhraseButtonCancel;

        public BallotTemplateEditViewModel()
        { 
        }

        public BallotTemplateEditViewModel(Translator translator, Session session)
            : base(translator,
            translator.Get("BallotTemplate.Edit.Title", "Title of the ballot template edit dialog", "Edit ballot template"),
            session)
        {
            PhraseFieldOrganizer = translator.Get("BallotTemplate.Edit.Field.Organizer", "Organizer field in the ballot emplate edit page", "Organizer").EscapeHtml();
            PhraseFieldParticipantTag = translator.Get("BallotTemplate.Edit.Field.ParticipantTag", "Participant tag field in the ballot template edit page", "Participant tag").EscapeHtml();
            PhraseFieldPreparationDays = translator.Get("BallotTemplate.Edit.Field.PreparationDays", "Preparation days field in the ballot template edit page", "Preparation days").EscapeHtml();
            PhraseFieldVotingDays = translator.Get("BallotTemplate.Edit.Field.VotingDays", "Voting days field in the ballot template edit page", "Voting days").EscapeHtml();
            PhraseButtonSave = translator.Get("BallotTemplate.Edit.Button.Save", "Save button in the ballot template edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("BallotTemplate.Edit.Button.Cancel", "Cancel button in the ballot template edit page", "Cancel").EscapeHtml();
        }

        public BallotTemplateEditViewModel(Translator translator, IDatabase db, Session session)
            : this(translator, session)
        {
            Method = "add";
            Id = string.Empty;
            Name = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.Name", "Name field in the ballot template edit page", "Name", new MultiLanguageString());
            Organizer = string.Empty;
            ParticipantTag = string.Empty;
            PreparationDays = string.Empty;
            VotingDays = string.Empty;
            AnnouncementMailSubject = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.AnnouncementMailSubject", "Announcement mail subject field in the ballot template edit page", "Announcement mail subject", new MultiLanguageString());
            AnnouncementMailText = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.AnnouncementMailText", "Announcement mail text field in the ballot template edit page", "Announcement mail text", new MultiLanguageString());
            AnnouncementLetter = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.AnnouncementLetter", "Announcement letter field in the ballot template edit page", "Announcement letter", new MultiLanguageString());
            InvitationMailSubject = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.InvitationMailSubject", "Invitation mail subject field in the ballot template edit page", "Invitation mail subject", new MultiLanguageString());
            InvitationMailText = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.InvitationMailText", "Invitation mail text field in the ballot template edit page", "Invitation mail text", new MultiLanguageString());
            InvitationLetter = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.InvitationLetter", "Invitation letter field in the ballot template edit page", "Invitation letter", new MultiLanguageString());
            VoterCard = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.VoterCard", "Voter card field in the ballot template edit page", "Voter card", new MultiLanguageString());
            BallotPaper = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.BallotPaper", "Ballot paper field in the ballot template edit page", "Ballot paper", new MultiLanguageString());
            Organizers = new List<NamedIdViewModel>(db
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Ballot, AccessRight.Write))
                .Select(g => new NamedIdViewModel(translator, g, false)));
            ParticipantTags = new List<NamedIdViewModel>(db
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false)));
        }

        public BallotTemplateEditViewModel(Translator translator, IDatabase db, Session session, BallotTemplate ballotTemplate)
            : this(translator, session)
        {
            Method = "edit";
            Id = ballotTemplate.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.Name", "Name field in the ballot template edit page", "Name", ballotTemplate.Name.Value);
            Organizer = string.Empty;
            ParticipantTag = string.Empty;
            PreparationDays = string.Empty;
            VotingDays = string.Empty;
            AnnouncementMailSubject = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.AnnouncementMailSubject", "Announcement mail subject field in the ballot template edit page", "Announcement mail subject", ballotTemplate.AnnouncementMailSubject);
            AnnouncementMailText = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.AnnouncementMailText", "Announcement mail text field in the ballot template edit page", "Announcement mail text", ballotTemplate.AnnouncementMailText);
            AnnouncementLetter = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.AnnouncementLetter", "Announcement letter field in the ballot template edit page", "Announcement letter", ballotTemplate.AnnouncementLetter);
            InvitationMailSubject = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.InvitationMailSubject", "Invitation mail subject field in the ballot template edit page", "Invitation mail subject", ballotTemplate.InvitationMailSubject);
            InvitationMailText = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.InvitationMailText", "Invitation mail text field in the ballot template edit page", "Invitation mail text", ballotTemplate.InvitationMailText);
            InvitationLetter = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.InvitationLetter", "Invitation letter field in the ballot template edit page", "Invitation letter", ballotTemplate.InvitationLetter);
            VoterCard = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.VoterCard", "Voter card field in the ballot template edit page", "Voter card", ballotTemplate.VoterCard);
            BallotPaper = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.BallotPaper", "Ballot paper field in the ballot template edit page", "Ballot paper", ballotTemplate.BallotPaper);
            Organizers = new List<NamedIdViewModel>(db
                .Query<Group>()
                .Where(g => session.HasAccess(g.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                .Select(g => new NamedIdViewModel(translator, g, ballotTemplate.Organizer.Value == g)));
            ParticipantTags = new List<NamedIdViewModel>(db
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, ballotTemplate.ParticipantTag.Value == t)));
        }
    }

    public class BallotTemplateViewModel : MasterViewModel
    {
        public BallotTemplateViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("BallotTemplate.List.Title", "Title of the ballot template list page", "Ballot templates"), 
            session)
        {
        }
    }

    public class BallotTemplateListItemViewModel
    {
        public string Id;
        public string Organizer;
        public string Name;
        public string PhraseDeleteConfirmationQuestion;
        public bool WriteAccess;

        public BallotTemplateListItemViewModel(Translator translator, Session session, BallotTemplate ballotTemplate)
        {
            Id = ballotTemplate.Id.Value.ToString();
            Organizer = ballotTemplate.Organizer.Value.GetText(translator).EscapeHtml();
            Name = ballotTemplate.Name.Value[translator.Language].EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("BallotTemplate.List.Delete.Confirm.Question", "Delete ballot sending template confirmation question", "Do you really wish to delete ballot template {0}?", ballotTemplate.GetText(translator));
            WriteAccess = session.HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write);
        }
    }

    public class BallotTemplateListViewModel
    {
        public string PhraseHeaderOrganizer;
        public string PhraseHeaderName;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<BallotTemplateListItemViewModel> List;
        public bool AddAccess;

        public BallotTemplateListViewModel(Translator translator, IDatabase database, Session session)
        {
            PhraseHeaderOrganizer = translator.Get("BallotTemplate.List.Header.Organizer", "Header part 'Organizer' in the ballot template list", "Language").EscapeHtml();
            PhraseHeaderName = translator.Get("BallotTemplate.List.Header.Name", "Link 'Name' caption in the ballot template list", "Name").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("BallotTemplate.List.Delete.Confirm.Title", "Delete ballot template confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("BallotTemplate.List.Delete.Confirm.Info", "Delete ballot template confirmation info", "This will also delete all ballots using that template.").EscapeHtml();
            List = new List<BallotTemplateListItemViewModel>(database
                .Query<BallotTemplate>()
                .Where(bt => session.HasAccess(bt.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Read))
                .Select(bt => new BallotTemplateListItemViewModel(translator, session, bt))
                .OrderBy(bt => bt.Organizer + bt.Name));
            AddAccess = session.HasAnyOrganizationAccess(PartAccess.Ballot, AccessRight.Write);
        }
    }

    public class BallotTemplateModule : QuaesturModule
    {
        public BallotTemplateModule()
        {
            RequireCompleteLogin();

            Get["/ballottemplate"] = parameters =>
            {
                return View["View/ballottemplate.sshtml",
                    new BallotTemplateViewModel(Translator, CurrentSession)];
            };
            Get["/ballottemplate/list"] = parameters =>
            {
                return View["View/ballottemplatelist.sshtml",
                    new BallotTemplateListViewModel(Translator, Database, CurrentSession)];
            };
            Get["/ballottemplate/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballotTemplate = Database.Query<BallotTemplate>(idString);

                if (ballotTemplate != null)
                {
                    if (HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        return View["View/ballottemplateedit.sshtml",
                            new BallotTemplateEditViewModel(Translator, Database, CurrentSession, ballotTemplate)];
                    }
                }

                return null;
            };
            Get["/ballottemplate/copy/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballotTemplate = Database.Query<BallotTemplate>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(ballotTemplate))
                {
                    if (status.HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        var newTemplate = new BallotTemplate(Guid.NewGuid());
                        newTemplate.Organizer.Value = ballotTemplate.Organizer.Value;
                        newTemplate.ParticipantTag.Value = ballotTemplate.ParticipantTag.Value;
                        newTemplate.Name.Value = ballotTemplate.Name.Value +=
                            Translate("BallotTemplate.Copy.NameSuffix", "Suffix on copyied ballot template", " (Copy)");
                        newTemplate.PreparationDays.Value = ballotTemplate.PreparationDays.Value;
                        newTemplate.VotingDays.Value = ballotTemplate.VotingDays.Value;
                        newTemplate.AnnouncementMailSubject.Value = ballotTemplate.AnnouncementMailSubject.Value;
                        newTemplate.AnnouncementMailText.Value = ballotTemplate.AnnouncementMailText.Value;
                        newTemplate.AnnouncementLetter.Value = ballotTemplate.AnnouncementLetter.Value;
                        newTemplate.InvitationMailSubject.Value = ballotTemplate.InvitationMailSubject.Value;
                        newTemplate.InvitationMailText.Value = ballotTemplate.InvitationMailText.Value;
                        newTemplate.InvitationLetter.Value = ballotTemplate.InvitationLetter.Value;
                        newTemplate.VoterCard.Value = ballotTemplate.VoterCard.Value;
                        newTemplate.BallotPaper.Value = ballotTemplate.BallotPaper.Value;
                        Database.Save(newTemplate);
                    }
                }

                return status.CreateJsonData();
            };
            Post["/ballottemplate/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<BallotTemplateEditViewModel>(ReadBody());
                var ballotTemplate = Database.Query<BallotTemplate>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(ballotTemplate))
                {
                    if (status.HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", ballotTemplate.Name, model.Name);
                        status.AssignObjectIdString("Organizer", ballotTemplate.Organizer, model.Organizer);
                        status.AssignObjectIdString("ParticipantTag", ballotTemplate.ParticipantTag, model.ParticipantTag);
                        status.AssignInt32String("PreparationDays", ballotTemplate.PreparationDays, model.PreparationDays);
                        status.AssignInt32String("VotingDays", ballotTemplate.VotingDays, model.VotingDays);
                        status.AssignMultiLanguageFree("AnnouncementMailSubject", ballotTemplate.AnnouncementMailSubject, model.AnnouncementMailSubject);
                        status.AssignMultiLanguageFree("AnnouncementMailText", ballotTemplate.AnnouncementMailText, model.AnnouncementMailText);
                        status.AssignMultiLanguageFree("AnnouncementLetter", ballotTemplate.AnnouncementLetter, model.AnnouncementLetter);
                        status.AssignMultiLanguageFree("InvitationMailSubject", ballotTemplate.InvitationMailSubject, model.InvitationMailSubject);
                        status.AssignMultiLanguageFree("InvitationMailText", ballotTemplate.InvitationMailText, model.InvitationMailText);
                        status.AssignMultiLanguageFree("InvitationLetter", ballotTemplate.InvitationLetter, model.InvitationLetter);
                        status.AssignMultiLanguageFree("VoterCard", ballotTemplate.VoterCard, model.VoterCard);
                        status.AssignMultiLanguageFree("BallotPaper", ballotTemplate.BallotPaper, model.BallotPaper);

                        if (status.HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                        {
                            if (status.IsSuccess)
                            {
                                Database.Save(ballotTemplate);
                                Notice("{0} changed ballot template {1}", CurrentSession.User.ShortHand, ballotTemplate);
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/ballottemplate/add"] = parameters =>
            {
                return View["View/ballottemplateedit.sshtml",
                    new BallotTemplateEditViewModel(Translator, Database, CurrentSession)];
            };
            Post["/ballottemplate/add"] = parameters =>
            {
                var status = CreateStatus();

                var model = JsonConvert.DeserializeObject<BallotTemplateEditViewModel>(ReadBody());
                var ballotTemplate = new BallotTemplate(Guid.NewGuid());
                status.AssignMultiLanguageRequired("Name", ballotTemplate.Name, model.Name);
                status.AssignObjectIdString("Organizer", ballotTemplate.Organizer, model.Organizer);
                status.AssignObjectIdString("ParticipantTag", ballotTemplate.ParticipantTag, model.ParticipantTag);
                status.AssignInt32String("PreparationDays", ballotTemplate.PreparationDays, model.PreparationDays);
                status.AssignInt32String("VotingDays", ballotTemplate.VotingDays, model.VotingDays);
                status.AssignMultiLanguageFree("AnnouncementMailSubject", ballotTemplate.AnnouncementMailSubject, model.AnnouncementMailSubject);
                status.AssignMultiLanguageFree("AnnouncementMailText", ballotTemplate.AnnouncementMailText, model.AnnouncementMailText);
                status.AssignMultiLanguageFree("AnnouncementLetter", ballotTemplate.AnnouncementLetter, model.AnnouncementLetter);
                status.AssignMultiLanguageFree("InvitationMailSubject", ballotTemplate.InvitationMailSubject, model.InvitationMailSubject);
                status.AssignMultiLanguageFree("InvitationMailText", ballotTemplate.InvitationMailText, model.InvitationMailText);
                status.AssignMultiLanguageFree("InvitationLetter", ballotTemplate.InvitationLetter, model.InvitationLetter);
                status.AssignMultiLanguageFree("VoterCard", ballotTemplate.VoterCard, model.VoterCard);
                status.AssignMultiLanguageFree("BallotPaper", ballotTemplate.BallotPaper, model.BallotPaper);

                if (status.HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                {
                    if (status.IsSuccess)
                    {
                        Database.Save(ballotTemplate);
                        Notice("{0} added ballot template {1}", CurrentSession.User.ShortHand, ballotTemplate);
                    }
                }

                return status.CreateJsonData();
            };
            Get["/ballottemplate/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballotTemplate = Database.Query<BallotTemplate>(idString);

                if (ballotTemplate != null)
                {
                    if (HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            ballotTemplate.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted ballot template {1}", CurrentSession.User.ShortHand, ballotTemplate);
                        }
                    }
                }

                return null;
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class SendingTemplateLanguageEdit
    {
        public string Text { get; private set; }
        public string Link { get; private set; }
        public string ButtonId { get; private set; }

        public SendingTemplateLanguageEdit(Translator translator, SendingTemplate sendingTemplate, Language language, string translationPrefix, string technicalName)
        {
            ButtonId = Guid.NewGuid().ToString();
            var sendingTemplateLanguage = sendingTemplate.Languages.SingleOrDefault(stl => stl.Language.Value == language);

            if (sendingTemplateLanguage == null)
            {
                Text = translator.Get(translationPrefix + ".Add", "Add button on a sending template for " + translationPrefix, "Add {0} " + technicalName, language.Translate(translator));
                Link = string.Format("/sendingtemplatelanguage/add/{0}/{1}", sendingTemplate.Id.Value.ToString(), (int)language);
            }
            else
            {
                Text = translator.Get(translationPrefix + ".Edit", "Edit button on a sending template for " + translationPrefix, "Edit {0} " + technicalName, language.Translate(translator));
                Link = string.Format("/sendingtemplatelanguage/edit/{0}", sendingTemplateLanguage.Id.Value.ToString());
            }
        }
    }

    public class BallotTemplateEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Organizer;
        public string ParticipantTag;
        public string PreparationDays;
        public string VotingDays;
        public List<SendingTemplateLanguageEdit> Announcement;
        public List<SendingTemplateLanguageEdit> Invitation;
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
            Name = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.Name", "Name field in the ballot template edit page", "Name ({0})", new MultiLanguageString());
            Organizer = string.Empty;
            ParticipantTag = string.Empty;
            PreparationDays = string.Empty;
            VotingDays = string.Empty;
            Announcement = new List<SendingTemplateLanguageEdit>();
            Invitation = new List<SendingTemplateLanguageEdit>();
            VoterCard = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.VoterCard", "Voter card field in the ballot template edit page", "Voter card ({0})", new MultiLanguageString());
            BallotPaper = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.BallotPaper", "Ballot paper field in the ballot template edit page", "Ballot paper ({0})", new MultiLanguageString());
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
            Name = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.Name", "Name field in the ballot template edit page", "Name ({0})", ballotTemplate.Name.Value);
            Organizer = string.Empty;
            ParticipantTag = string.Empty;
            PreparationDays = ballotTemplate.PreparationDays.Value.ToString();
            VotingDays = ballotTemplate.VotingDays.Value.ToString();
            Announcement = new List<SendingTemplateLanguageEdit>(LanguageExtensions.Natural
                .Select(l => new SendingTemplateLanguageEdit(translator, ballotTemplate.Announcement.Value, l, "BallotTemplate.Announcement", "Announcement")));
            Invitation = new List<SendingTemplateLanguageEdit>(LanguageExtensions.Natural
                .Select(l => new SendingTemplateLanguageEdit(translator, ballotTemplate.Invitation.Value, l, "BallotTemplate.Invitation", "Invitation")));
            VoterCard = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.VoterCard", "Voter card field in the ballot template edit page", "Voter card ({0})", ballotTemplate.VoterCard);
            BallotPaper = translator.CreateLanguagesMultiItem("BallotTemplate.Edit.Field.BallotPaper", "Ballot paper field in the ballot template edit page", "Ballot paper ({0})", ballotTemplate.BallotPaper);
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
        public string Editable;

        public BallotTemplateListItemViewModel(Translator translator, Session session, BallotTemplate ballotTemplate)
        {
            Id = ballotTemplate.Id.Value.ToString();
            Organizer = ballotTemplate.Organizer.Value.GetText(translator).EscapeHtml();
            Name = ballotTemplate.Name.Value[translator.Language].EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("BallotTemplate.List.Delete.Confirm.Question", "Delete ballot sending template confirmation question", "Do you really wish to delete ballot template {0}?", ballotTemplate.GetText(translator));
            var writeAccess = session.HasAccess(ballotTemplate. Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write);
            Editable = writeAccess ? "editable" : string.Empty;
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
            PhraseHeaderOrganizer = translator.Get("BallotTemplate.List.Header.Organizer", "Header part 'Organizer' in the ballot template list", "Organizer").EscapeHtml();
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
                        newTemplate.VoterCard.Value = ballotTemplate.VoterCard.Value;
                        newTemplate.BallotPaper.Value = ballotTemplate.BallotPaper.Value;

                        using (var transaction = Database.BeginTransaction())
                        {
                            var announcementSendingTemplate = Database.Query<SendingTemplate>(
                                DC.Equal("parentid", ballotTemplate.Id.Value).And(DC.Equal("fieldname", newTemplate.Announcement.ColumnName))).Single();
                            CopyTemplateLanguages(announcementSendingTemplate, AddTemplate(newTemplate, newTemplate.Announcement));

                            var invitationSendingTemplate = Database.Query<SendingTemplate>(
                                DC.Equal("parentid", ballotTemplate.Id.Value).And(DC.Equal("fieldname", newTemplate.Invitation.ColumnName))).Single();
                            CopyTemplateLanguages(invitationSendingTemplate, AddTemplate(newTemplate, newTemplate.Invitation));

                            Database.Save(newTemplate);
                            transaction.Commit();
                        }
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
                status.AssignMultiLanguageFree("VoterCard", ballotTemplate.VoterCard, model.VoterCard);
                status.AssignMultiLanguageFree("BallotPaper", ballotTemplate.BallotPaper, model.BallotPaper);

                using (var transaction = Database.BeginTransaction())
                {
                    AddTemplate(ballotTemplate, ballotTemplate.Announcement);
                    AddTemplate(ballotTemplate, ballotTemplate.Invitation);

                    if (status.IsSuccess &&
                        status.HasAccess(ballotTemplate.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        Database.Save(ballotTemplate);
                        transaction.Commit();
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

        private void CopyTemplateLanguages(SendingTemplate source, SendingTemplate destination)
        { 
            foreach (var sourceLanguage in source.Languages)
            {
                var newLanguage = new SendingTemplateLanguage(Guid.NewGuid());
                newLanguage.Template.Value = destination;
                newLanguage.Language.Value = sourceLanguage.Language.Value;
                newLanguage.MailSubject.Value = sourceLanguage.MailSubject.Value;
                newLanguage.MailHtmlText.Value = sourceLanguage.MailHtmlText.Value;
                newLanguage.MailPlainText.Value = sourceLanguage.MailPlainText.Value;
                newLanguage.LetterLatex.Value = sourceLanguage.LetterLatex.Value;
                Database.Save(newLanguage);
            }
        }

        private SendingTemplate AddTemplate(BallotTemplate ballotTemplate, ForeignKeyField<SendingTemplate, BallotTemplate> field)
        {
            field.Value = new SendingTemplate(Guid.NewGuid());
            field.Value.ParentType.Value = SendingTemplateParentType.BallotTemplate;
            field.Value.ParentId.Value = ballotTemplate.Id.Value;
            field.Value.FieldName.Value = field.ColumnName;
            Database.Save(field.Value);
            return field.Value;
        }
    }
}

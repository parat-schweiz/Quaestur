using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using MimeKit;

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
        public string PhraseButtonTest;

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
            PhraseButtonTest = translator.Get("Ballot.Edit.Button.Test", "Test button in the ballot edit page", "Test").EscapeHtml();
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

                if (ballot != null &&
                    ballot.Status.Value == BallotStatus.New)
                {
                    if (HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Ballot, AccessRight.Write))
                    {
                        return View["View/ballotedit.sshtml",
                            new BallotEditViewModel(Translator, Database, CurrentSession, ballot)];
                    }
                }

                return Response.AsRedirect("/ballot");
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
                        newBallot.Secret.Value = Rng.Get(32);
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
                ballot.Secret.Value = Rng.Get(32);

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
            Get["/ballot/test/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var ballot = Database.Query<Ballot>(idString);
                var result = new PostResult();

                if (ballot != null &&
                    HasAccess(ballot.Template.Value.Organizer.Value.Organization.Value, PartAccess.Structure, AccessRight.Write))
                {
                    var content = new Multipart("mixed");
                    var bodyText = Translate("BallotTemplate.Test.Text", "Text of the test ballot template mail", "See attachements");
                    var bodyPart = new TextPart("plain") { Text = bodyText };
                    bodyPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                    content.Add(bodyPart);

                    var membership = new Membership(Guid.NewGuid());
                    membership.Organization.Value = ballot.Template.Value.Organizer.Value.Organization.Value;
                    membership.Person.Value = CurrentSession.User;
                    membership.StartDate.Value = DateTime.Now.Date.AddDays(-10);

                    var ballotPaper = new BallotPaper(Guid.NewGuid());
                    ballotPaper.Ballot.Value = ballot;
                    ballotPaper.Member.Value = membership;
                    ballotPaper.Status.Value = BallotPaperStatus.Invited;

                    var ballotTemplate = ballot.Template.Value;

                    foreach (var language in new Language[] { Language.English, Language.French, Language.German, Language.Italian })
                    {
                        if (ballotTemplate.Announcement.Value.Languages.Any(stl => stl.Language.Value == language))
                        {
                            var sendingTemplateLanguage = ballotTemplate.Announcement.Value.Languages.Single(stl => stl.Language.Value == language);
                            var message = BallotTask.CreateMail(Database, ballotPaper, sendingTemplateLanguage);
                            Global.MailCounter.Used();
                            Global.Mail.Send(message);
                        }

                        if (ballotTemplate.Invitation.Value.Languages.Any(stl => stl.Language.Value == language))
                        {
                            var sendingTemplateLanguage = ballotTemplate.Invitation.Value.Languages.Single(stl => stl.Language.Value == language);
                            var message = BallotTask.CreateMail(Database, ballotPaper, sendingTemplateLanguage);
                            Global.MailCounter.Used();
                            Global.Mail.Send(message);
                        }

                        var ballotPaperDocument = new BallotPaperDocument(new Translator(Translation, language), Database, ballotPaper);

                        var documentData = ballotPaperDocument.Compile();

                        if (documentData != null)
                        {
                            var documentStream = new MemoryStream(documentData);
                            var documentPart = new MimePart("application", "pdf");
                            documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
                            documentPart.ContentType.Name = language.ToString() + ".ballot.pdf";
                            documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                            documentPart.ContentDisposition.FileName = language.ToString() + ".ballot.pdf";
                            documentPart.ContentTransferEncoding = ContentEncoding.Base64;
                            content.Add(documentPart);
                        }

                        var latexPart = new TextPart("plain") { Text = ballotPaperDocument.TexDocument };
                        latexPart.ContentType.Name = language.ToString() + ".tex";
                        latexPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                        latexPart.ContentDisposition.FileName = language.ToString() + ".tex";
                        latexPart.ContentTransferEncoding = ContentEncoding.Base64;
                        content.Add(latexPart);

                        var errorPart = new TextPart("plain") { Text = ballotPaperDocument.ErrorText };
                        errorPart.ContentType.Name = language.ToString() + ".output.txt";
                        errorPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                        errorPart.ContentDisposition.FileName = language.ToString() + ".output.txt";
                        errorPart.ContentTransferEncoding = ContentEncoding.Base64;
                        content.Add(errorPart);
                    }

                    if (content.Count > 1)
                    {
                        var to = new MailboxAddress(CurrentSession.User.ShortHand, CurrentSession.User.PrimaryMailAddress);
                        var subject = Translate("BallotTemplate.Test.Subject", "Subject of the test ballot template mail", "Test ballot template");
                        Global.MailCounter.Used();
                        Global.Mail.Send(to, subject, content);

                        result.MessageType = "succss";
                        result.MessageText = Translate("BallotTemplate.Test.Success", "Success during test create bill", "Compilation finished. You will recieve the output via mail.");
                        result.IsSuccess = true;
                    }
                    else
                    {
                        result.MessageType = "warning";
                        result.MessageText = Translate("BallotTemplate.Test.Failed.Failed", "LaTeX failed during test create bill", "Compilation failed. No output was generated.");
                        result.IsSuccess = false;
                    }
                }
                else
                {
                    result.MessageType = "warning";
                    result.MessageText = Translate("MembershipType.TestCreateBill.Failed.NotFound", "Object not found during test create bill", "Object not found");
                    result.IsSuccess = false;
                }

                return JsonConvert.SerializeObject(result);
            };
        }
    }
}

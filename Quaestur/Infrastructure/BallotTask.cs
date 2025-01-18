using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class BallotTask : ITask
    {
        private DateTime _lastAction;

        public BallotTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddMinutes(15))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Info("Running ballot task");

                RunAll(database);

                Global.Log.Info("Ballot task complete");
            }
        }

        private void Journal(IDatabase db, BallotPaper ballotPaper, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, ballotPaper.Member.Value.Person.Value.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.Ballot.Process", "Ballot process naming", "Ballot process");
            entry.Person.Value = ballotPaper.Member.Value.Person.Value;
            db.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        private void RunAll(IDatabase database)
        {
            foreach (var ballot in database.Query<Ballot>())
            {
                RunBallot(database, ballot);
            }
        }

        private Dictionary<Guid, Membership> QueryAllMemberships(IDatabase database, Ballot ballot)
        {
            var list = new Dictionary<Guid, Membership>();

            foreach (var membership in database
                .Query<Membership>(DC.Equal("organizationid", ballot.Template.Value.Organizer.Value.Organization.Value.Id.Value)))
            {
                if (membership.IsActive && (!membership.Person.Value.Deleted.Value))
                {
                    list.Add(membership.Id.Value, membership);
                }
            }

            return list;
        }

        private Dictionary<Guid, BallotPaper> QueryAllBallotPapers(IDatabase database, Ballot ballot)
        {
            var list = new Dictionary<Guid, BallotPaper>();

            foreach (var ballotPaper in database.Query<BallotPaper>(DC.Equal("ballotid", ballot.Id.Value)))
            {
                list.Add(ballotPaper.Member.Value.Id.Value, ballotPaper);
            }

            return list;
        }

        private void RunBallot(IDatabase database, Ballot ballot)
        {
            switch (ballot.Status.Value)
            {
                case BallotStatus.New:
                    {
                        if (DateTime.Now.Date >= ballot.AnnouncementDate.Value.Date)
                        {
                            ballot.Status.Value = BallotStatus.Announcing;
                            database.Save(ballot);
                        }
                        UpdateBallot(database, ballot);
                    }
                    break;
                case BallotStatus.Announcing:
                    {
                        if (DateTime.Now.Date >= ballot.StartDate.Value.Date)
                        {
                            ballot.Status.Value = BallotStatus.Voting;
                            database.Save(ballot);
                        }
                        UpdateBallot(database, ballot);
                    }
                    break;
                case BallotStatus.Voting:
                    {
                        if (DateTime.Now.Date > ballot.EndDate.Value.Date)
                        {
                            ballot.Status.Value = BallotStatus.Finished;
                            database.Save(ballot);
                        }
                        UpdateBallot(database, ballot);
                    }
                    break;
                case BallotStatus.Finished:
                    {
                        UpdateBallot(database, ballot);
                    }
                    break;
            }
        }

        private void UpdateBallot(IDatabase database, Ballot ballot)
        {
            var memberships = QueryAllMemberships(database, ballot);
            var ballotPapers = QueryAllBallotPapers(database, ballot);
            AddNewMembers(database, ballot, memberships, ballotPapers);
            UpdateMembers(database, ballot, memberships, ballotPapers);
        }

        private void UpdateMembers(IDatabase database, Ballot ballot, IDictionary<Guid, Membership> memberships, IDictionary<Guid, BallotPaper> ballotPapers)
        {
            foreach (var ballotPaper in ballotPapers.Values)
            {
                if (DateTime.UtcNow >= ballotPaper.LastTry.Value.AddHours(7d))
                {
                    UpdateMember(database, ballot, memberships, ballotPaper);
                }
            }
        }

        private void UpdateMember(IDatabase database, Ballot ballot, IDictionary<Guid, Membership> memberships, BallotPaper ballotPaper)
        {
            switch (ballotPaper.Status.Value)
            {
                case BallotPaperStatus.New:
                    if (!memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                    {
                        ballotPaper.Status.Value = BallotPaperStatus.Canceled;
                        database.Save(ballotPaper);
                    }
                    else if (ballot.Status.Value == BallotStatus.Announcing)
                    {
                        Announce(database, ballotPaper);
                    }
                    else if (ballot.Status.Value == BallotStatus.Voting)
                    {
                        Invite(database, ballotPaper);
                    }
                    else if (ballot.Status.Value == BallotStatus.Finished)
                    {
                        CheckRight(database, ballotPaper);
                    }
                    break;
                case BallotPaperStatus.Informed:
                    if (!memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                    {
                        ballotPaper.Status.Value = BallotPaperStatus.Canceled;
                        database.Save(ballotPaper);
                    }
                    else if (ballot.Status.Value == BallotStatus.Voting)
                    {
                        Invite(database, ballotPaper);
                    }
                    else if (ballot.Status.Value == BallotStatus.Finished)
                    {
                        CheckRight(database, ballotPaper);
                    }
                    break;
                case BallotPaperStatus.Invited:
                    if (!memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                    {
                        ballotPaper.Status.Value = BallotPaperStatus.Canceled;
                        database.Save(ballotPaper);
                    }
                    else if (ballot.Status.Value == BallotStatus.Finished)
                    {
                        CheckRight(database, ballotPaper);
                    }
                    break;
                case BallotPaperStatus.Canceled:
                    if (memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                    {
                        ballotPaper.Status.Value = BallotPaperStatus.New;
                        database.Save(ballotPaper);
                    }
                    break;
            }
        }

        private void Announce(IDatabase database, BallotPaper ballotPaper)
        {
            if (Global.MailCounter.Available)
            {
                if (Send(database, ballotPaper, true))
                {
                    ballotPaper.Status.Value = BallotPaperStatus.Informed;
                    database.Save(ballotPaper);
                }
            }
        }

        private void Invite(IDatabase database, BallotPaper ballotPaper)
        {
            if (Global.MailCounter.Available)
            {
                if (Send(database, ballotPaper, false))
                {
                    ballotPaper.Status.Value = BallotPaperStatus.Invited;
                    database.Save(ballotPaper);
                }
                else
                {
                    ballotPaper.LastTry.Value = DateTime.UtcNow;
                    database.Save(ballotPaper);
                }
            }
        }

        private bool Send(IDatabase database, BallotPaper ballotPaper, bool announcement)
        {
            var ballot = ballotPaper.Ballot.Value;
            var template = ballot.Template.Value;
            var person = ballotPaper.Member.Value.Person.Value;
            var mailTemplate = announcement ? 
                template.AnnouncementMails.Value(database, person.Language.Value) : 
                template.InvitationMails.Value(database, person.Language.Value);

            if (mailTemplate != null)
            {
                if (SendMail(database, ballotPaper, mailTemplate, announcement))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                if (announcement)
                {
                    Journal(database, ballotPaper,
                    "Document.BallotPaper.NoLanguageTemplateAnnouncement",
                    "When template in that language is available to send ballot announcement",
                    "No mail language template available to send ballot announcement for {0}",
                    t => ballotPaper.Ballot.Value.GetText(t));
                }
                else
                {
                    Journal(database, ballotPaper,
                    "Document.BallotPaper.NoLanguageTemplateInvitation",
                    "When template in that language is available to send ballot invitation",
                    "No mail language template available to send ballot invitation for {0}",
                    t => ballotPaper.Ballot.Value.GetText(t));
                }
                return false;
            }
        }

        public static MimeMessage CreateMail(IDatabase database, BallotPaper ballotPaper, MailTemplate mailTemplate)
        {
            var ballot = ballotPaper.Ballot.Value;
            var template = ballot.Template.Value;
            var person = ballotPaper.Member.Value.Person.Value;

            var from = new MailboxAddress(
                template.Organizer.Value.MailName.Value[person.Language.Value],
                template.Organizer.Value.MailAddress.Value[person.Language.Value]);
            var to = new MailboxAddress(
                person.ShortHand,
                person.PrimaryMailAddress);
            var senderKey = string.IsNullOrEmpty(template.Organizer.Value.GpgKeyId.Value) ? null :
                new GpgPrivateKeyInfo(
                template.Organizer.Value.GpgKeyId.Value,
                template.Organizer.Value.GpgKeyPassphrase.Value);
            var translation = new Translation(database);
            var translator = new Translator(translation, person.Language.Value);
            var templator = new Templator(
                new PersonContentProvider(database, translator, person),
                new BallotPaperContentProvider(translator, ballotPaper));
            var subject = templator.Apply(mailTemplate.Subject);
            var htmlText = templator.Apply(mailTemplate.HtmlText);
            var plainText = templator.Apply(mailTemplate.PlainText);
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainText };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            var htmlPart = new TextPart("html") { Text = htmlText };
            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(htmlPart);

            return Global.Mail.Create(from, to, senderKey, null, subject, alternative);
        }

        private bool SendMail(IDatabase database, BallotPaper ballotPaper, MailTemplate mailTemplate, bool announcement)
        {
            var ballot = ballotPaper.Ballot.Value;
            var template = ballot.Template.Value;
            var person = ballotPaper.Member.Value.Person.Value;

            if (string.IsNullOrEmpty(person.PrimaryMailAddress))
            {
                if (announcement)
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.NoMailAddressAnnouncement",
                        "When no mail address is available to send ballot announcement",
                        "No mail address available to send ballot announcement for {0}",
                        t => ballotPaper.Ballot.Value.GetText(t));
                }
                else
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.NoMailAddressInvitation",
                        "When no mail address is available to send ballot invitation",
                        "No mail address available to send ballot invitation for {0}",
                        t => ballotPaper.Ballot.Value.GetText(t));
                }
                return false;
            }

            if (string.IsNullOrEmpty(mailTemplate.Subject.Value) ||
                string.IsNullOrEmpty(mailTemplate.HtmlText.Value) ||
                string.IsNullOrEmpty(mailTemplate.PlainText.Value))
            {
                if (announcement)
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.IncompleteTemplateAnnouncement",
                        "When template to send announcement is incomplete",
                        "Template to send ballot announcement for {0} in {1} is incomplete",
                        t => ballotPaper.Ballot.Value.GetText(t),
                        t => mailTemplate.Language.Value.Translate(t));
                }
                else
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.IncompleteTemplateInvitation",
                        "When template to send invitation is incomplete",
                        "Template to send ballot invitation for {0} in {1} is incomplete",
                        t => ballotPaper.Ballot.Value.GetText(t),
                        t => mailTemplate.Language.Value.Translate(t));
                }
                return false;
            }

            var message = CreateMail(database, ballotPaper, mailTemplate);

            try
            {
                Global.MailCounter.Used();
                Global.Mail.Send(message);

                if (announcement)
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.SentAnnouncement",
                        "Successfully sent announcement for ballot",
                        "Sent announcement for {0} in {1} by e-mail to {2}",
                        t => ballot.GetText(t),
                        t => mailTemplate.Language.Value.Translate(t),
                        t => person.PrimaryMailAddress);
                }
                else
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.SentInvitation",
                        "Successfully sent invitation for ballot",
                        "Sent invitation to {0} in {1} by e-mail to {2}",
                        t => ballot.GetText(t),
                        t => mailTemplate.Language.Value.Translate(t),
                        t => person.PrimaryMailAddress);
                }

                return true;
            }
            catch (Exception exception)
            {
                if (announcement)
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.SendAnnouncementFail",
                        "Failed to send announcement for ballot",
                        "Failed to send announcement for {0} by e-mail to {1}",
                        t => ballot.GetText(t),
                        t => person.PrimaryMailAddress);
                }
                else
                {
                    Journal(database, ballotPaper,
                        "Document.BallotPaper.SendInvitationFailed",
                        "Failed to send invitation for ballot",
                        "Failed to send invitation to {0} by e-mail to {1}",
                        t => ballot.GetText(t),
                        t => person.PrimaryMailAddress);
                }

                Global.Log.Error(exception.ToString());
                return false;
            }
        }

        private void CheckRight(IDatabase database, BallotPaper ballotPaper)
        {
            ballotPaper.Member.Value.UpdateVotingRight(database);

            if (ballotPaper.Member.Value.HasVotingRight.Value.Value)
            {
                ballotPaper.Status.Value = BallotPaperStatus.RightVerified;
            }
            else
            {
                ballotPaper.Status.Value = BallotPaperStatus.NoRight;
            }

            ballotPaper.LastTry.Value = DateTime.UtcNow;
            database.Save(ballotPaper);
        }

        private void AddNewMembers(IDatabase database, Ballot ballot, IDictionary<Guid, Membership> memberships, IDictionary<Guid, BallotPaper> ballotPapers)
        { 
            foreach (var membership in memberships.Values)
            {
                if (!ballotPapers.ContainsKey(membership.Id))
                {
                    var newBallotPaper = new BallotPaper(Guid.NewGuid());
                    newBallotPaper.Ballot.Value = ballot;
                    newBallotPaper.Member.Value = membership;
                    newBallotPaper.Status.Value = BallotPaperStatus.New;
                    newBallotPaper.LastTry.Value = DateTime.UtcNow.AddDays(-10);
                    database.Save(newBallotPaper);
                    ballotPapers.Add(newBallotPaper.Id.Value, newBallotPaper);
                }
            }
        }
    }
}

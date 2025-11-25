using System;
using System.Linq;
using SiteLibrary;
using BaseLibrary;
using MimeKit;

namespace Quaestur
{
    public class PointsTallyTask : ITask
    {
        private DateTime _lastSending;

        public PointsTallyTask()
        {
            _lastSending = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                Global.Log.Info("Running points tally task");

                RunAll(database);

                Global.Log.Info("Billing points tally complete");
            }
        }

        private static void Journal(IDatabase db, Membership membership, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, membership.Person.Value.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.PointsTally.Process", "Points tally process naming", "Points tally process");
            entry.Person.Value = membership.Person.Value;
            db.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        private void RunAll(IDatabase database)
        {
            var translation = new Translation(database);
            var persons = database.Query<Person>().ToList();

            foreach (var person in persons
                .Where(p => !p.Deleted.Value))
            {
                try
                {
                    ProcessPerson(database, translation, person);
                }
                catch (Exception exception)
                {
                    Global.Log.Error("Point tally for {0} failed to process: {1}", person.ShortHand, exception.ToString());
                    Global.Mail.SendAdmin(
                        "Point tally process failed",
                        string.Format("Point tally failed to process: {0}", exception.ToString()));
                }
            }
        }

        private static void ProcessPerson(IDatabase database, Translation translation, Person person)
        {
            if (Global.MailCounter.Available)
            {
                var membership = person.Memberships
                    .Where(m => m.Type.Value.Collection.Value == CollectionModel.Direct &&
                                m.Type.Value.Payment.Value != PaymentModel.None &&
                                m.Type.Value.MaximumPoints.Value > 0)
                    .OrderByDescending(m => m.Organization.Value.Subordinates.Count())
                    .FirstOrDefault();

                if (membership != null)
                {
                    var lastTally = database
                        .Query<PointsTally>(DC.Equal("personid", person.Id.Value))
                        .OrderByDescending(t => t.UntilDate.Value)
                        .FirstOrDefault();
                    var lastTallyUntilDate = lastTally == null ? new DateTime(1850, 1, 1) : lastTally.UntilDate.Value;
                    var untilDate = PointsTallyDocument.ComputeUntilDate(database, membership, lastTally);
                    Global.Log.Info(
                        "Checking tally for {0} (last tally {1}, until date {2})",
                        person.ShortHand,
                        lastTallyUntilDate,
                        untilDate);

                    if (DateTime.UtcNow.Date > untilDate.Date &&
                        untilDate.Date > lastTallyUntilDate.Date)
                    {
                        CreatePointsTallyAndSend(database, translation, membership);
                    }
                }
            }
        }

        public static void CreatePointsTallyAndSend(IDatabase database, Translation translation, Membership membership)
        {
            var tally = CreatePointsTallyInternal(database, translation, membership);

            if (tally != null)
            {
                SendTally(database, membership, tally);
            }
        }

        private static void SendTally(IDatabase database, Membership membership, PointsTally tally)
        {
            var pointsTallyMailTemplate = membership.Type.Value.PointsTallyMails.Value(database, membership.Person.Value.Language.Value);
            var message = CreateMail(database, membership, pointsTallyMailTemplate, tally);
            Global.MailCounter.Used();
            Global.Mail.Send(message);
        }

        private static PointsTally CreatePointsTallyInternal(IDatabase database, Translation translation, Membership membership)
        {
            Translator translator = new Translator(translation, membership.Person.Value.Language.Value);
            var pointsTallyDocument = new PointsTallyDocument(translator, database, membership);

            if (pointsTallyDocument.Create())
            {
                using (var transaction = database.BeginTransaction())
                {
                    database.Save(pointsTallyDocument.PointsTally);
                    Journal(
                        database,
                        membership,
                        "Document.PointsTally.Created",
                        "Points tally created message",
                        "Created points tally {0} for {1}",
                        t => pointsTallyDocument.PointsTally.GetText(translator),
                        t => pointsTallyDocument.PointsTally.Person.Value.ShortHand);
                    transaction.Commit();
                    return pointsTallyDocument.PointsTally;
                }
            }
            else
            {
                Journal(
                    database,
                    membership,
                    "Document.PointsTally.Failed",
                    "Points tally creation failed message",
                    "Creation of bill {0} for {1} failed",
                    t => pointsTallyDocument.PointsTally.GetText(translator),
                    t => pointsTallyDocument.PointsTally.Person.Value.ShortHand);
                Global.Log.Error(pointsTallyDocument.ErrorText);
                return null;
            }
        }

        public static MimeMessage CreateMail(IDatabase database, Membership membership, MailTemplate mailTemplate, PointsTally tally)
        {
            var person = membership.Person.Value;
            var type = membership.Type.Value;
            var group = type.SenderGroup.Value;

            var from = new MailboxAddress(
                group.MailName.Value[person.Language.Value],
                group.MailAddress.Value[person.Language.Value]);
            var to = new MailboxAddress(
                person.ShortHand,
                person.PrimaryMailAddress);
            var senderKey = string.IsNullOrEmpty(group.GpgKeyId.Value) ? null :
                new GpgPrivateKeyInfo(
                group.GpgKeyId.Value,
                group.GpgKeyPassphrase.Value);
            var translation = new Translation(database);
            var translator = new Translator(translation, person.Language.Value);
            var templator = new Templator(
                new PersonContentProvider(database, translator, person));
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

            if (tally != null)
            {
                var content = new Multipart("mixed");
                content.Add(alternative);
                var pdfFileName = tally.CreatedDate.Value.ToString("yyyy-MM-dd") + ".pdf";
                var documentStream = new System.IO.MemoryStream(tally.DocumentData.Value);
                var documentPart = new MimePart("application", "pdf");
                documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
                documentPart.ContentType.Name = pdfFileName;
                documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                documentPart.ContentDisposition.FileName = pdfFileName;
                documentPart.ContentTransferEncoding = ContentEncoding.Base64;
                content.Add(documentPart);

                return Global.Mail.Create(from, to, senderKey, null, subject, content);
            }
            else
            {
                return Global.Mail.Create(from, to, senderKey, null, subject, alternative);
            }
        }
    }
}

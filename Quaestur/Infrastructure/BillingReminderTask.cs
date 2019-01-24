using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;

namespace Quaestur
{
    public class BillingReminderTask : ITask
    {
        private DateTime _lastSending;
        private int _maxMailsCount;

        public BillingReminderTask()
        {
            _lastSending = DateTime.MinValue;
        }

        private double DaysSinceLastReminder(Bill bill)
        {
            if (bill.ReminderDate.Value.HasValue)
            {
                return DateTime.UtcNow.Subtract(bill.ReminderDate.Value.Value).TotalDays;
            }
            else
            {
                return 9999999d;
            }
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                _maxMailsCount = 500;
                Global.Log.Notice("Running mailing task");

                foreach (var bill in database
                    .Query<Bill>()
                    .Where(b => b.Status.Value == BillStatus.New)
                    .Where(b => DaysSinceLastReminder(b) > b.Membership.Value.Type.Value.GetReminderPeriod())
                    .OrderByDescending(DaysSinceLastReminder))
                {
                    Send(database, bill);
                    _maxMailsCount--;
                    if (_maxMailsCount < 1) break;
                }

                Global.Log.Notice("Mailing task complete");
            }
        }

        private void Journal(IDatabase db, Bill bill, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, bill.Membership.Value.Person.Value.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.BillingReminder.Process", "Billing process naming", "Billing process");
            entry.Person.Value = bill.Membership.Value.Person.Value;
            db.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        private void Send(IDatabase database, Bill bill)
        {
            var translation = new Translation(database);
            var level = bill.ReminderLevel.Value + 1;
            var membership = bill.Membership.Value;
            var membershipType = membership.Type.Value;
            var person = membership.Person.Value;
            var translator = new Translator(translation, person.Language.Value);
            var template = database
                .Query<BillSendingTemplate>(DC.Equal("membershiptypeid", membershipType.Id.Value))
                .FirstOrDefault(t => t.Language == person.Language.Value && level >= t.MinReminderLevel && level <= t.MaxReminderLevel);

            if (template == null)
            {
                Global.Log.Notice("No bill sending template for {0} in {1} in {2} at level {3}",
                    membershipType.Name.Value[person.Language.Value],
                    membershipType.Organization.Value.Name.Value[person.Language.Value],
                    person.Language.Value.Translate(translator),
                    level);
                bill.ReminderDate.Value = DateTime.UtcNow.AddDays(-7d);
                database.Save(bill);
                return;
            }

            switch (template.SendingMode.Value)
            {
                case SendingMode.MailOnly:
                    if (SendMail(database, bill, level, person, template))
                    {
                        bill.ReminderLevel.Value = level;
                        bill.ReminderDate.Value = DateTime.UtcNow;
                        database.Save(bill);
                    }
                    else
                    {
                        SendingFailed(database, bill, level, person, template);
                        bill.ReminderDate.Value = DateTime.UtcNow.AddDays(-9d);
                        database.Save(bill);
                    }
                    break;
                case SendingMode.PostalOnly:
                    if (SendPostal(database, translator, bill, level, person, template))
                    {
                        bill.ReminderLevel.Value = level;
                        bill.ReminderDate.Value = DateTime.UtcNow;
                        database.Save(bill);
                    }
                    else
                    {
                        SendingFailed(database, bill, level, person, template);
                        bill.ReminderDate.Value = DateTime.UtcNow.AddDays(-9d);
                        database.Save(bill);
                    }
                    break;
                case SendingMode.MailPreferred:
                    if (SendMail(database, bill, level, person, template))
                    {
                        bill.ReminderLevel.Value = level;
                        bill.ReminderDate.Value = DateTime.UtcNow;
                        database.Save(bill);
                    }
                    else if (SendPostal(database, translator, bill, level, person, template))
                    {
                        bill.ReminderLevel.Value = level;
                        bill.ReminderDate.Value = DateTime.UtcNow;
                        database.Save(bill);
                    }
                    else
                    {
                        SendingFailed(database, bill, level, person, template);
                        bill.ReminderDate.Value = DateTime.UtcNow.AddDays(-9d);
                        database.Save(bill);
                    }
                    break;
                case SendingMode.PostalPrefrerred:
                    if (SendPostal(database, translator, bill, level, person, template))
                    {
                        bill.ReminderLevel.Value = level;
                        bill.ReminderDate.Value = DateTime.UtcNow;
                        database.Save(bill);
                    }
                    else if (SendMail(database, bill, level, person, template))
                    {
                        bill.ReminderLevel.Value = level;
                        bill.ReminderDate.Value = DateTime.UtcNow;
                        database.Save(bill);
                    }
                    else
                    {
                        SendingFailed(database, bill, level, person, template);
                        bill.ReminderDate.Value = DateTime.UtcNow.AddDays(-9d);
                        database.Save(bill);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void SendingFailed(IDatabase database, Bill bill, int level, Person person, BillSendingTemplate template)
        {
            if (level < 2)
            {
                Journal(database, bill,
                    "Document.BillingReminder.InitalFailed",
                    "Sending bill failed",
                    "Sending bill {0} using template {1} failed",
                    t => bill.Number.Value,
                    t => template.Name.Value);
            }
            else
            {
                Journal(database, bill,
                    "Document.BillingReminder.ReminderFailed",
                    "Sending bill reminder failed",
                    "Sending reminder for bill {0} at level {1} using template {2} failed",
                    t => bill.Number.Value,
                    t => level.ToString(),
                    t => template.Name.Value);
            }
        }

        private bool SendPostal(IDatabase database, Translator translator, Bill bill, int level, Person person, BillSendingTemplate template)
        {
            if ((person.PrimaryPostalAddress == null) ||
                (!person.PrimaryPostalAddress.IsValid))
            {
                Journal(database, bill,
                    "Document.BillingReminder.NoPostalAddress",
                    "When no postal address is available in billing",
                    "No postal address available to send bill {0}",
                    t => bill.Number.Value);
                return false;
            }

            var letter = new UniversalDocument(translator, person, template.LetterLatex);
            var document = letter.Compile();

            if (document == null)
            {
                Journal(database, bill,
                    "Document.BillingReminder.CannotCompile",
                    "When letter clould not be compiled",
                    "Cannot compile letter to send bill {0} by post",
                    t => bill.Number.Value);
                return false;
            }

            try
            {
                var both = PdfUnite.Work(document, bill.DocumentData.Value);

                var pingen = new Pingen(Global.Config.PingenApiToken);
                pingen.Upload(both, false, PingenSpeed.Economy);

                if (level < 2)
                {
                    Journal(database, bill,
                        "Document.BillingReminder.SentInitialPostal",
                        "Successfully sent bill by post",
                        "Sent bill {0} using template {1} by post to {2}",
                        t => bill.Number.Value,
                        t => template.Name.Value,
                        t => person.PrimaryPostalAddressText(translator));
                }
                else
                {
                    Journal(database, bill,
                        "Document.BillingReminder.SentReminderPostal",
                        "Successfully sent bill reminder by post",
                        "Sent reminder for bill {0} at level {1} using template {2} by post to {3}",
                        t => bill.Number.Value,
                        t => level.ToString(),
                        t => template.Name.Value,
                        t => person.PrimaryPostalAddressText(translator));
                }

                return true;
            }
            catch (Exception exception)
            {
                Journal(database, bill,
                    "Document.BillingReminder.PostaFailed",
                    "Sending mail failed in billing",
                    "Sending postal to {0} failed",
                    t => person.PrimaryPostalAddressText(translator));
                Global.Log.Error(exception.ToString());
                return false;
            }
        }

        private bool SendMail(IDatabase database, Bill bill, int level, Person person, BillSendingTemplate template)
        {
            if (string.IsNullOrEmpty(person.PrimaryMailAddress))
            {
                Journal(database, bill,
                    "Document.BillingReminder.NoMailAddress",
                    "When no mail address is available in billing",
                    "No mail address available to send bill {0}",
                    t => bill.Number.Value);
                return false;
            }

            var from = new MailboxAddress(
                template.MailSender.Value.MailName.Value[person.Language.Value],
                template.MailSender.Value.MailAddress.Value[person.Language.Value]);
            var to = new MailboxAddress(
                person.ShortHand,
                person.PrimaryMailAddress);
            var senderKey = template.MailSender.Value.GpgKeyId.Value == null ? null :
                new GpgPrivateKeyInfo(
                template.MailSender.Value.GpgKeyId.Value,
                template.MailSender.Value.GpgKeyPassphrase.Value);
            var recipientKey = person.GetPublicKey();
            var content = new Multipart("mixed");
            var translation = new Translation(database);
            var translator = new Translator(translation, person.Language.Value);
            var templator = new Templator(new PersonContentProvider(translator, person));
            var htmlText = templator.Apply(template.MailHtmlText);
            var plainText = templator.Apply(template.MailPlainText);
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainText };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            var htmlPart = new TextPart("html") { Text = htmlText };
            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(htmlPart);
            content.Add(alternative);
            var documentStream = new System.IO.MemoryStream(bill.DocumentData);
            var documentPart = new MimePart("application", "pdf");
            documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
            documentPart.ContentType.Name = bill.Number.Value + ".pdf";
            documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
            documentPart.ContentDisposition.FileName = bill.Number.Value + ".pdf";
            documentPart.ContentTransferEncoding = ContentEncoding.Base64;
            content.Add(documentPart);

            try
            {
                Global.Mail.Send(from, to, senderKey, recipientKey, template.MailSubject, content);

                if (level < 2)
                {
                    Journal(database, bill,
                        "Document.BillingReminder.SentInitialMail",
                        "Successfully sent bill",
                        "Sent bill {0} using template {1} by e-mail to {2}",
                        t => bill.Number.Value,
                        t => template.Name.Value,
                        t => person.PrimaryMailAddress);
                }
                else
                {
                    Journal(database, bill,
                        "Document.BillingReminder.SentReminderMail",
                        "Successfully sent bill reminder",
                        "Sent reminder for bill {0} at level {1} using template {2} by e-mail to {3}",
                        t => bill.Number.Value,
                        t => level.ToString(),
                        t => template.Name.Value,
                        t => person.PrimaryMailAddress);
                }

                return true;
            }
            catch (Exception exception)
            {
                Journal(database, bill,
                    "Document.BillingReminder.MailFailed",
                    "Sending mail failed in billing",
                    "Sending e-mail to {0} failed",
                    t => person.PrimaryMailAddress);
                Global.Log.Error(exception.ToString());
                return false;
            }
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class BillingReminderTask : ITask
    {
        private DateTime _lastSending;

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

        private class Billing
        {
            public Organization Organization { get; private set; }
            public Person Person { get; private set; }
            public List<Bill> Bills { get; private set; }
            public string Id { get { return Organization.Id.Value.ToString() + "-" + Person.Id.Value.ToString(); } }

            public Billing(Organization organization, Person person)
            {
                Organization = organization;
                Person = person;
                Bills = new List<Bill>();
            }
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                Global.Log.Notice("Running mailing task");
                var translation = new Translation(database);
                var remindPersons = new Dictionary<string, Billing>();

                foreach (var bill in database
                    .Query<Bill>()
                    .Where(b => b.Status.Value == BillStatus.New && !b.Membership.Value.Person.Value.Deleted.Value)
                    .OrderByDescending(DaysSinceLastReminder))
                {
                    var currentPrepayment = bill.Membership.Value.Person.Value.CurrentPrepayment(database);

                    if (currentPrepayment >= bill.Amount.Value)
                    {
                        var translator = new Translator(translation, bill.Membership.Value.Person.Value.Language.Value);

                        using (var transaction = database.BeginTransaction())
                        {
                            var prepayment = new Prepayment(Guid.NewGuid());
                            prepayment.Person.Value = bill.Membership.Value.Person.Value;
                            prepayment.Moment.Value = DateTime.UtcNow;
                            prepayment.Amount.Value = -bill.Amount.Value;
                            prepayment.Reason.Value = translator.Get(
                                "BillingReminderTask.Prepayment.Reason.SettledBill",
                                "Settle bill with prepayment in billing remainder task",
                                "Settled bill {0}",
                                bill.Number.Value);
                            database.Save(prepayment);

                            bill.Status.Value = BillStatus.Payed;
                            bill.PayedDate.Value = prepayment.Moment.Value;
                            database.Save(bill);

                            Journal(database, bill.Membership.Value.Person.Value,
                                "BillingReminderTask.Journal.Prepayment.SettledBill",
                                "Journal entry for settle bill from prepayment",
                                "Settled bill {0} with amount {1} from prepayment",
                                t => bill.Number.Value,
                                t => bill.Amount.Value.FormatMoney());

                            transaction.Commit();
                        }
                    }
                    else
                    {
                        var billing = new Billing(
                            bill.Membership.Value.Organization.Value, 
                            bill.Membership.Value.Person.Value);

                        if (!remindPersons.ContainsKey(billing.Id))
                        {
                            remindPersons.Add(billing.Id, billing);
                        }

                        remindPersons[billing.Id].Bills.Add(bill);
                    }
                }

                foreach (var billing in remindPersons.Values)
                {
                    if (billing.Bills.Any(b => DaysSinceLastReminder(b) > b.Membership.Value.Type.Value.GetReminderPeriod(database)) &&
                        Global.MailCounter.Available)
                    {
                        Send(database, billing);
                    }
                }

                Global.Log.Notice("Mailing task complete");
            }
        }

        private void Journal(IDatabase db, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, person.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.BillingReminder.Process", "Billing process naming", "Billing process");
            entry.Person.Value = person;
            db.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        private BillSendingTemplate SelectTemplate(IDatabase database, Person person, IEnumerable<Bill> bills, int level)
        {
            var allTemplates = database.Query<BillSendingTemplate>();
            return allTemplates
                .Where(t => person.ActiveMemberships.Any(m => m.Type.Value == t.MembershipType.Value) &&
                            level >= t.MinReminderLevel.Value && level <= t.MaxReminderLevel.Value)
                .OrderByDescending(t => t.MembershipType.Value.Organization.Value.Subordinates.Count())
                .FirstOrDefault();
        }

        private void Send(IDatabase database, Billing billing)
        {
            var translation = new Translation(database);
            var translator = new Translator(translation, billing.Person.Language.Value);
            var level = billing.Bills.Max(b => b.ReminderLevel.Value + 1);
            var template = SelectTemplate(database, billing.Person, billing.Bills, level);

            if (template == null)
            {
                Global.Log.Notice("No bill sending template for {0} at level {1}",
                    billing.Person.ShortHand,
                    level);
                UpdateBills(database, billing, DateTime.UtcNow.AddDays(-7d), null);
                return;
            }

            switch (template.SendingMode.Value)
            {
                case SendingMode.MailOnly:
                    if (SendMail(database, translator, billing, template))
                    {
                        UpdateBills(database, billing, DateTime.UtcNow, level);
                    }
                    else
                    {
                        SendingFailed(database, billing, template);
                        UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                    }
                    break;
                case SendingMode.PostalOnly:
                    if (SendPostal(database, translator, billing, template))
                    {
                        UpdateBills(database, billing, DateTime.UtcNow, level);
                    }
                    else
                    {
                        SendingFailed(database, billing, template);
                        UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                    }
                    break;
                case SendingMode.MailPreferred:
                    if (SendMail(database, translator, billing, template))
                    {
                        UpdateBills(database, billing, DateTime.UtcNow, level);
                    }
                    else if (SendPostal(database, translator, billing, template))
                    {
                        UpdateBills(database, billing, DateTime.UtcNow, level);
                    }
                    else
                    {
                        SendingFailed(database, billing, template);
                        UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                    }
                    break;
                case SendingMode.PostalPrefrerred:
                    if (SendPostal(database, translator, billing, template))
                    {
                        UpdateBills(database, billing, DateTime.UtcNow, level);
                    }
                    else if (SendMail(database, translator, billing, template))
                    {
                        UpdateBills(database, billing, DateTime.UtcNow, level);
                    }
                    else
                    {
                        SendingFailed(database, billing, template);
                        UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void UpdateBills(IDatabase database, Billing billing, DateTime date, int? level)
        {
            using (ITransaction transaction = database.BeginTransaction())
            {
                foreach (var bill in billing.Bills)
                {
                    if (level.HasValue)
                    {
                        bill.ReminderLevel.Value = level.Value;
                    }

                    bill.ReminderDate.Value = date;
                    bill.Membership.Value.UpdateVotingRight(database);
                    database.Save(bill);
                }
            }
        }

        private string ComputeBillText(Translator translator, IEnumerable<Bill> bills)
        { 
            return string.Join(", ", bills
                .Select(b => translator.Get(
                    "Document.BillingReminder.BillText",
                    "Bill text when sending bill failed",
                    "{0} at level {1}",
                    b.Number.Value,
                    b.ReminderLevel.Value)));
        }

        private void SendingFailed(IDatabase database, Billing billing, BillSendingTemplate template)
        {
            Journal(database, billing.Person,
                "Document.BillingReminder.Failed",
                "Sending bill failed",
                "Sending bill(s) {0} using template {1} failed",
                t => ComputeBillText(t, billing.Bills),
                t => template.Name.Value);
        }

        private bool SendPostal(IDatabase database, Translator translator, Billing billing, BillSendingTemplate template)
        {
            if ((billing.Person.PrimaryPostalAddress == null) ||
                (!billing.Person.PrimaryPostalAddress.IsValid))
            {
                Journal(database, billing.Person,
                    "Document.BillingReminder.NoPostalAddress",
                    "When no postal address is available in billing",
                    "No postal address available to send bills");
                return false;
            }

            byte[] document = CreateLetter(database, translator, billing.Person, template);

            if (document == null)
            {
                Journal(database, billing.Person,
                    "Document.BillingReminder.CannotCompile",
                    "When letter clould not be compiled",
                    "Cannot compile letter to send bill(s) by post");
                return false;
            }

            try
            {
                var pdfDocuments = document;

                if (billing.Bills.Count() == 1)
                {
                    var bill = billing.Bills.Single();
                    pdfDocuments = PdfUnite.Work(pdfDocuments, bill.DocumentData);
                }
                else
                {
                    var arrearsListDocument = CreateArrearsList(database, translator, billing, template);

                    if (arrearsListDocument != null)
                    {
                        pdfDocuments = PdfUnite.Work(pdfDocuments, arrearsListDocument);
                    }
                    else
                    {
                        Journal(database, billing.Person,
                            "Document.BillingReminder.ArrearsList.CannotCompile",
                            "ArrearsList document cloud not be compiled when sending bill",
                            "Clould not create arrears document for bill(s) {0} using template {1}",
                            t => ComputeBillText(translator, billing.Bills),
                            t => template.Name.Value);
                        return false;
                    }
                }

                var pingen = new Pingen(Global.Config.PingenApiToken);
                pingen.Upload(pdfDocuments, false, PingenSpeed.Economy);

                Journal(database, billing.Person,
                    "Document.BillingReminder.SentInitialPostal",
                    "Successfully sent bill by post",
                    "Sent bill(s) {0} using template {1} by post to {2}",
                    t => ComputeBillText(translator, billing.Bills),
                    t => template.Name.Value,
                    t => billing.Person.PrimaryPostalAddressText(translator));
                return true;
            }
            catch (Exception exception)
            {
                Journal(database, billing.Person,
                    "Document.BillingReminder.PostaFailed",
                    "Sending mail failed in billing",
                    "Sending postal to {0} failed",
                    t => billing.Person.PrimaryPostalAddressText(translator));
                Global.Log.Error(exception.ToString());
                return false;
            }
        }

        private static byte[] CreateLetter(IDatabase database, Translator translator, Person person, BillSendingTemplate template)
        {
            var latexTemplate = template.GetBillSendingLetter(database, person.Language.Value);
            var letter = new UniversalDocument(translator, person, latexTemplate.Text.Value);
            return letter.Compile();
        }

        private static byte[] CreateArrearsList(IDatabase database, Translator translator, Billing billing, BillSendingTemplate template)
        {
            var latexTemplate = template.GetBillSendingArrearsList(database, billing.Person.Language.Value);
            var letter = new ArrearsDocument(database, translator, billing.Organization, billing.Person, billing.Bills, latexTemplate.Text.Value);
            return letter.Compile();
        }

        private bool SendMail(IDatabase database, Translator translator, Billing billing, BillSendingTemplate template)
        {
            if (string.IsNullOrEmpty(billing.Person.PrimaryMailAddress))
            {
                Journal(database, billing.Person,
                    "Document.BillingReminder.NoMailAddress",
                    "When no mail address is available in billing",
                    "No mail address available to send bills");
                return false;
            }

            var from = new MailboxAddress(
                template.MailSender.Value.MailName.Value[billing.Person.Language.Value],
                template.MailSender.Value.MailAddress.Value[billing.Person.Language.Value]);
            var to = new MailboxAddress(
                billing.Person.ShortHand,
                billing.Person.PrimaryMailAddress);
            var senderKey = string.IsNullOrEmpty(template.MailSender.Value.GpgKeyId.Value) ? null :
                new GpgPrivateKeyInfo(
                template.MailSender.Value.GpgKeyId.Value,
                template.MailSender.Value.GpgKeyPassphrase.Value);
            var recipientKey = billing.Person.GetPublicKey();
            var content = new Multipart("mixed");
            var templator = new Templator(new PersonContentProvider(translator, billing.Person));
            var mailTemplate = template.GetBillSendingMail(database, billing.Person.Language.Value);
            var htmlText = templator.Apply(mailTemplate.HtmlText.Value);
            var plainText = templator.Apply(mailTemplate.PlainText.Value);
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainText };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            var htmlPart = new TextPart("html") { Text = htmlText };
            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(htmlPart);
            content.Add(alternative);

            if (billing.Bills.Count() == 1)
            {
                var bill = billing.Bills.Single();
                AddDocument(content, bill.DocumentData, bill.Number.Value);
            }
            else
            {
                var document = CreateArrearsList(database, translator, billing, template);

                if (document != null)
                {
                    var arrearsListDocumentName = translator.Get(
                        "Document.BillingReminder.ArrearsList.DocumentName",
                        "Name of the arrears list document when sending bill",
                        "arrears");

                    AddDocument(content, document, arrearsListDocumentName);
                }
                else
                {
                    Journal(database, billing.Person,
                        "Document.BillingReminder.ArrearsList.CannotCompile",
                        "ArrearsList document cloud not be compiled when sending bill",
                        "Clould not create arrears document for bill(s) {0} using template {1}",
                        t => ComputeBillText(translator, billing.Bills),
                        t => template.Name.Value);
                    return false; 
                }
            }

            try
            {
                Global.MailCounter.Used();
                Global.Mail.Send(from, to, senderKey, recipientKey, mailTemplate.Subject.Value, content);

                Journal(database, billing.Person,
                    "Document.BillingReminder.SentMail",
                    "Successfully sent bill",
                    "Sent bill(s) {0} using template {1} by e-mail to {2}",
                    t => ComputeBillText(translator, billing.Bills),
                    t => template.Name.Value,
                    t => billing.Person.PrimaryMailAddress);
                return true;
            }
            catch (Exception exception)
            {
                Journal(database, billing.Person,
                    "Document.BillingReminder.MailFailed",
                    "Sending mail failed in billing",
                    "Sending e-mail to {0} failed",
                    t => billing.Person.PrimaryMailAddress);
                Global.Log.Error(exception.ToString());
                return false;
            }
        }

        private static void AddDocument(Multipart content, byte[] documentData, string documentName)
        {
            var documentStream = new System.IO.MemoryStream(documentData);
            var documentPart = new MimePart("application", "pdf");
            documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
            documentPart.ContentType.Name = documentName + ".pdf";
            documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
            documentPart.ContentDisposition.FileName = documentName + ".pdf";
            documentPart.ContentTransferEncoding = ContentEncoding.Base64;
            content.Add(documentPart);
        }
    }
}

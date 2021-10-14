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

        private static double DaysSinceLastReminder(Bill bill)
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
                Global.Log.Info("Running mailing task");
                var translation = new Translation(database);
                var remindPersons = new Dictionary<string, Billing>();

                foreach (var bill in database
                    .Query<Bill>()
                    .Where(b => b.Status.Value == BillStatus.New && !b.Membership.Value.Person.Value.Deleted.Value)
                    .OrderByDescending(DaysSinceLastReminder))
                {
                    var currentPrepayment = bill.Membership.Value.Person.Value.CurrentPrepayment(database);

                    var billing = new Billing(
                        bill.Membership.Value.Organization.Value,
                        bill.Membership.Value.Person.Value);

                    if (!remindPersons.ContainsKey(billing.Id))
                    {
                        remindPersons.Add(billing.Id, billing);
                    }

                    remindPersons[billing.Id].Bills.Add(bill);
                }

                foreach (var billing in remindPersons.Values)
                {
                    RemindOrSettleInternal(database, translation, billing, false);
                }

                Global.Log.Info("Mailing task complete");
            }
        }

        public static void RemindOrSettle(IDatabase database, Translation translation, Membership membership, bool forceSend)
        {
            var billing = new Billing(membership.Organization.Value, membership.Person.Value);
            billing.Bills.AddRange(database
                .Query<Bill>(DC.Equal("membershipid", membership.Id.Value))
                .Where(b => b.Status.Value == BillStatus.New)
                .OrderByDescending(DaysSinceLastReminder));
            RemindOrSettleInternal(database, translation, billing, forceSend);
        }

        private static void RemindOrSettleInternal(IDatabase database, Translation translation, Billing billing, bool forceReminder)
        {
            var prepayment = billing.Person.CurrentPrepayment(database);
            var outstanding = billing.Bills.Sum(b => b.Amount) - prepayment;
            var forceSend = billing.Bills.Any(b => DaysSinceLastReminder(b) > b.Membership.Value.Type.Value.GetReminderPeriod(database));

            if (outstanding <= 0m)
            {
                SettleBills(database, translation, billing);
            }
            else if ((forceReminder && Global.MailCounter.Available) || forceSend)
            {
                SendReminder(database, billing);
            }
        }

        private static void SettleBills(IDatabase database, Translation translation, Billing billing)
        {
            var translator = new Translator(translation, billing.Person.Language.Value);

            SentSettlementMail(database, billing);

            foreach (var bill in billing.Bills)
            {
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
        }

        private static void Journal(IDatabase db, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
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

        private static BillSendingTemplate SelectTemplate(IDatabase database, Person person, IEnumerable<Bill> bills, int level)
        {
            var allTemplates = database.Query<BillSendingTemplate>();
            return allTemplates
                .Where(t => person.ActiveMemberships.Any(m => m.Type.Value == t.MembershipType.Value) &&
                            level >= t.MinReminderLevel.Value && level <= t.MaxReminderLevel.Value)
                .OrderByDescending(t => t.MembershipType.Value.Organization.Value.Subordinates.Count())
                .FirstOrDefault();
        }

        private static void SendReminder(IDatabase database, Billing billing)
        {
            var translation = new Translation(database);
            var translator = new Translator(translation, billing.Person.Language.Value);
            var level = 
                billing.HasLevelZero ? 1 :
                billing.Bills.Max(b => b.ReminderLevel.Value + 1);
            var template = SelectTemplate(database, billing.Person, billing.Bills, level);
            var sendBilling = billing.HasLevelZero ? billing.SelectLevelZero() : billing;

            using (ITransaction transaction = database.BeginTransaction())
            {
                if (template == null)
                {
                    Journal(database, billing.Person,
                        "Document.BillingReminder.NoTemplate",
                        "Whenn a billing reminder cannot be send because no template is configured",
                        "Sending bill(s) failed for lack of a template",
                        t => ComputeBillText(t, billing.Bills),
                        t => template.Name.Value);
                    UpdateBills(database, billing, DateTime.UtcNow.AddDays(-7d), null);
                    return;
                }

                switch (template.SendingMode.Value)
                {
                    case SendingMode.MailOnly:
                        if (SendReminderMail(database, translator, sendBilling, template))
                        {
                            UpdateBills(database, billing, DateTime.UtcNow, level);
                        }
                        else
                        {
                            SendingFailed(database, sendBilling, template);
                            UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                        }
                        break;
                    case SendingMode.PostalOnly:
                        if (SendPostalReminder(database, translator, sendBilling, template))
                        {
                            UpdateBills(database, billing, DateTime.UtcNow, level);
                        }
                        else
                        {
                            SendingFailed(database, sendBilling, template);
                            UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                        }
                        break;
                    case SendingMode.MailPreferred:
                        if (SendReminderMail(database, translator, sendBilling, template))
                        {
                            UpdateBills(database, billing, DateTime.UtcNow, level);
                        }
                        else if (SendPostalReminder(database, translator, sendBilling, template))
                        {
                            UpdateBills(database, billing, DateTime.UtcNow, level);
                        }
                        else
                        {
                            SendingFailed(database, sendBilling, template);
                            UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                        }
                        break;
                    case SendingMode.PostalPrefrerred:
                        if (SendPostalReminder(database, translator, sendBilling, template))
                        {
                            UpdateBills(database, billing, DateTime.UtcNow, level);
                        }
                        else if (SendReminderMail(database, translator, sendBilling, template))
                        {
                            UpdateBills(database, billing, DateTime.UtcNow, level);
                        }
                        else
                        {
                            SendingFailed(database, sendBilling, template);
                            UpdateBills(database, billing, DateTime.UtcNow.AddDays(-9), null);
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }

                transaction.Commit();
            }
        }

        private static void UpdateBills(IDatabase database, Billing billing, DateTime date, int? level)
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

        private static string ComputeBillText(Translator translator, IEnumerable<Bill> bills)
        { 
            return string.Join(", ", bills
                .Select(b => translator.Get(
                    "Document.BillingReminder.BillText",
                    "Bill text when sending bill failed",
                    "{0} at level {1}",
                    b.Number.Value,
                    b.ReminderLevel.Value)));
        }

        private static void SendingFailed(IDatabase database, Billing billing, BillSendingTemplate template)
        {
            Journal(database, billing.Person,
                "Document.BillingReminder.Failed",
                "Sending bill failed",
                "Sending bill(s) {0} using template {1} failed",
                t => ComputeBillText(t, billing.Bills),
                t => template.Name.Value);
        }

        private static bool SendPostalReminder(IDatabase database, Translator translator, Billing billing, BillSendingTemplate template)
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

                var settlementDocument = CreateSettlement(database, translator, billing);

                if (settlementDocument != null)
                {
                    pdfDocuments = PdfUnite.Work(pdfDocuments, settlementDocument);

                    foreach (var bill in billing.Bills)
                    {
                        pdfDocuments = PdfUnite.Work(pdfDocuments, bill.DocumentData);
                    }
                }
                else
                {
                    Journal(database, billing.Person,
                        "Document.BillingReminder.Settlement.CannotCompile",
                        "Settlement document cloud not be compiled when sending bill",
                        "Clould not create settlement document for bill(s) {0} using template {1}",
                        t => ComputeBillText(translator, billing.Bills),
                        t => template.Name.Value);
                    return false;
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
            var document = letter.Compile();

            if (document == null)
            {
                var texDocument = new TextAttachement(letter.TexDocument, "document.tex");
                var errorDocument = new TextAttachement(letter.ErrorText, "error.txt");
                Global.Mail.SendAdminEncrypted(
                    "LaTeX Error", "Could not compile bill sending letter",
                    texDocument, errorDocument);
            }

            return document;
        }

        private static byte[] CreateSettlement(IDatabase database, Translator translator, Billing billing)
        {
            var membership = billing.Person.Memberships.First(m => m.Organization.Value == billing.Organization);
            var latexTemplate = membership.Type.Value.GetSettlementDocument(database, translator.Language);
            var settlement = new SettlementDocument(database, translator, billing.Organization, billing.Person, billing.Bills, latexTemplate.Text.Value);
            var document = settlement.Compile();

            if (document == null)
            {
                Journal(database, billing.Person,
                    "Document.BillingReminder.Settlement.CannotCompile",
                    "Settlement document cloud not be compiled when sending bill",
                    "Clould not create settlement document for bill(s) {0} using template {1}",
                    t => ComputeBillText(translator, billing.Bills),
                    t => latexTemplate.Label.Value);

                var texDocument = new TextAttachement(settlement.TexDocument, "document.tex");
                var errorDocument = new TextAttachement(settlement.ErrorText, "error.txt");
                Global.Mail.SendAdminEncrypted(
                    "LaTeX Error", "Could not compile bill settlement document",
                    texDocument, errorDocument);
            }

            return document;
        }

        private static bool SendReminderMail(IDatabase database, Translator translator, Billing billing, BillSendingTemplate template)
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

            var settlementDocument = CreateSettlement(database, translator, billing);

            if (settlementDocument != null)
            {
                string settlementDocumentName = GetSettlementDocumentName(translator);

                content.AddDocument(new PdfAttachement(settlementDocument, settlementDocumentName));

                foreach (var bill in billing.Bills)
                {
                    content.AddDocument(new PdfAttachement(bill.DocumentData, bill.Number.Value));
                }
            }
            else
            {
                return false;
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

        private static string GetSettlementDocumentName(Translator translator)
        {
            return translator.Get(
                "Document.BillingReminder.Settlement.DocumentName",
                "Name of the settlement document when sending bill",
                "Settlement");
        }

        private static bool SentSettlementMail(IDatabase database, Billing billing)
        {
            var membership = billing.Person.Memberships.First(m => m.Organization.Value == billing.Organization);
            var mailSender = membership.Type.Value.SenderGroup.Value;

            if (string.IsNullOrEmpty(billing.Person.PrimaryMailAddress))
            {
                Journal(database, billing.Person,
                    "BillingReminderTask.Settlement.NoMailAddress",
                    "When no mail address is available in settlement",
                    "No mail address available to send settlement");
                return false;
            }

            var message = CreateSettlementMail(database, billing, membership);

            if (message == null)
                return false;

            try
            {
                Global.MailCounter.Used();
                Global.Mail.Send(message);

                Journal(database, membership.Person.Value,
                    "BillingReminderTask.Settlement.SentMail",
                    "Successfully sent settlement mail",
                    "Sent settlement by e-mail to {0}",
                    t => billing.Person.PrimaryMailAddress);

                return true;
            }
            catch (Exception exception)
            {
                Journal(database, membership.Person.Value,
                    "BillingReminderTask.Settlement.MailFailed",
                    "Failed to sent settlment mail",
                    "Sending settlement e-mail to {0} failed",
                    t => billing.Person.PrimaryMailAddress);
                Global.Log.Error(exception.ToString());
                return false;
            }
        }

        public static MimeMessage CreateSettlementMail(IDatabase database, Billing billing, Membership membership)
        {
            var person = membership.Person.Value;
            var group = membership.Type.Value.SenderGroup.Value;
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
            var recipientKey = person.GetPublicKey();
            var translation = new Translation(database);
            var translator = new Translator(translation, person.Language.Value);
            var templator = new Templator(new PersonContentProvider(translator, person));
            var mailTemplate = membership.Type.Value.GetSettlementMail(database, person.Language.Value);
            var htmlText = templator.Apply(mailTemplate.HtmlText.Value);
            var plainText = templator.Apply(mailTemplate.PlainText.Value);
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainText };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            var htmlPart = new TextPart("html") { Text = htmlText };
            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(htmlPart);

            var content = new Multipart("mixed");
            content.Add(alternative);

            var settlementDocument = CreateSettlement(database, translator, billing);

            if (settlementDocument != null)
            {
                content.AddDocument(new PdfAttachement(settlementDocument, GetSettlementDocumentName(translator)));
            }
            else
            {
                return null; 
            }

            foreach (var bill in billing.Bills)
            {
                content.AddDocument(new PdfAttachement(bill.DocumentData, bill.Number.Value));
            }

            return Global.Mail.Create(from, to, senderKey, recipientKey, mailTemplate.Subject.Value, content);
        }
    }

    public class Billing
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

        public bool HasLevelZero
        {
            get
            {
                return Bills.Any(b => b.ReminderLevel.Value < 1);
            }
        }

        public Billing SelectLevelZero()
        {
            var newBilling = new Billing(Organization, Person);
            newBilling.Bills.AddRange(Bills.Where(b => b.ReminderLevel.Value < 1));
            return newBilling;
        }
    }
}

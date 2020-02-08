using System;
using System.Linq;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;
using MimeKit;

namespace Quaestur
{
    public class BillingTask : ITask
    {
        private DateTime _lastSending;
        private int _maxMailsCount;

        public BillingTask()
        {
            _lastSending = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                _maxMailsCount = 500;
                Global.Log.Notice("Running billing task");

                RunAll(database);

                Global.Log.Notice("Billing task complete");
            }
        }

        private void Journal(IDatabase db, Membership membership, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, membership.Person.Value.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.Billing.Process", "Billing process naming", "Billing process");
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
            var memberships = database.Query<Membership>().ToList();

            foreach (var membership in memberships
                .Where(m => m.Type.Value.Collection.Value == CollectionModel.Direct &&
                            !m.Person.Value.Deleted.Value))
            {
                var translator = new Translator(translation, membership.Person.Value.Language.Value);
                var model = membership.Type.Value.CreatePaymentModel(database);
                var advancePeriod = model != null ? model.GetBillAdvancePeriod() : 30;

                var bills = database.Query<Bill>(DC.Equal("membershipid", membership.Id.Value)).ToList();

                if (bills.Count > 0)
                {
                    if (DateTime.Now.Date >= bills.Max(b => b.UntilDate.Value).AddDays(-advancePeriod).Date)
                    {
                        if (CreateBill(database, translation, membership))
                        {
                            _maxMailsCount--;
                        }
                    }
                }
                else
                {
                    if (CreateBill(database, translation, membership))
                    {
                        _maxMailsCount--;
                    }
                }

                if (_maxMailsCount < 1)
                {
                    break;
                }
            }
        }

        private bool CreateBill(IDatabase database, Translation translation, Membership membership)
        {
            Translator translator = new Translator(translation, membership.Person.Value.Language.Value);
            var billDocument = new BillDocument(translator, database, membership);

            if (billDocument.Create())
            {
                using (var transaction = database.BeginTransaction())
                {
                    if (billDocument.Prepayment != null)
                    {
                        database.Save(billDocument.Prepayment); 
                    }

                    database.Save(billDocument.Bill);
                    membership.UpdateVotingRight(database);
                    database.Save(membership);
                    Journal(
                        database,
                        membership,
                        "Document.Bill.Created",
                        "Bill created message",
                        "Created bill {0} for {1} in {2}",
                        t => billDocument.Bill.Number.Value,
                        t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                        t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);

                    transaction.Commit();
                }

                if (billDocument.Prepayment != null)
                {
                    SentSettlementMail(database, billDocument.Bill, membership);
                }

                return true;
            }
            else if (billDocument.RequiresPersonalPaymentUpdate)
            {
                Journal(
                    database,
                    membership,
                    "Document.Bill.RequiresPersonalPaymentUpdate",
                    "Cannot create bill because personal payment parameter update required",
                    "Cannot create bill {0} for {1} in {2} because an update of the personal payment parameter is required",
                    t => billDocument.Bill.Number.Value,
                    t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                    t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);
                return false;
            }
            else if (billDocument.RequiresNewPointsTally)
            {
                Journal(
                    database,
                    membership,
                    "Document.Bill.RequiresNewPointsTally",
                    "Cannot create bill because new points tally required",
                    "Cannot create bill {0} for {1} in {2} because a new points tally is required",
                    t => billDocument.Bill.Number.Value,
                    t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                    t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);
                return false;
            }
            else
            {
                Journal(
                    database,
                    membership,
                    "Document.Bill.Failed",
                    "Bill creation failed message",
                    "Creation of bill {0} for {1} in {2} failed",
                    t => billDocument.Bill.Number.Value,
                    t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                    t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);
                Global.Log.Error(billDocument.ErrorText);
                return false;
            }
        }

        private bool SentSettlementMail(IDatabase database, Bill bill, Membership membership)
        {
            var person = membership.Person.Value;
            var mailSender = membership.Type.Value.SenderGroup.Value;

            if (string.IsNullOrEmpty(person.PrimaryMailAddress))
            {
                Journal(database, membership,
                    "Document.Settlement.NoMailAddress",
                    "When no mail address is available in settlement",
                    "No mail address available to send settlement {0}",
                    t => bill.Number.Value);
                return false;
            }

            var message = CreateSettlementMail(database, membership, bill);

            try
            {
                Global.MailCounter.Used();
                Global.Mail.Send(message);

                Journal(database, membership,
                    "Document.Settlement.SentMail",
                    "Successfully sent settlement mail",
                    "Sent settlement {0} by e-mail to {1}",
                    t => bill.Number.Value,
                    t => person.PrimaryMailAddress);

                return true;
            }
            catch (Exception exception)
            {
                Journal(database, membership,
                    "Document.Settlement.MailFailed",
                    "Failed to sent settlment mail",
                    "Sending settlement e-mail to {0} failed",
                    t => person.PrimaryMailAddress);
                Global.Log.Error(exception.ToString());
                return false;
            }
        }

        public static MimeMessage CreateSettlementMail(IDatabase database, Membership membership, Bill bill)
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

            if (bill != null)
            {
                var content = new Multipart("mixed");
                content.Add(alternative);
                var documentStream = new System.IO.MemoryStream(bill.DocumentData);
                var documentPart = new MimePart("application", "pdf");
                documentPart.Content = new MimeContent(documentStream, ContentEncoding.Binary);
                documentPart.ContentType.Name = bill.Number.Value + ".pdf";
                documentPart.ContentDisposition = new ContentDisposition(ContentDisposition.Attachment);
                documentPart.ContentDisposition.FileName = bill.Number.Value + ".pdf";
                documentPart.ContentTransferEncoding = ContentEncoding.Base64;
                content.Add(documentPart);
                return Global.Mail.Create(from, to, senderKey, recipientKey, mailTemplate.Subject.Value, content);
            }
            else
            {
                return Global.Mail.Create(from, to, senderKey, recipientKey, mailTemplate.Subject.Value, alternative);
            }
        }
    }
}

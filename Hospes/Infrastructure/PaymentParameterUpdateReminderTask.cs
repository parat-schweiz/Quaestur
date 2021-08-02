using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Hospes
{
    public class PaymentParameterUpdateReminderTask : ITask
    {
        private DateTime _lastSending;

        public PaymentParameterUpdateReminderTask()
        {
            _lastSending = DateTime.MinValue;
        }

        public void UpdateRequired()
        { 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                Global.Log.Notice("Running parameter update reminder task");

                foreach (var person in database.Query<Person>())
                {
                    if (Global.MailCounter.Available &&
                        !person.Deleted.Value &&
                        person.Memberships.Any(m => m.Type.Value.Payment.Value != PaymentModel.None && m.Type.Value.CreatePaymentModel(database).InviteForParameterUpdate(m)) &&
                        (!person.PaymentParameterUpdateReminderDate.Value.HasValue ||
                        DateTime.Now.Subtract(person.PaymentParameterUpdateReminderDate.Value.Value).TotalDays >= 7d))
                    {
                        Send(database, person, false);
                    }
                }

                Global.Log.Notice("Parameter update reminder task complete");
            }
        }

        private static void Journal(IDatabase db, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, person.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.PaymentParameterUpdateReminder.Process", "Billing process naming", "Parameter update reminder process");
            entry.Person.Value = person;
            db.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        public static void Send(IDatabase database, Person person, bool forceSend)
        {
            var parametersRequested = new List<string>();

            foreach (var membership in person.Memberships
                .Where(m => m.Type.Value.Payment.Value != PaymentModel.None)
                .OrderByDescending(m => m.Organization.Value.Subordinates.Count()))
            {
                var model = membership.Type.Value.CreatePaymentModel(database);
                var requestParamemterUpdate = model.InviteForParameterUpdate(membership);

                if ((requestParamemterUpdate || forceSend) &&
                    model.PersonalParameterTypes.Any(p => !parametersRequested.Contains(p.Key)))
                {
                    parametersRequested.AddRange(model.PersonalParameterTypes.Select(p => p.Key));

                    if (SendMail(database, membership, membership.Type.Value.CreatePaymentModel(database).RequireParameterUpdate(membership)))
                    {
                        person.PaymentParameterUpdateReminderDate.Value = DateTime.UtcNow;

                        if (person.PaymentParameterUpdateReminderLevel.Value.HasValue)
                        {
                            person.PaymentParameterUpdateReminderLevel.Value = person.PaymentParameterUpdateReminderLevel.Value.Value + 1;
                        }
                        else
                        {
                            person.PaymentParameterUpdateReminderLevel.Value = 1;
                        }

                        database.Save(person);
                    }
                    else
                    {
                        person.PaymentParameterUpdateReminderDate.Value = DateTime.UtcNow.AddDays(-6);
                        database.Save(person);
                    }
                }
            }
        }

        private static bool SendMail(IDatabase database, Membership membership, bool requireUpdate)
        {
            var person = membership.Person.Value;
            var senderGroup = membership.Type.Value.SenderGroup.Value;
            var template = requireUpdate ?
                membership.Type.Value.GetPaymentParameterUpdateRequiredMail(database, person.Language.Value) :
                membership.Type.Value.GetPaymentParameterUpdateInvitationMail(database, person.Language.Value);

            if (senderGroup == null)
            {
                Global.Log.Notice("Missing sender group at payment parameter update for {0} membership {1}.", person.FullName, membership.Organization.Value.ToString());
                return false;
            }

            if (template == null)
            {
                Global.Log.Notice("Missing template at payment parameter update for {0} membership {1}.", person.FullName, membership.Organization.Value.ToString());
                return false;
            }

            if (string.IsNullOrEmpty(person.PrimaryMailAddress))
            {
                Journal(database, person,
                    "Document.PaymentParameterUpdateReminder.NoMailAddress",
                    "When no mail address is available in parameter update reminding",
                    "No mail address available to send parameter update reminder");
                return false;
            }

            var from = new MailboxAddress(
                senderGroup.MailName.Value[person.Language.Value],
                senderGroup.MailAddress.Value[person.Language.Value]);
            var to = new MailboxAddress(
                person.ShortHand,
                person.PrimaryMailAddress);
            var senderKey = string.IsNullOrEmpty(senderGroup.GpgKeyId.Value) ? null :
                new GpgPrivateKeyInfo(
                senderGroup.GpgKeyId.Value,
                senderGroup.GpgKeyPassphrase.Value);
            var recipientKey = person.GetPublicKey();
            var translation = new Translation(database);
            var translator = new Translator(translation, person.Language.Value);
            var templator = new Templator(new PersonContentProvider(translator, person));
            var htmlText = templator.Apply(template.HtmlText);
            var plainText = templator.Apply(template.PlainText);
            var alternative = new Multipart("alternative");
            var plainPart = new TextPart("plain") { Text = plainText };
            plainPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(plainPart);
            var htmlPart = new TextPart("html") { Text = htmlText };
            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
            alternative.Add(htmlPart);

            try
            {
                Global.MailCounter.Used();
                Global.Mail.Send(from, to, senderKey, recipientKey, template.Subject, alternative);
                Journal(database, person,
                    "Document.PaymentParameterUpdateReminder.SentInitialMail",
                    "Successfully sent payment parameter update reminder",
                    "Payment parameter update reminder sent to {0}",
                    t => person.PrimaryMailAddress);
                return true;
            }
            catch (Exception exception)
            {
                Journal(database, person,
                    "Document.PaymentParameterUpdateReminder.MailFailed",
                    "Sending payment parameter update reminder failed",
                    "Failed to send payment parameter update reminder to {0}",
                    t => person.PrimaryMailAddress);
                Global.Log.Error(exception.ToString());
                return false;
            }
        }
    }
}

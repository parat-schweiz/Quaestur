using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PaymentParameterUpdateTask : ITask
    {
        private DateTime _lastSending;

        public PaymentParameterUpdateTask()
        {
            _lastSending = DateTime.MinValue;
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                Global.Log.Info("Running parameter update reminder task");
                var currentTasks = database
                    .Query<MembershipTask>(DC.Equal("type", (int)MembershipTaskType.PaymentParameterUpdate))
                    .ToList();

                foreach (var membership in database.Query<Membership>())
                {
                    if ((!currentTasks.Any(t => t.Membership.Value == membership)))
                    {
                        var task = new PaymentParameterUpdateReminderTask(database, membership);
                        if (string.IsNullOrEmpty(task.Validate()))
                        {
                            var membershipTask = new MembershipTask(Guid.NewGuid());
                            membershipTask.Membership.Value = membership;
                            membershipTask.Type.Value = MembershipTaskType.PaymentParameterUpdate;
                            membershipTask.Status.Value = MembershipTaskStatus.New;
                            membershipTask.Created.Value = DateTime.UtcNow;
                            membershipTask.Modifed.Value = membershipTask.Created.Value;
                            membershipTask.Message.Value = string.Empty;
                            membershipTask.Error.Value = string.Empty;
                            database.Save(membershipTask);
                        }
                    }
                }

                Global.Log.Info("Parameter update reminder task complete");
            }
        }
    }

    public class PaymentParameterUpdateReminderTask : IMembershipTask
    {
        private readonly IDatabase _database;
        private readonly Membership _membership;

        public PaymentParameterUpdateReminderTask(IDatabase database, Membership membership)
        {
            _database = database;
            _membership = membership;
        }

        public string Validate()
        {
            var translator = new Translator(new Translation(_database), _membership.Person.Value.Language.Value);

            if (_membership.Person.Value.Deleted.Value)
            {
                return translator.Get(
                    "PaymentParameterUpdateReminderTask.Invalid.Reason.Deleted",
                    "Reason person deleted in the payment parameter update reminder task",
                    "Person was marked deleted.");
            }
            else if (_membership.Type.Value.Payment.Value != PaymentModel.None)
            {
                return translator.Get(
                    "PaymentParameterUpdateReminderTask.Invalid.Reason.NoPaymentModel",
                    "Reason no payment model deleted in the payment parameter update reminder task",
                    "No payment model selected for membership.");
            }
            else if (!_membership.Type.Value.CreatePaymentModel(_database).InviteForParameterUpdate(_membership))
            {
                return translator.Get(
                    "PaymentParameterUpdateReminderTask.Invalid.Reason.NoUpdateRequired",
                    "Reason no update required in the payment parameter update reminder task",
                    "Current payment model does not require a parameter update.");
            }
            else if (_membership.Person.Value.PaymentParameterUpdateReminderDate.Value.HasValue &&
                     DateTime.Now.Subtract(_membership.Person.Value.PaymentParameterUpdateReminderDate.Value.Value).TotalDays < 7d)
            {
                return translator.Get(
                    "PaymentParameterUpdateReminderTask.Invalid.Reason.RecentlyUpdated",
                    "Reason recently update in the payment parameter update reminder task",
                    "Payment parameters were recently updated.");
            }
            else
            {
                return null;
            }
        }

        public void Execute()
        {
            Send(_database, _membership);
        }

        public string Title(Translator translator)
        {
            return translator.Get(
                "PaymentParameterUpdateReminderTask.Title",
                "Title in the payment parameter update reminder task",
                "Reminder for parameter update");
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

        public static void Send(IDatabase database, Membership membership)
        {
            var model = membership.Type.Value.CreatePaymentModel(database);
            var requestParamemterUpdate = model.InviteForParameterUpdate(membership);

            if (SendMail(database, membership, membership.Type.Value.CreatePaymentModel(database).RequireParameterUpdate(membership)))
            {
                membership.Person.Value.PaymentParameterUpdateReminderDate.Value = DateTime.UtcNow;

                if (membership.Person.Value.PaymentParameterUpdateReminderLevel.Value.HasValue)
                {
                    membership.Person.Value.PaymentParameterUpdateReminderLevel.Value = membership.Person.Value.PaymentParameterUpdateReminderLevel.Value.Value + 1;
                }
                else
                {
                    membership.Person.Value.PaymentParameterUpdateReminderLevel.Value = 1;
                }

                database.Save(membership.Person.Value);
            }
            else
            {
                membership.Person.Value.PaymentParameterUpdateReminderDate.Value = DateTime.UtcNow.AddDays(-6);
                database.Save(membership.Person.Value);
            }
        }

        private static bool SendMail(IDatabase database, Membership membership, bool requireUpdate)
        {
            var person = membership.Person.Value;
            var senderGroup = membership.Type.Value.SenderGroup.Value;
            var template = requireUpdate ?
                membership.Type.Value.PaymentParameterUpdateRequiredMails.Value(database, person.Language.Value) :
                membership.Type.Value.PaymentParameterUpdateInvitationMails.Value(database, person.Language.Value);

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
            var templator = new Templator(new PersonContentProvider(database, translator, person));
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

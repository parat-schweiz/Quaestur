using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class MailingTask : ITask
    {
        private DateTime _lastSending;

        public MailingTask()
        {
            _lastSending = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(2))
            {
                _lastSending = DateTime.UtcNow;
                Global.Log.Info("Running mailing task");

                foreach (var mailing in database.Query<Mailing>())
                {
                    if (mailing.Status.Value == MailingStatus.Scheduled &&
                        DateTime.UtcNow >= mailing.SendingDate.Value)
                    {
                        RunSend(database, mailing);
                    }
                    else if (mailing.Status.Value == MailingStatus.Sending)
                    {
                        RunSending(database, mailing);
                    }
                }

                Global.Log.Info("Mailing task complete");
            }
        }

        public static IEnumerable<ServiceAddress> Targets(IDatabase database, Mailing mailing)
        {
            return database
                .Query<Membership>(DC.Equal("organizationid", mailing.RecipientOrganization.Value.Id.Value))
                .Where(m => m.IsActive)
                .Select(m => m.Person.Value)
                .Where(p => !p.Deleted.Value)
                .Where(p => (mailing.RecipientTag.Value == null) ||
                            (database.Query<TagAssignment>(DC.Equal("personid", p.Id.Value).And(DC.Equal("tagid", mailing.RecipientTag.Value.Id.Value)))).Any())
                .Where(p => (mailing.RecipientLanguage.Value == null) ||
                            (p.Language.Value == mailing.RecipientLanguage.Value))
                .Select(a => a.PrimaryAddress(ServiceType.EMail))
                .Where(a => a != null)
                .ToList();
        }

        private void RunSend(IDatabase database, Mailing mailing)
        {
            int count = 0;

            foreach (var address in Targets(database, mailing))
            {
                var sending = new Sending(Guid.NewGuid());
                sending.Mailing.Value = mailing;
                sending.Address.Value = address;
                sending.Status.Value = SendingStatus.Created;
                database.Save(sending);
                count++;
            }

            mailing.Status.Value = MailingStatus.Sending;
            database.Save(mailing);
            Global.Log.Info("Mailing {0} is now sending {1} mails", mailing.Title, count);
        }

        private void Journal(IDatabase database, Mailing mailing, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(database);
            var translator = new Translator(translation, person.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = mailing.Creator.Value.ShortHand;
            entry.Person.Value = person;
            database.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        private void RunSending(IDatabase database, Mailing mailing)
        {
            int remainingCount = 0;

            foreach (var sending in database.Query<Sending>(DC.Equal("mailingid", mailing.Id.Value)))
            {
                if (sending.Status.Value == SendingStatus.Created)
                {
                    if (Global.MailCounter.Available)
                    {
                        var header = mailing.Header.Value;
                        var footer = mailing.Footer.Value;
                        var htmlText = mailing.HtmlText.Value;
                        var plainText = mailing.PlainText.Value;

                        if (header != null)
                        {
                            htmlText = HtmlText.ConcatHtml(header.HtmlText.Value, htmlText);
                            plainText = header.PlainText.Value + plainText;
                        }

                        if (footer != null)
                        {
                            htmlText = HtmlText.ConcatHtml(htmlText, footer.HtmlText.Value);
                            plainText = plainText + footer.PlainText.Value;
                        }

                        var translation = new Translation(database);
                        var translator = new Translator(translation, sending.Address.Value.Person.Value.Language.Value);
                        var templator = new Templator(new PersonContentProvider(database, translator, sending.Address.Value.Person.Value));
                        htmlText = templator.Apply(htmlText);
                        plainText = templator.Apply(plainText);

                        try
                        {
                            var language = sending.Address.Value.Person.Value.Language.Value;
                            var from = new MailboxAddress(
                                mailing.Sender.Value.MailName.Value[language],
                                mailing.Sender.Value.MailAddress.Value[language]);
                            var to = new MailboxAddress(
                                sending.Address.Value.Person.Value.ShortHand, 
                                sending.Address.Value.Address.Value);
                            var senderKey = string.IsNullOrEmpty(mailing.Sender.Value.GpgKeyId.Value) ? null :
                                new GpgPrivateKeyInfo(
                                mailing.Sender.Value.GpgKeyId.Value,
                                mailing.Sender.Value.GpgKeyPassphrase.Value);
                            var content = new Multipart("alternative");
                            var textPart = new TextPart("plain") { Text = plainText };
                            textPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                            content.Add(textPart);
                            var htmlPart = new TextPart("html") { Text = htmlText };
                            htmlPart.ContentTransferEncoding = ContentEncoding.QuotedPrintable;
                            content.Add(htmlPart);

                            Global.MailCounter.Used();
                            Global.Mail.Send(from, to, senderKey, null, mailing.Subject.Value, content);
                            sending.Status.Value = SendingStatus.Sent;
                            sending.SentDate.Value = DateTime.UtcNow;
                            database.Save(sending);
                            Journal(database, mailing, sending.Address.Value.Person.Value,
                                "MailTask.Journal.Sent",
                                "Journal entry sent mail",
                                "Task sent mail {0}",
                                t => mailing.Title.Value);
                        }
                        catch (Exception exception)
                        {
                            sending.Status.Value = SendingStatus.Failed;
                            sending.FailureMessage.Value = exception.Message;
                            sending.SentDate.Value = DateTime.UtcNow;
                            database.Save(sending);
                            Journal(database, mailing, sending.Address.Value.Person.Value,
                                "MailTask.Journal.Failed",
                                "Journal entry sending mail failed",
                                "Task failed sending mail {0}",
                                t => mailing.Title.Value);
                        }
                    }
                    else
                    {
                        remainingCount++;
                    }
                } 
            }

            if (remainingCount < 1)
            {
                mailing.Status.Value = MailingStatus.Sent;
                mailing.SentDate.Value = DateTime.UtcNow;
                database.Save(mailing);
                Global.Log.Info("Mailing {0} has finished sending", mailing.Title);
            }
            else
            {
                Global.Log.Info("Mailing {0} needs to send {1} more mails", mailing.Title, remainingCount);
            }
        }
    }
}

using System;
using System.Collections.Generic;

namespace Publicus
{
    public enum MailingStatus
    {
        New,
        Scheduled,
        Sending,
        Sent,
        Canceled, 
    }

    public static class MailingStatusExtensions
    {
        public static string Translate(this MailingStatus status, Translator translator)
        {
            switch (status)
            {
                case MailingStatus.New:
                    return translator.Get("Enum.MailingStatus.New", "New value in the mailing status enum", "New");
                case MailingStatus.Scheduled:
                    return translator.Get("Enum.MailingStatus.Scheduled", "Scheduled value in the mailing status enum", "Scheduled");
                case MailingStatus.Sending:
                    return translator.Get("Enum.MailingStatus.Sending", "Sending value in the mailing status enum", "Sending");
                case MailingStatus.Sent:
                    return translator.Get("Enum.MailingStatus.Sent", "Sent value in the mailing status enum", "Sent");
                case MailingStatus.Canceled:
                    return translator.Get("Enum.MailingStatus.Canceled", "Canceled value in the mailing status enum", "Canceled");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Mailing : DatabaseObject
    {
        public StringField Title { get; set; }
        public ForeignKeyField<Feed, Mailing> RecipientFeed { get; set; }
        public ForeignKeyField<Tag, Mailing> RecipientTag { get; set; }
        public EnumNullField<Language> RecipientLanguage { get; set; }
        public ForeignKeyField<Group, Mailing> Sender { get; set; }
        public ForeignKeyField<User, Mailing> Creator { get; set; }
        public ForeignKeyField<MailingElement, Mailing> Header { get; set; }
        public ForeignKeyField<MailingElement, Mailing> Footer { get; set; }
        public StringField Subject { get; set; }
        public StringField HtmlText { get; set; }
        public StringField PlainText { get; set; }
        public Field<DateTime> CreatedDate { get; set; }
        public FieldNull<DateTime> SendingDate { get; set; }
        public FieldNull<DateTime> SentDate { get; set; }
        public EnumField<MailingStatus> Status { get; set; }

        public Mailing() : this(Guid.Empty)
        {
        }

        public Mailing(Guid id) : base(id)
        {
            Title = new StringField(this, "title", 256);
            RecipientFeed = new ForeignKeyField<Feed, Mailing>(this, "recipientfeedid", false, null);
            RecipientTag = new ForeignKeyField<Tag, Mailing>(this, "recipienttagid", true, null);
            RecipientLanguage = new EnumNullField<Language>(this, "recipientlanguage", LanguageExtensions.Translate);
            Sender = new ForeignKeyField<Group, Mailing>(this, "senderid", false, null);
            Creator = new ForeignKeyField<User, Mailing>(this, "creatorid", false, null);
            Header = new ForeignKeyField<MailingElement, Mailing>(this, "headerid", true, null);
            Footer = new ForeignKeyField<MailingElement, Mailing>(this, "footerid", true, null);
            Subject = new StringField(this, "subject", 256);
            HtmlText = new StringField(this, "htmltext", 33554432, AllowStringType.SafeHtml);
            PlainText = new StringField(this, "plaintext", 33554432, AllowStringType.SafeHtml);
            CreatedDate = new Field<DateTime>(this, "createddate", DateTime.UtcNow);
            SendingDate = new FieldNull<DateTime>(this, "sendingdate");
            SentDate = new FieldNull<DateTime>(this, "sentdate");
            Status = new EnumField<MailingStatus>(this, "status", MailingStatus.New, MailingStatusExtensions.Translate);
        }

        public override void Delete(IDatabase database)
        {
            foreach (var sending in database.Query<Sending>(DC.Equal("mailingid", Id.Value)))
            {
                sending.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Title.Value;
        }

        public override string GetText(Translator translator)
        {
            return Title.Value;
        }
    }
}

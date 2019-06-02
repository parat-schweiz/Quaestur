using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public enum ArticleType
    {
        Note = 0,
        Mail = 1,
    }

    public static class ArticleTypeExtensions
    {
        public static string Translate(this ArticleType type, Translator translator)
        {
            switch (type)
            {
                case ArticleType.Note:
                    return translator.Get("Enum.ArticleType.Note", "Note value in the article type enum", "Note");
                case ArticleType.Mail:
                    return translator.Get("Enum.ArticleType.Mail", "Mail value in the article type enum", "Mail");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum ArticleStatus
    {
        New = 0,
        Pending = 1,
        Sent = 2,
        Read = 3,
        Failed = 4,
    }

    public static class ArticleStatusExtensions
    {
        public static string Translate(this ArticleStatus status, Translator translator)
        {
            switch (status)
            {
                case ArticleStatus.New:
                    return translator.Get("Enum.ArticleStatus.New", "New value in the article status enum", "New");
                case ArticleStatus.Pending:
                    return translator.Get("Enum.ArticleStatus.Pending", "Pending value in the article status enum", "Pending");
                case ArticleStatus.Sent:
                    return translator.Get("Enum.ArticleStatus.Sent", "Sent value in the article status enum", "Sent");
                case ArticleStatus.Read:
                    return translator.Get("Enum.ArticleStatus.Read", "Read value in the article status enum", "Read");
                case ArticleStatus.Failed:
                    return translator.Get("Enum.ArticleStatus.Failed", "Failed value in the article status enum", "Failed");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    [Flags]
    public enum ArticleSecurity
    {
        None = 0,
        Min = 1,
        Sign = 1,
        Encrypt = 2,
        Verified = 4,
        VerifyFailed = 8,
        NotVerified = 16,
        Decrypted = 32,
        Max = 32,
    }

    public static class ArticleSecurityExtensions
    {
        public static string TranslateValue(this ArticleSecurity security, Translator translator)
        {
            switch (security)
            {
                case ArticleSecurity.None:
                    return translator.Get("Enum.ArticleSecurity.None", "None value in the ticket security enum", "None");
                case ArticleSecurity.Sign:
                    return translator.Get("Enum.ArticleSecurity.Sign", "Sign value in the ticket security enum", "Sign");
                case ArticleSecurity.Encrypt:
                    return translator.Get("Enum.ArticleSecurity.Encrypt", "Encrypt value in the ticket security enum", "Encrypt");
                case ArticleSecurity.Verified:
                    return translator.Get("Enum.ArticleSecurity.Verified", "Verified value in the ticket security enum", "Verified");
                case ArticleSecurity.VerifyFailed:
                    return translator.Get("Enum.ArticleSecurity.VerifyFailed", "Verify failed value in the ticket security enum", "Verify failed");
                case ArticleSecurity.NotVerified:
                    return translator.Get("Enum.ArticleSecurity.NotVerified", "Not verified value in the ticket security enum", "Not verified");
                case ArticleSecurity.Decrypted:
                    return translator.Get("Enum.ArticleSecurity.Decrypted", "Decrypted value in the ticket security enum", "Decrypted");
                default:
                    throw new NotSupportedException();
            }
        }

        public static string Translate(this ArticleSecurity security, Translator translator)
        {
            var values = new List<string>();

            for (ArticleSecurity value = ArticleSecurity.Min; value <= ArticleSecurity.Max; value = ((ArticleSecurity)((int)value * 2)))
            {
                if (security.HasFlag(value))
                {
                    values.Add(TranslateValue(value, translator));
                }
            }

            return string.Join(", ", values);
        }
    }

    public class Article : DatabaseObject
    {
        public ForeignKeyField<Ticket, Article> Ticket { get; private set; }
        public EnumField<ArticleType> Type { get; private set; }
        public EnumField<ArticleStatus> Status { get; private set; }
        public EnumField<ArticleSecurity> Security { get; private set; }
        public FieldNull<DateTime> SentDate { get; private set; }
        public Field<DateTime> CreatedDate { get; private set; }
        public StringField Subject { get; private set; }
        public StringField Text { get; private set; }
        public ByteArrayField Data { get; private set; }
        public ForeignKeyField<User, Article> User { get; private set; }
        public List<Attachement> Attachements { get; private set; }
        public List<Participant> Participants { get; private set; }

        public IEnumerable<Participant> From
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.From); }
        }

        public IEnumerable<Participant> To
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.To); }
        }

        public IEnumerable<Participant> CC
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.CC); }
        }

        public IEnumerable<Participant> BCC
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.BCC); }
        }

        public Article() : this(Guid.Empty)
        {
        }

        public Article(Guid id) : base(id)
        {
            Ticket = new ForeignKeyField<Ticket, Article>(this, "ticketid", false, t => t.Articles);
            Type = new EnumField<ArticleType>(this, "type", ArticleType.Note, ArticleTypeExtensions.Translate);
            Status = new EnumField<ArticleStatus>(this, "status", ArticleStatus.New, ArticleStatusExtensions.Translate);
            Security = new EnumField<ArticleSecurity>(this, "security", ArticleSecurity.None, ArticleSecurityExtensions.Translate);
            SentDate = new FieldNull<DateTime>(this, "sentdate");
            CreatedDate = new Field<DateTime>(this, "createddate", new DateTime(1970, 1, 1));
            User = new ForeignKeyField<User, Article>(this, "user", true, null);
            Text = new StringField(this, "text", 262144, AllowStringType.SafeHtml);
            Subject = new StringField(this, "subject", 1024, AllowStringType.SimpleText);
            Data = new ByteArrayField(this, "data", true);
            Attachements = new List<Attachement>();
            Participants = new List<Participant>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Attachement>("articleid", Id.Value, () => Attachements);
                yield return new MultiCascade<Participant>("articleid", Id.Value, () => Participants);
            }
        }

        public override string ToString()
        {
            return Subject.Value;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var address in database.Query<Attachement>(DC.Equal("articleid", Id.Value)))
            {
                address.Delete(database);
            }

            foreach (var address in database.Query<Participant>(DC.Equal("articleid", Id.Value)))
            {
                address.Delete(database);
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Subject.Value;
        }
    }
}

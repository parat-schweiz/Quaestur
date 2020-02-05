using System;
using System.Collections.Generic;
using System.Linq;
using SiteLibrary;

namespace Quaestur
{
    public enum SendingMode
    { 
        MailOnly = 0,
        PostalOnly = 1,
        MailPreferred = 2,
        PostalPrefrerred = 3,
    }

    public static class SendingModeExtensions
    {
        public static string Translate(this SendingMode mode, Translator translator)
        {
            switch (mode)
            {
                case SendingMode.MailOnly:
                    return translator.Get("Enum.SendingMode.MailOnly", "Value 'Mail only' in BillStatus enum", "Mail only");
                case SendingMode.PostalOnly:
                    return translator.Get("Enum.SendingMode.PostalOnly", "Value 'Postal only' in BillStatus enum", "Postal only");
                case SendingMode.MailPreferred:
                    return translator.Get("Enum.SendingMode.MailPreferred", "Value 'Mail preferred' in BillStatus enum", "Mail preferred");
                case SendingMode.PostalPrefrerred:
                    return translator.Get("Enum.SendingMode.PostalPrefrerred", "Value 'Postal prefrerred' in BillStatus enum", "Postal prefrerred");
                default:
                    throw new NotSupportedException(); 
            }
        } 
    }

    public class BillSendingTemplate : DatabaseObject
    {
        public ForeignKeyField<MembershipType, BillSendingTemplate> MembershipType { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public Field<int> MinReminderLevel { get; private set; }
        public Field<int> MaxReminderLevel { get; private set; }
        public StringField Name { get; private set; }
        public StringField MailSubject { get; private set; }
        public StringField MailHtmlText { get; private set; }
        public StringField MailPlainText { get; private set; }
        public ForeignKeyField<Group, BillSendingTemplate> MailSender { get; private set; }
        public StringField LetterLatex { get; private set; }
        public EnumField<SendingMode> SendingMode { get; private set; }

        public const string BillSendingLetterFieldName = "BillSendingLetters";
        public const string BillSendingMailFieldName = "BillSendingMails";

        public BillSendingTemplate() : this(Guid.Empty)
        {
        }

        public BillSendingTemplate(Guid id) : base(id)
        {
            MembershipType = new ForeignKeyField<MembershipType, BillSendingTemplate>(this, "membershiptypeid", false, null);
            Language = new EnumField<Language>(this, "language", SiteLibrary.Language.English, LanguageExtensions.Translate);
            MinReminderLevel = new Field<int>(this, "minreminderlevel", 1);
            MaxReminderLevel = new Field<int>(this, "maxreminderlevel", 1);
            Name = new StringField(this, "name", 256);
            MailSubject = new StringField(this, "mailsubject", 256);
            MailHtmlText = new StringField(this, "mailhtmltext", 262144, AllowStringType.SafeHtml);
            MailPlainText = new StringField(this, "mailplaintext", 262144, AllowStringType.SafeHtml);
            MailSender = new ForeignKeyField<Group, BillSendingTemplate>(this, "mailsenderid", false, null);
            LetterLatex = new StringField(this, "letterlatex", 262144, AllowStringType.SafeLatex);
            SendingMode = new EnumField<SendingMode>(this, "sendingmode", Quaestur.SendingMode.MailOnly, SendingModeExtensions.Translate);
        }

        public TemplateField BillSendingLetter
        {
            get { return new TemplateField(TemplateAssignmentType.BillSendingTemplate, Id.Value, BillSendingLetterFieldName); }
        }

        public TemplateField BillSendingMail
        {
            get { return new TemplateField(TemplateAssignmentType.BillSendingTemplate, Id.Value, BillSendingMailFieldName); }
        }

        public IEnumerable<LatexTemplateAssignment> BillSendingLetters(IDatabase database)
        {
            return database.Query<LatexTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", BillSendingLetterFieldName)));
        }

        public IEnumerable<MailTemplateAssignment> BillSendingMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", BillSendingMailFieldName)));
        }

        public LatexTemplate GetBillSendingLetter(IDatabase database, Language language)
        {
            var list = BillSendingLetters(database);

            foreach (var l in LanguageExtensions.PreferenceList(language))
            {
                var assignment = list.FirstOrDefault(a => a.Template.Value.Language.Value == l);
                if (assignment != null)
                    return assignment.Template.Value;
            }

            return null;
        }

        public MailTemplate GetBillSendingMail(IDatabase database, Language language)
        {
            var list = BillSendingMails(database);

            foreach (var l in LanguageExtensions.PreferenceList(language))
            {
                var assignment = list.FirstOrDefault(a => a.Template.Value.Language.Value == l);
                if (assignment != null)
                    return assignment.Template.Value;
            }

            return null;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this); 
        }

        public override string ToString()
        {
            return Name.Value;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value;
        }
    }
}

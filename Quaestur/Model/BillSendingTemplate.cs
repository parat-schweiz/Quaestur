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
        public ForeignKeyField<Group, BillSendingTemplate> MailSender { get; private set; }
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
            MailSender = new ForeignKeyField<Group, BillSendingTemplate>(this, "mailsenderid", false, null);
            SendingMode = new EnumField<SendingMode>(this, "sendingmode", Quaestur.SendingMode.MailOnly, SendingModeExtensions.Translate);
        }

        public LatexTemplateAssignmentField BillSendingLetters
        {
            get { return new LatexTemplateAssignmentField(TemplateAssignmentType.BillSendingTemplate, Id.Value, BillSendingLetterFieldName); }
        }

        public MailTemplateAssignmentField BillSendingMails
        {
            get { return new MailTemplateAssignmentField(TemplateAssignmentType.BillSendingTemplate, Id.Value, BillSendingMailFieldName); }
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

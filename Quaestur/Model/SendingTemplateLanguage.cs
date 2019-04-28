using System;
using System.Collections.Generic;

namespace Quaestur
{
    public class SendingTemplateLanguage : DatabaseObject
    {
        public ForeignKeyField<SendingTemplate, SendingTemplateLanguage> Template { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public StringField MailSubject { get; private set; }
        public StringField MailHtmlText { get; private set; }
        public StringField MailPlainText { get; private set; }
        public StringField LetterLatex { get; private set; }

        public SendingTemplateLanguage() : this(Guid.Empty)
        {
        }

        public SendingTemplateLanguage(Guid id) : base(id)
        {
            Template = new ForeignKeyField<SendingTemplate, SendingTemplateLanguage>(this, "templateid", false, st => st.Languages);
            Language = new EnumField<Language>(this, "language", Quaestur.Language.English, LanguageExtensions.Translate);
            MailSubject = new StringField(this, "mailsubject", 256, AllowStringType.SimpleText);
            MailHtmlText = new StringField(this, "mailhtmltext", 262144, AllowStringType.SafeHtml);
            MailPlainText = new StringField(this, "mailplaintext", 262144, AllowStringType.SafeLatex);
            LetterLatex = new StringField(this, "letterlatex", 262144, AllowStringType.SimpleText);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return ToString();
        }

        public bool HasAccess(IDatabase database, Session session, AccessRight right)
        {
            return Template.Value.HasAccess(database, session, right); 
        }
    }
}

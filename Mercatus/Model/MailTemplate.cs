using System;
using System.Collections.Generic;
using System.Linq;
using SiteLibrary;

namespace Publicus
{
    public class MailTemplate : DatabaseObject
    {
        public ForeignKeyField<Feed, MailTemplate> Feed { get; private set; }
        public EnumField<TemplateAssignmentType> AssignmentType { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public StringField Label { get; private set; }
        public StringField Subject { get; private set; }
        public StringField HtmlText { get; private set; }
        public StringField PlainText { get; private set; }  

        public MailTemplate() : this(Guid.Empty)
        {
        }

        public MailTemplate(Guid id) : base(id)
        {
            Feed = new ForeignKeyField<Feed, MailTemplate>(this, "feedid", false, null);
            AssignmentType = new EnumField<TemplateAssignmentType>(this, "assignmenttype", TemplateAssignmentType.Petition, TemplateAssignmentTypeExtensions.Translate);
            Language = new EnumField<Language>(this, "language", SiteLibrary.Language.English, LanguageExtensions.Translate);
            Label = new StringField(this, "label", 256, AllowStringType.SimpleText);
            Subject = new StringField(this, "subject", 256, AllowStringType.SimpleText);
            HtmlText = new StringField(this, "htmltext", 262144, AllowStringType.SafeHtml);
            PlainText = new StringField(this, "laintext", 262144, AllowStringType.SafeLatex);
        }

        public override string ToString()
        {
            return Label.Value;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var assignment in database.Query<MailTemplateAssignment>(DC.Equal("templateid", Id.Value)))
            {
                assignment.Delete(database);
            }

            database.Delete(this);
        }

        public IEnumerable<MailTemplateAssignment> Assignments(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("templateid", Id.Value));
        }

        public override string GetText(Translator translator)
        {
            return Label.Value;
        }
    }
}

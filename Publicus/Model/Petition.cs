using System;
using System.Collections.Generic;
using System.Linq;
using SiteLibrary;

namespace Publicus
{
    public class Petition : DatabaseObject
    {
        public ForeignKeyField<Feed, Petition> Feed { get; set; }
        public MultiLanguageStringField Title { get; set; }
        public MultiLanguageStringField Text { get; set; }

        public Petition() : this(Guid.Empty)
        {
        }

        public Petition(Guid id) : base(id)
        {
            Feed = new ForeignKeyField<Feed, Petition>(this, "feedid", false, null);
            Title = new MultiLanguageStringField(this, "title", AllowStringType.SimpleText);
            Text = new MultiLanguageStringField(this, "text", AllowStringType.SafeHtml);
        }

        public override string ToString()
        {
            return Title.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Title.Value[translator.Language];
        }

        public static string GetFieldNameTranslation(Translator translator, string fieldName)
        {
            switch (fieldName)
            {
                case ConfirmationMailFieldName:
                    return translator.Get("Peition.FieldName.ConfirmationMail", "Confirmation mail field name of the ballot template", "Confirmation mail");
                default:
                    throw new NotSupportedException();
            }
        }
        public const string ConfirmationMailFieldName = "ConfirmationMails";

        public TemplateField ConfirmationMail
        {
            get
            {
                return new TemplateField(TemplateAssignmentType.Petition, Id.Value, ConfirmationMailFieldName);
            }
        }

        public IEnumerable<MailTemplateAssignment> ConfirmationMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", ConfirmationMailFieldName)));
        }

        public MailTemplate GetConfirmationMail(IDatabase database, Language language)
        {
            var list = ConfirmationMails(database);

            foreach (var l in LanguageExtensions.PreferenceList(language))
            {
                var assignment = list.FirstOrDefault(a => a.Template.Value.Language.Value == l);
                if (assignment != null)
                    return assignment.Template.Value;
            }

            return null;
        }
    }
}

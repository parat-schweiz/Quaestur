using System;
using System.Collections.Generic;
using System.Linq;
using SiteLibrary;

namespace Publicus
{
    public class Petition : DatabaseObject
    {
        public ForeignKeyField<Group, Petition> Group { get; private set; }
        public MultiLanguageStringField Label { get; private set; }
        public MultiLanguageStringField Text { get; private set; }
        public MultiLanguageStringField WebAddress { get; private set; }
        public MultiLanguageStringField ShareText { get; private set; }
        public ForeignKeyField<Tag, Petition> PetitionTag { get; private set; }
        public ForeignKeyField<Tag, Petition> SpecialNewsletterTag { get; private set; }
        public ForeignKeyField<Tag, Petition> GeneralNewsletterTag { get; private set; }
        public ForeignKeyField<Tag, Petition> ShowPubliclyTag { get; private set; }
        public ByteArrayField EmailKey { get; private set; }

        public Petition() : this(Guid.Empty)
        {
        }

        public Petition(Guid id) : base(id)
        {
            Group = new ForeignKeyField<Group, Petition>(this, "groupid", false, null);
            Label = new MultiLanguageStringField(this, "label", AllowStringType.SimpleText);
            Text = new MultiLanguageStringField(this, "text", AllowStringType.SafeHtml);
            WebAddress = new MultiLanguageStringField(this, "webaddress", AllowStringType.SimpleText);
            ShareText = new MultiLanguageStringField(this, "sharetext", AllowStringType.SimpleText);
            PetitionTag = new ForeignKeyField<Tag, Petition>(this, "petitiontagid", false, null);
            SpecialNewsletterTag = new ForeignKeyField<Tag, Petition>(this, "specialnewslettertagid", true, null);
            GeneralNewsletterTag = new ForeignKeyField<Tag, Petition>(this, "generalnewslettertagid", true, null);
            ShowPubliclyTag = new ForeignKeyField<Tag, Petition>(this, "showpubliclytag", true, null);
            EmailKey = new ByteArrayField(this, "emailkey", false);
        }

        public override string ToString()
        {
            return Label.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Label.Value[translator.Language];
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

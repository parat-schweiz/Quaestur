using System;
using System.Collections.Generic;
using System.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class LatexTemplate : DatabaseObject
    {
        public ForeignKeyField<Organization, LatexTemplate> Organization { get; private set; }
        public EnumNullField<TemplateAssignmentType> AssignmentType { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public StringField Label { get; private set; }
        public StringField Text { get; private set; }

        public LatexTemplate() : this(Guid.Empty)
        {
        }

        public LatexTemplate(Guid id) : base(id)
        {
            Organization = new ForeignKeyField<Organization, LatexTemplate>(this, "organizationid", true, null);
            AssignmentType = new EnumNullField<TemplateAssignmentType>(this, "assignmenttype", TemplateAssignmentTypeExtensions.Translate);
            Language = new EnumField<Language>(this, "language", SiteLibrary.Language.English, LanguageExtensions.Translate);
            Label = new StringField(this, "label", 256, AllowStringType.SimpleText);
            Text = new StringField(this, "text", 262144, AllowStringType.SafeLatex);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Delete(IDatabase database)
        {
            foreach (var assignment in database.Query<LatexTemplateAssignment>(DC.Equal("templateid", Id.Value)))
            {
                assignment.Delete(database);
            }

            database.Delete(this);
        }

        public IEnumerable<LatexTemplateAssignment> Assignments(IDatabase database)
        {
            return database.Query<LatexTemplateAssignment>(DC.Equal("templateid", Id.Value));
        }

        public override string GetText(Translator translator)
        {
            return Label.Value;
        }
    }
}

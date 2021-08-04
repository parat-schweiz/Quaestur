using System;
using System.Collections.Generic;
using System.Linq;
using SiteLibrary;

namespace Publicus
{
    public class LatexTemplate : DatabaseObject
    {
        public ForeignKeyField<Feed, LatexTemplate> Feed { get; private set; }
        public EnumField<TemplateAssignmentType> AssignmentType { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public StringField Label { get; private set; }
        public StringField Text { get; private set; }

        public LatexTemplate() : this(Guid.Empty)
        {
        }

        public LatexTemplate(Guid id) : base(id)
        {
            Feed = new ForeignKeyField<Feed, LatexTemplate>(this, "feedid", false, null);
            AssignmentType = new EnumField<TemplateAssignmentType>(this, "assignmenttype", TemplateAssignmentType.Petition, TemplateAssignmentTypeExtensions.Translate);
            Language = new EnumField<Language>(this, "language", SiteLibrary.Language.English, LanguageExtensions.Translate);
            Label = new StringField(this, "label", 256, AllowStringType.SimpleText);
            Text = new StringField(this, "text", 262144, AllowStringType.SafeLatex);
        }

        public override string ToString()
        {
            return Label.Value;
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

using System;
using System.Collections.Generic;
using System.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class PageTemplate : DatabaseObject, ITemplate
    {
        public ForeignKeyField<Organization, PageTemplate> Organization { get; private set; }
        public EnumField<TemplateAssignmentType> AssignmentType { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public StringField Label { get; private set; }
        public StringField Title { get; private set; }
        public StringField HtmlText { get; private set; }

        Organization ITemplate.Organization => Organization.Value;

        TemplateAssignmentType ITemplate.AssignmentType => AssignmentType.Value;

        Language ITemplate.Language => Language.Value;

        string ITemplate.Label => Label.Value;

        Guid ITemplate.Id => Id.Value;

        public PageTemplate() : this(Guid.Empty)
        {
        }

        public PageTemplate(Guid id) : base(id)
        {
            Organization = new ForeignKeyField<Organization, PageTemplate>(this, "organizationid", false, null);
            AssignmentType = new EnumField<TemplateAssignmentType>(this, "assignmenttype", TemplateAssignmentType.BallotTemplate, TemplateAssignmentTypeExtensions.Translate);
            Language = new EnumField<Language>(this, "language", SiteLibrary.Language.English, LanguageExtensions.Translate);
            Label = new StringField(this, "label", 256, AllowStringType.SimpleText);
            Title = new StringField(this, "title", 4096, AllowStringType.SimpleText);
            HtmlText = new StringField(this, "htmltext", 262144, AllowStringType.SafeHtml);
        }

        public override string ToString()
        {
            return Label.Value;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var assignment in database.Query<PageTemplateAssignment>(DC.Equal("templateid", Id.Value)))
            {
                assignment.Delete(database);
            }

            database.Delete(this);
        }

        public IEnumerable<PageTemplateAssignment> Assignments(IDatabase database)
        {
            return database.Query<PageTemplateAssignment>(DC.Equal("templateid", Id.Value));
        }

        public override string GetText(Translator translator)
        {
            return Label.Value;
        }
    }
}

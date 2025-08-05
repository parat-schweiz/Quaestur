using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class PageTemplateAssignment : DatabaseObject, ITemplateAssignment<PageTemplate>
    {
        public ForeignKeyField<PageTemplate, PageTemplateAssignment> Template { get; private set; }
        public EnumField<TemplateAssignmentType> AssignedType { get; private set; }
        public Field<Guid> AssignedId { get; private set; }
        public StringField FieldName { get; private set; }

        public PageTemplateAssignment() : this(Guid.Empty)
        {
        }

        public PageTemplateAssignment(Guid id) : base(id)
        {
            Template = new ForeignKeyField<PageTemplate, PageTemplateAssignment>(this, "templateid", false, null);
            AssignedType = new EnumField<TemplateAssignmentType>(this, "assignedtype", TemplateAssignmentType.BallotTemplate, TemplateAssignmentTypeExtensions.Translate);
            AssignedId = new Field<Guid>(this, "assignedid", Guid.Empty);
            FieldName = new StringField(this, "fieldname", 256, AllowStringType.SimpleText);
        }

        public Organization GetOrganization(IDatabase database)
        {
            var o = AssignedTo(database);

            if (o is Subscription)
            {
                return ((Subscription)o).Membership.Value.Organization.Value;
            }
            else
            {
                throw new NotSupportedException();
            }
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

        public string FieldNameTranslation(Translator translator)
        {
            return translator.Get(
                "PageTemplateAssignment.FieldName." + FieldName.Value,
                FieldName.Value + " field in the page template assignment",
                FieldName.Value);
        }

        public string GetText(IDatabase database, Translator translator)
        {
            return AssignedTo(database).GetText(translator) + "/" + FieldNameTranslation(translator);
        }

        public DatabaseObject AssignedTo(IDatabase database)
        {
            switch (AssignedType.Value)
            {
                case TemplateAssignmentType.Subscription:
                    return database.Query<Subscription>(AssignedId.Value);
                default:
                    throw new NotSupportedException();
            }
        }

        PageTemplate ITemplateAssignment<PageTemplate>.Template
        {
            get { return Template.Value; }
            set { Template.Value = value; }
        }

        TemplateAssignmentType ITemplateAssignment<PageTemplate>.AssignedType
        {
            get { return AssignedType.Value; }
            set { AssignedType.Value = value; }
        }

        Guid ITemplateAssignment<PageTemplate>.AssignedId
        {
            get { return AssignedId.Value; }
            set { AssignedId.Value = value; }
        }

        string ITemplateAssignment<PageTemplate>.FieldName
        {
            get { return FieldName.Value; }
            set { FieldName.Value = value; }
        }
    }
}

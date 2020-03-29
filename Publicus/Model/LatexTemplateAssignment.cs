using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Publicus
{
    public class LatexTemplateAssignment : DatabaseObject
    {
        public ForeignKeyField<LatexTemplate, LatexTemplateAssignment> Template { get; private set; }
        public EnumField<TemplateAssignmentType> AssignedType { get; private set; }
        public Field<Guid> AssignedId { get; private set; }
        public StringField FieldName { get; private set; }

        public LatexTemplateAssignment() : this(Guid.Empty)
        {
        }

        public LatexTemplateAssignment(Guid id) : base(id)
        {
            Template = new ForeignKeyField<LatexTemplate, LatexTemplateAssignment>(this, "templateid", false, null);
            AssignedType = new EnumField<TemplateAssignmentType>(this, "assignedtype", TemplateAssignmentType.Petition, TemplateAssignmentTypeExtensions.Translate);
            AssignedId = new Field<Guid>(this, "assignedid", Guid.Empty);
            FieldName = new StringField(this, "fieldname", 256, AllowStringType.SimpleText);
        }

        public Feed GetFeed(IDatabase database)
        {
            var o = AssignedTo(database);

            if (o is Petition)
            {
                return ((Petition)o).Feed.Value;
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
                "LatexTemplateAssignment.FieldName." + FieldName.Value,
                FieldName.Value + " field in the mail template assignment",
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
                case TemplateAssignmentType.Petition:
                    return database.Query<Petition>(AssignedId.Value);
                default:
                    throw new NotSupportedException();
            }
        }

        public string TranslateFieldName(IDatabase database, Translator translator)
        {
            switch (AssignedType.Value)
            {
                case TemplateAssignmentType.Petition:
                    return Petition.GetFieldNameTranslation(translator, FieldName.Value);
                default:
                    throw new NotSupportedException();
            }
        }

        public PartAccess AssignedPartAccess
        {
            get
            {
                switch (AssignedType.Value)
                {
                    case TemplateAssignmentType.Petition:
                        return PartAccess.Petition;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        public bool HasAccess(IDatabase database, Session session, AccessRight right)
        {
            switch (AssignedType.Value)
            {
                case TemplateAssignmentType.Petition:
                    var feed1 = (AssignedTo(database) as Petition).Feed.Value;
                    return session.HasAccess(feed1, AssignedPartAccess, right);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

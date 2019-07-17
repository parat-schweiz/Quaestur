using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public enum TemplateAssignmentType
    { 
        BallotTemplate = 0,
        MembershipType = 1,
    }

    public static class TemplateAssignmentTypeExtensions
    {
        public static string Translate(this TemplateAssignmentType type, Translator translator)
        {
            switch (type)
            {
                case TemplateAssignmentType.BallotTemplate:
                    return translator.Get("Enum.TemplateAssignmentType.BallotTemplate", "Value 'Ballot template' in template assignment type enum", "Ballot template");
                case TemplateAssignmentType.MembershipType:
                    return translator.Get("Enum.TemplateAssignmentType.MembershipType", "Value 'Membership type' in template assignment type enum", "Membership type");
                default:
                    throw new NotSupportedException();
            }
        }

        public static PartAccess AccessPart(this TemplateAssignmentType type)
        {
            switch (type)
            {
                case TemplateAssignmentType.BallotTemplate:
                    return PartAccess.Ballot;
                case TemplateAssignmentType.MembershipType:
                    return PartAccess.Structure;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class TemplateField
    {
        public TemplateAssignmentType AssignedType { get; private set; }
        public Guid AssignedId { get; private set; }
        public string FieldName { get; private set; }

        public TemplateField(
            TemplateAssignmentType assignedType,
            Guid assignedId,
            string fieldName)
        {
            AssignedType = assignedType;
            AssignedId = assignedId;
            FieldName = fieldName; 
        }
    }
}

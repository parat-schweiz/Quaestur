using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Publicus
{
    public enum TemplateAssignmentType
    { 
        Petition = 0,
    }

    public static class TemplateAssignmentTypeExtensions
    {
        public static string Translate(this TemplateAssignmentType type, Translator translator)
        {
            switch (type)
            {
                case TemplateAssignmentType.Petition:
                    return translator.Get("Enum.TemplateAssignmentType.Petition", "Value 'Petition' in template assignment type enum", "Petition");
                default:
                    throw new NotSupportedException();
            }
        }

        public static PartAccess AccessPart(this TemplateAssignmentType type)
        {
            switch (type)
            {
                case TemplateAssignmentType.Petition:
                    return PartAccess.Petition;
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

﻿using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public enum TemplateAssignmentType
    { 
        MembershipType = 0,
    }

    public static class TemplateAssignmentTypeExtensions
    {
        public static string Translate(this TemplateAssignmentType type, Translator translator)
        {
            switch (type)
            {
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

    public interface ITemplateAssignment<T> where T : ITemplate
    {
        T Template { get; }
        TemplateAssignmentType AssignedType { get; }
        Guid AssignedId { get; }
        string FieldName { get; }
    }

    public static class TemplateUtil
    {
        public static T GetItem<T>(IDatabase database, Language language, Func<IDatabase, IEnumerable<ITemplateAssignment<T>>> get)
            where T : class, ITemplate
        {
            var list = get(database);

            foreach (var l in LanguageExtensions.PreferenceList(language))
            {
                var assignment = list.FirstOrDefault(a => a.Template.Language == l);
                if (assignment != null)
                    return assignment.Template;
            }

            return null;
        }
    }
}

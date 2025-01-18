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
        BillSendingTemplate = 2,
        Subscription = 3,
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
                case TemplateAssignmentType.BillSendingTemplate:
                    return translator.Get("Enum.TemplateAssignmentType.BillSendingTemplate", "Value 'Bill sending template' in template assignment type enum", "Bill sending template type");
                case TemplateAssignmentType.Subscription:
                    return translator.Get("Enum.TemplateAssignmentType.Subscription", "Value 'Subscription' in template assignment type enum", "Subscription");
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
                case TemplateAssignmentType.BillSendingTemplate:
                case TemplateAssignmentType.Subscription:
                    return PartAccess.Structure;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class MailTemplateAssignmentField
     : TemplateField<MailTemplate, MailTemplateAssignment>
    {
        public MailTemplateAssignmentField(TemplateAssignmentType assignedType, Guid assignedId, string fieldName)
         : base(assignedType, assignedId, fieldName)
        {
        }
    }

    public class LatexTemplateAssignmentField
     : TemplateField<LatexTemplate, LatexTemplateAssignment>
    {
        public LatexTemplateAssignmentField(TemplateAssignmentType assignedType, Guid assignedId, string fieldName)
         : base(assignedType, assignedId, fieldName)
        {
        }
    }

    public class TemplateField<T, TA>
        where T : DatabaseObject, ITemplate, new()
        where TA : DatabaseObject, ITemplateAssignment<T>, new()
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

        public IEnumerable<TA> Assignments(IDatabase database)
        {
            return database.Query<TA>(DC.Equal("assignedid", AssignedId).And(DC.Equal("fieldname", FieldName)));
        }

        public IEnumerable<T> Values(IDatabase database)
        {
            return Assignments(database).Select(a => a.Template);
        }

        public T Value(IDatabase database, Language language)
        {
            var list = Values(database);

            foreach (var l in LanguageExtensions.PreferenceList(language))
            {
                var templates = list.FirstOrDefault(a => a.Language == l);
                if (templates != null)
                    return templates;
            }

            return null;
        }
    }

    public interface ITemplateAssignment<T> where T : ITemplate
    {
        T Template { get; }
        TemplateAssignmentType AssignedType { get; }
        Guid AssignedId { get; }
        string FieldName { get; }
    }
}

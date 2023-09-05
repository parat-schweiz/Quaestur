using System;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public enum InteractionReferenceType
    {
        None = 0,
        DiscoursePost = 1,
        RedmineIssue = 2,
        StorePayment = 3,
        Decay = 4,
        BalanceForward = 5,
    }

    public static class InteractionReferenceTypeExtensions
    {
        public static string Translate(this InteractionReferenceType status, Translator translator)
        {
            switch (status)
            {
                case InteractionReferenceType.None:
                    return translator.Get("Enum.InteractionReferenceType.None", "Value 'None' in InteractionReferenceType enum", "None");
                case InteractionReferenceType.DiscoursePost:
                    return translator.Get("Enum.InteractionReferenceType.DiscoursePost", "Value 'DiscoursePost' in InteractionReferenceType enum", "Discourse Post");
                case InteractionReferenceType.RedmineIssue:
                    return translator.Get("Enum.InteractionReferenceType.RedmineIssue", "Value 'RedmineIssue' in InteractionReferenceType enum", "Redmine Issue");
                case InteractionReferenceType.StorePayment:
                    return translator.Get("Enum.InteractionReferenceType.StorePayment", "Value 'StorePayment' in InteractionReferenceType enum", "Store Payment");
                case InteractionReferenceType.Decay:
                    return translator.Get("Enum.InteractionReferenceType.Decay", "Value 'Decay' in InteractionReferenceType enum", "Decay");
                case InteractionReferenceType.BalanceForward:
                    return translator.Get("Enum.InteractionReferenceType.BalanceForward", "Value 'Balance forward' in InteractionReferenceType enum", "Balance forward");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Points : DatabaseObject
    {
        public ForeignKeyField<Person, Points> Owner { get; private set; }
        public ForeignKeyField<PointBudget, Points> Budget { get; private set; }
        public FieldDateTime Moment { get; private set; }
        public Field<int> Amount { get; private set; }
        public StringField Reason { get; private set; }
        public StringField Url { get; private set; }
        public EnumField<InteractionReferenceType> ReferenceType { get; private set; }
        public Field<Guid> ReferenceId { get; private set; }

        public Points() : this(Guid.Empty)
        {
        }

        public Points(Guid id) : base(id)
        {
            Owner = new ForeignKeyField<Person, Points>(this, "ownerid", false, null);
            Budget = new ForeignKeyField<PointBudget, Points>(this, "budgetid", false, null);
            Moment = new FieldDateTime(this, "moment", DateTime.UtcNow);
            Amount = new Field<int>(this, "amount", 0);
            Reason = new StringField(this, "reason", 4096, AllowStringType.SimpleText);
            Url = new StringField(this, "url", 2048, AllowStringType.SimpleText);
            ReferenceType = new EnumField<InteractionReferenceType>(this, "referencetype", InteractionReferenceType.None, InteractionReferenceTypeExtensions.Translate);
            ReferenceId = new Field<Guid>(this, "referenceid", Guid.Empty);
        }

        public override string ToString()
        {
            return Reason.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Reason.Value;
        }
    }
}
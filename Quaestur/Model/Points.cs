using System;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public enum PointsReferenceType
    {
        None = 0 
    }

    public static class PointsReferenceTypeExtensions
    {
        public static string Translate(this PointsReferenceType status, Translator translator)
        {
            switch (status)
            {
                case PointsReferenceType.None:
                    return translator.Get("Enum.PointsReferenceType.None", "Value 'None' in PointsReferenceType enum", "None");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Points : DatabaseObject
    {
        public ForeignKeyField<Person, Points> Owner { get; private set; }
        public ForeignKeyField<PointBudget, Points> Budget { get; private set; }
        public Field<DateTime> Moment { get; private set; }
        public Field<int> Amount { get; private set; }
        public StringField Reason { get; private set; }
        public EnumField<PointsReferenceType> ReferenceType { get; private set; }
        public Field<Guid> ReferenceId { get; private set; }

        public Points() : this(Guid.Empty)
        {
        }

        public Points(Guid id) : base(id)
        {
            Owner = new ForeignKeyField<Person, Points>(this, "ownerid", false, null);
            Budget = new ForeignKeyField<PointBudget, Points>(this, "budgetid", false, null);
            Moment = new Field<DateTime>(this, "moment", DateTime.UtcNow);
            Amount = new Field<int>(this, "amount", 0);
            Reason = new StringField(this, "reason", 4096, AllowStringType.SimpleText);
            ReferenceType = new EnumField<PointsReferenceType>(this, "referencetype", PointsReferenceType.None, PointsReferenceTypeExtensions.Translate);
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
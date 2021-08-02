using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class PointBudget : DatabaseObject
    {
        public ForeignKeyField<Group, PointBudget> Owner { get; private set; }
        public ForeignKeyField<BudgetPeriod, PointBudget> Period { get; private set; }
        public MultiLanguageStringField Label { get; private set; }
        public DecimalField Share { get; private set; }
        public Field<long> CurrentPoints { get; private set; }

        public PointBudget() : this(Guid.Empty)
        {
        }

        public PointBudget(Guid id) : base(id)
        {
            Owner = new ForeignKeyField<Group, PointBudget>(this, "ownerid", false, null);
            Period = new ForeignKeyField<BudgetPeriod, PointBudget>(this, "periodid", false, null);
            Label = new MultiLanguageStringField(this, "label", AllowStringType.SimpleText);
            Share = new DecimalField(this, "share", 16, 4);
            CurrentPoints = new Field<long>(this, "currentpoints", 0);
        }

        public override string ToString()
        {
            return Label.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Period.Value.GetText(translator) + " / " + Label.Value[translator.Language];
        }

        public void UpdateCurrentPoints(IDatabase database)
        {
            long currentPoints = 0;

            foreach (var points in database.Query<Points>(DC.Equal("budgetid", Id.Value)))
            {
                currentPoints += points.Amount.Value;
            }

            CurrentPoints.Value = currentPoints;
        }
    }
}
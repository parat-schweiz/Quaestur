using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class PointTransfer : DatabaseObject
    {
        public ForeignKeyField<BudgetPeriod, PointTransfer> Source { get; private set; }
        public ForeignKeyField<BudgetPeriod, PointTransfer> Sink { get; private set; }
        public DecimalField Share { get; private set; }

        public PointTransfer() : this(Guid.Empty)
        {
        }

        public PointTransfer(Guid id) : base(id)
        {
            Source = new ForeignKeyField<BudgetPeriod, PointTransfer>(this, "sourceid", false, null);
            Sink = new ForeignKeyField<BudgetPeriod, PointTransfer>(this, "sinkid", false, null);
            Share = new DecimalField(this, "share", 16, 4);
        }

        public override string ToString()
        {
            return Sink.Value.Organization.Value.Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Source.Value.GetText(translator) + " / " + Sink.Value.Organization.Value.Name.Value[translator.Language];
        }
    }
}
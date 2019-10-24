using System;
using System.Linq;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PointsTally : DatabaseObject
    {
        public ForeignKeyField<Person, PointsTally> Person { get; private set; }
        public Field<DateTime> FromDate { get; private set; }
        public Field<DateTime> UntilDate { get; private set; }
        public Field<DateTime> CreatedDate { get; private set; }
        public Field<long> Considered { get; private set; }
        public Field<long> ForwardBalance { get; private set; }
        public ByteArrayField DocumentData { get; private set; }
        public FieldNull<DateTime> InformationDate { get; private set; }

        public PointsTally() : this(Guid.Empty)
        {
        }

        public PointsTally(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, PointsTally>(this, "personid", false, null);
            FromDate = new Field<DateTime>(this, "fromdate", new DateTime(1850, 1, 1));
            UntilDate = new Field<DateTime>(this, "untildate", new DateTime(1850, 1, 1));
            CreatedDate = new Field<DateTime>(this, "createddate", new DateTime(1850, 1, 1));
            Considered = new Field<long>(this, "considered", 0);
            ForwardBalance = new Field<long>(this, "forwardbalance", 0);
            DocumentData = new ByteArrayField(this, "documentdata", false);
            InformationDate = new FieldNull<DateTime>(this, "informationdate");
        }

        public override string ToString()
        {
            return FromDate.Value.ToString("dd.MM.yyyy") + " / " + 
                   UntilDate.Value.ToString("dd.MM.yyyy");
        }

        public override string GetText(Translator translator)
        {
            return FromDate.Value.ToString("dd.MM.yyyy") + " / " + 
                   UntilDate.Value.ToString("dd.MM.yyyy");
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public string FileName(Translator translator)
        {
            return translator.Get(
                "PointsTally.FileName", 
                "File name for the points tally", 
                "Points_tally_{0}_{1}.pdf", 
                FromDate.Value.ToString("yyyyMMdd"), 
                UntilDate.Value.ToString("yyyyMMdd"));
        }
    }
}

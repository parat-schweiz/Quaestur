using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace RedmineEngagement
{
    public class Assignment : DatabaseObject
    {
        public ForeignKeyField<Issue, Assignment> Issue { get; private set; }
        public StringField ConfigId { get; private set; }
        public ForeignKeyField<Person, Assignment> Person { get; private set; }
        public StringField AwardedCalculation { get; private set; }
        public FieldNull<int> AwardedPoints { get; private set; }
        public FieldNull<Guid> AwardedPointsId { get; private set; }

        public Assignment() : this(Guid.Empty)
        {
        }

        public Assignment(Guid id) : base(id)
        {
            Issue = new ForeignKeyField<Issue, Assignment>(this, "issueid", false, i => i.Assignments);
            ConfigId = new StringField(this, "configid", 256);
            Person = new ForeignKeyField<Person, Assignment>(this, "personid", false, null);
            AwardedCalculation = new StringField(this, "awardedcalculation", 4096);
            AwardedPoints = new FieldNull<int>(this, "awardedpoints");
            AwardedPointsId = new FieldNull<Guid>(this, "awardedpointsid");
        }

        public override string ToString()
        {
            return "Assignment " + Id.ToString();
        }

        public override string GetText(Translator translator)
        {
            return Id.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this); 
        }
    }
}

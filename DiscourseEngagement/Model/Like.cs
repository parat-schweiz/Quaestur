using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace DiscourseEngagement
{
    public class Like : DatabaseObject
    {
        public ForeignKeyField<Post, Like> Post { get; private set; }
        public ForeignKeyField<Person, Like> Person { get; private set; }
        public DateTimeField Created { get; private set; }
        public EnumField<AwardDecision> AwardDecision { get; private set; }
        public StringField AwardedFromCalculation { get; private set; }
        public FieldNull<int> AwardedFromPoints { get; private set; }
        public FieldNull<Guid> AwardedFromPointsId { get; private set; }
        public StringField AwardedToCalculation { get; private set; }
        public FieldNull<int> AwardedToPoints { get; private set; }
        public FieldNull<Guid> AwardedToPointsId { get; private set; }

        public Like() : this(Guid.Empty)
        {
        }

        public Like(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, Like>(this, "personid", true, null);
            Post = new ForeignKeyField<Post, Like>(this, "postid", false, null);
            Created = new DateTimeField(this, "created", new DateTime(1850, 1, 1));
            AwardDecision = new EnumField<AwardDecision>(this, "awarddecision", DiscourseEngagement.AwardDecision.None, AwardDecisionExtensions.Translate);
            AwardedFromCalculation = new StringField(this, "awardedfromcalculation", 4096, AllowStringType.SimpleText);
            AwardedFromPoints = new FieldNull<int>(this, "awardedfrompoints");
            AwardedFromPointsId = new FieldNull<Guid>(this, "awardedfrompointsid");
            AwardedToCalculation = new StringField(this, "awardedtocalculation", 4096, AllowStringType.SimpleText);
            AwardedToPoints = new FieldNull<int>(this, "awardedtopoints");
            AwardedToPointsId = new FieldNull<Guid>(this, "awardedpointsid");
        }

        public override string ToString()
        {
            return "Topic " + Id.ToString();
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

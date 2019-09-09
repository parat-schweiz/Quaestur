using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace DiscourseEngagement
{
    public enum AwardDecision
    {
        None = 0,
        Negative = 1,
        Positive = 2,
    }

    public static class AwardDecisionExtensions
    {
        public static string Translate(this AwardDecision awardDecision, Translator translator)
        {
            return awardDecision.ToString();
        }
    }

    public class Post : DatabaseObject
    {
        public ForeignKeyField<Topic, Post> Topic { get; private set; }
        public ForeignKeyField<Person, Post> Person { get; private set; }
        public Field<int> PostId { get; private set; }
        public Field<DateTime> Created { get; private set; }
        public Field<int> LikeCount { get; private set; }
        public EnumField<AwardDecision> AwardDecision { get; private set; }
        public StringField AwardedCalculation { get; private set; }
        public FieldNull<int> AwardedPoints { get; private set; }
        public FieldNull<Guid> AwardedPointsId { get; private set; }

        public Post() : this(Guid.Empty)
        {
        }

        public Post(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, Post>(this, "personid", true, null);
            Topic = new ForeignKeyField<Topic, Post>(this, "topicid", false, null);
            PostId = new Field<int>(this, "postid", 0);
            Created = new Field<DateTime>(this, "created", new DateTime(1850, 1, 1));
            LikeCount = new Field<int>(this, "likecount", 0);
            AwardDecision = new EnumField<AwardDecision>(this, "awarddecision", DiscourseEngagement.AwardDecision.None, AwardDecisionExtensions.Translate);
            AwardedCalculation = new StringField(this, "awardedcalculation", 4096, AllowStringType.SimpleText);
            AwardedPoints = new FieldNull<int>(this, "awardedpoints");
            AwardedPointsId = new FieldNull<Guid>(this, "awardedpointsid");
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
            foreach (var like in database.Query<Like>(DC.Equal("postid", Id.Value)))
            {
                database.Delete(like);
            }

            database.Delete(this); 
        }
    }
}

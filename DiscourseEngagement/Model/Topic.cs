using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace DiscourseEngagement
{
    public class Topic : DatabaseObject
    {
        public Field<int> TopicId { get; private set; }
        public Field<int> PostsCount { get; private set; }
        public Field<int> LikeCount { get; private set; }
        public FieldDateTime LastPostedAt { get; private set; }

        public Topic() : this(Guid.Empty)
        {
        }

        public Topic(Guid id) : base(id)
        {
            TopicId = new Field<int>(this, "topicid", 0);
            PostsCount = new Field<int>(this, "postscount", 0);
            LikeCount = new Field<int>(this, "likecount", 0);
            LastPostedAt = new FieldDateTime(this, "lastpostedat", new DateTime(1970, 1, 1));
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
            foreach (var post in database.Query<Post>(DC.Equal("topicid", Id.Value)))
            {
                database.Delete(post);
            }

            database.Delete(this); 
        }
    }
}

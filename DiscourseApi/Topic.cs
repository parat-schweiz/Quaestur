using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscourseApi
{
    public class Topic
    {
        public int Id { get; private set; }
        public string Title { get; private set; }
        public bool Visible { get; private set; }
        public bool Closed { get; private set; }
        public int PostsCount { get; private set; }
        public int ReplyCount { get; private set; }
        public int LikeCount { get; private set; }
        public int Views { get; private set; }
        public List<string> Tags { get; private set; }
        public List<Post> Posts { get; private set; }

        public Topic(JObject obj, IEnumerable<Post> posts)
        {
            Id = obj.Value<int>("id");
            Title = obj.Value<string>("title");
            Visible = obj.Value<bool>("visible");
            Closed = obj.Value<bool>("closed");
            PostsCount = obj.Value<int>("posts_count");
            ReplyCount = obj.Value<int>("reply_count");
            LikeCount = obj.Value<int>("like_count");
            Views = obj.Value<int>("views");

            Tags = new List<string>();
            var tags = obj.Value<JArray>("tags");
            if (tags != null) tags.Values<string>();

            if (posts != null)
            {
                Posts = posts.ToList();
            }
            else
            {
                Posts = new List<Post>();
            }
        }
    }
}

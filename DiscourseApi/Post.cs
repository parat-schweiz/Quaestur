using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscourseApi
{
    public class Post
    {
        public int Id { get; private set; }
        public int TopicId { get; private set; }
        public int UserId { get; private set; }
        public string Username { get; private set; }
        public string DisplayName { get; private set; }
        public string Text { get; private set; }
        public double Score { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public int LikeCount { get; private set; }

        public Post(JObject obj)
        {
            Id = obj.Value<int>("id");
            TopicId = obj.Value<int>("topic_id");
            UserId = obj.Value<int>("user_id");
            Username = obj.Value<string>("username");
            DisplayName = obj.Value<string>("display_username");
            Text = obj.Value<string>("cooked");
            Score = obj.Value<double>("score");
            CreatedAt = DateTime.Parse(obj.Value<string>("created_at"));
            UpdatedAt = DateTime.Parse(obj.Value<string>("updated_at"));
            LikeCount = 0;
            foreach (var actObj in obj.Value<JArray>("actions_summary").Values<JObject>())
            {
                if (actObj.Value<int>("id") == 2)
                {
                    if (actObj["count"] != null)
                    {
                        LikeCount = actObj.Value<int>("count");
                    }
                    break;
                }
            }
        }
    }
}

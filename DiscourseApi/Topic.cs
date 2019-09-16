using System;
using System.Linq;
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

        public Topic(JObject obj)
        {
            Id = obj.Value<int>("id");
            Title = obj.Value<string>("title");
            Visible = obj.Value<bool>("visible");
            Closed = obj.Value<bool>("closed");
        }
    }
}

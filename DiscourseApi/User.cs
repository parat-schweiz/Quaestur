using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscourseApi
{
    public class User
    {
        public int Id { get; private set; }
        public string Username { get; private set; }
        public Guid? Auid { get; private set; }

        public User(JObject obj)
        {
            Id = obj.Value<int>("id");
            Username = obj.Value<string>("username");
            var auidString = obj.Value<string>("auid");
            Auid = string.IsNullOrEmpty(auidString) ? (Guid?)null : Guid.Parse(auidString);
        }
    }
}

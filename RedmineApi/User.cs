using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedmineApi
{
    public class User
    {
        public int Id { get; private set; }
        public string Username { get; private set; }
        public string Firstname { get; private set; }
        public string Lastname { get; private set; }
        public string Mail { get; private set; }

        public User(JObject obj)
        {
            Id = obj.Value<int>("id");
            Username = obj.Value<string>("login");
            Firstname = obj.Value<string>("firstname");
            Lastname = obj.Value<string>("lastname");
            Mail = obj.Value<string>("mail");
        }
    }
}

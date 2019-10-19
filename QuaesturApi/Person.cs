using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuaesturApi
{
    public class Person
    {
        public Guid Id { get; private set; }
        public string Username { get; private set; }
        public Language Language { get; private set; }

        public Person(JObject obj)
        {
            Id = Guid.Parse(obj.Value<string>("id"));
            Username = obj.Value<string>("username");
            Language = (Language)Enum.Parse(typeof(Language), obj.Value<string>("username"));
        }
    }
}

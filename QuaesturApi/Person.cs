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

        public Person(JObject obj)
        {
            Id = obj.Value<Guid>("id");
            Username = obj.Value<string>("username");
        }
    }
}

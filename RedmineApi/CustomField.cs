using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedmineApi
{
    public class CustomField
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Value { get; private set; }

        public CustomField(JObject obj)
        {
            Id = obj.Value<int>("id");
            Name = obj.Value<string>("name");
            Value = obj.Value<string>("value");
        }
    }
}

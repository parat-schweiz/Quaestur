using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedmineApi
{
    public class CustomField
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public IEnumerable<string> Values { get; private set; }

        public CustomField(JObject obj)
        {
            Id = obj.Value<int>("id");
            Name = obj.Value<string>("name");

            if (obj.GetValue("value") is JArray)
            {
                Values = obj.Value<JArray>("value").Values<string>().ToList();
            }
            else
            {
                Values = new string[] { obj.Value<string>("value") };
            }
        }
    }
}

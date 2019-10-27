using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedmineApi
{
    public class NamedId
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public NamedId(JObject obj)
        {
            Id = obj.Value<int>("id");
            Name = obj.Value<string>("name");
        }
    }
}

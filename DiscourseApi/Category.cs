using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscourseApi
{
    public class Category
    {
        public int Id { get; private set; }
        public string Name { get; private set; }

        public Category(JObject obj)
        {
            Id = obj.Value<int>("id");
            Name = obj.Value<string>("name");
        }
    }
}

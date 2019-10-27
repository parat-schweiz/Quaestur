using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RedmineApi
{
    public class Issue
    {
        public int Id { get; private set; }
        public string Subject { get; private set; }
        public string Description { get; private set; }
        public DateTime CreatedOn { get; private set; }
        public DateTime UpdatedOn { get; private set; }
        public NamedId Project { get; private set; }
        public NamedId Tracker { get; private set; }
        public NamedId Status { get; private set; }
        public NamedId Category { get; private set; }
        public NamedId Author { get; private set; }
        public NamedId AssignedTo { get; private set; }
        public List<CustomField> CustomFields { get; private set; }

        public Issue(JObject obj)
        {
            Id = obj.Value<int>("id");
            Subject = obj.Value<string>("subject");
            Description = obj.Value<string>("description");
            CreatedOn = obj.Value<DateTime>("created_on");
            UpdatedOn = obj.Value<DateTime>("updated_on");
            Project = new NamedId(obj.Value<JObject>("project"));
            Tracker = new NamedId(obj.Value<JObject>("tracker"));
            Status = new NamedId(obj.Value<JObject>("status"));
            Category = new NamedId(obj.Value<JObject>("category"));
            Author = new NamedId(obj.Value<JObject>("author"));
            AssignedTo = new NamedId(obj.Value<JObject>("assigned_to"));
            CustomFields = new List<CustomField>();
            var customFields = obj.Value<JArray>("custom_fields");

            if (customFields != null)
            {
                foreach (var customField in customFields.Values<JObject>())
                {
                    CustomFields.Add(new CustomField(customField)); 
                } 
            }
        }
    }
}

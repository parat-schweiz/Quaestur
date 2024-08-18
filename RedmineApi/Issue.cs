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
        public NamedId Version { get; private set; }
        public List<CustomField> CustomFields { get; private set; }

        private NamedId GetNamedId(JObject obj, string key)
        {
            var subObj = obj.Value<JObject>(key);

            if (subObj == null)
            {
                return null;
            }
            else
            {
                return new NamedId(subObj); 
            }
        }

        public Issue(JObject obj)
        {
            Id = obj.Value<int>("id");
            Subject = obj.Value<string>("subject");
            Description = obj.Value<string>("description");
            CreatedOn = obj.Value<DateTime>("created_on");
            UpdatedOn = obj.Value<DateTime>("updated_on");
            Project = GetNamedId(obj, "project");
            Tracker = GetNamedId(obj, "tracker");
            Status = GetNamedId(obj, "status");
            Category = GetNamedId(obj, "category");
            Author = GetNamedId(obj, "author");
            AssignedTo = GetNamedId(obj, "assigned_to");
            Version = GetNamedId(obj, "fixed_version");
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

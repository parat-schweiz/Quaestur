using System;
using System.Collections.Generic;
using System.Xml.Linq;
using BaseLibrary;
using QuaesturApi;
using RedmineApi;

namespace RedmineEngagement
{
    public class StatusUpdateConfig : ConfigSection
    {
        public string Id { get; private set; }
        public string Tracker { get; private set; }
        public string Status { get; private set; }
        public string Project { get; private set; }
        public string Category { get; private set; }
        public DateTime MinimumDate { get; private set; } = DateTime.MinValue;
        public DateTime MaximumDate { get; private set; } = DateTime.MaxValue;
        public string NewStatus { get; private set; }

        public StatusUpdateConfig(XElement element)
        {
            Load(element);
        }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("Id", v => Id = v);
                yield return new ConfigItemString("Tracker", v => Tracker = v, false);
                yield return new ConfigItemString("Status", v => Status = v, true);
                yield return new ConfigItemString("Project", v => Project = v, false);
                yield return new ConfigItemString("Category", v => Category = v, false);
                yield return new ConfigItemString("NewStatus", v => NewStatus = v, true);
            } 
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }
}

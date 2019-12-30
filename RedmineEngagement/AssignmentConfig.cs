using System;
using System.Collections.Generic;
using System.Xml.Linq;
using BaseLibrary;
using QuaesturApi;
using RedmineApi;

namespace RedmineEngagement
{
    public class AssignmentConfig : ConfigSection
    {
        public string Id { get; private set; }
        public string PointsBudget { get; private set; }
        public string PointsField { get; private set; }
        public int Points { get; private set; }
        public string UserField { get; private set; }
        public string Tracker { get; private set; }
        public string Status { get; private set; }
        public string Project { get; private set; }
        public string Category { get; private set; }
        public string Reason { get; private set; }
        public DateTime MinimumDate { get; private set; } = DateTime.MinValue;
        public DateTime MaximumDate { get; private set; } = DateTime.MaxValue;
        public string NewStatus { get; private set; }

        public AssignmentConfig(XElement element)
        {
            Points = 0;
            Load(element);
        }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("Id", v => Id = v);
                yield return new ConfigItemString("PointsBudget", v => PointsBudget = v);
                yield return new ConfigItemString("PointsField", v => PointsField = v, false);
                yield return new ConfigItemInt32("Points", v => Points = v, false);
                yield return new ConfigItemString("UserField", v => UserField = v);
                yield return new ConfigItemString("Tracker", v => Tracker = v, false);
                yield return new ConfigItemString("Status", v => Status = v, false);
                yield return new ConfigItemString("Project", v => Project = v, false);
                yield return new ConfigItemString("Category", v => Category = v, false);
                yield return new ConfigItemString("Reason", v => Reason = v);
                yield return new ConfigItemDateTime("MinimumDate", v => MinimumDate = v, false);
                yield return new ConfigItemDateTime("MaximumDate", v => MaximumDate = v, false);
                yield return new ConfigItemString("NewStatus", v => NewStatus = v, false);
            } 
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }
}

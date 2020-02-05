using System;
using System.Collections.Generic;
using System.Xml.Linq;
using BaseLibrary;
using QuaesturApi;
using RedmineApi;

namespace RedmineEngagement
{
    public class EngagementConfig : Config
    {
        public QuaesturApiConfig QuaesturApi { get; private set; }
        public RedmineApiConfig RedmineApi { get; private set; }
        public ConfigSectionDatabase Database { get; private set; }
        public string LogFilePrefix { get; private set; }
        public List<AssignmentConfig> Assignments { get; private set; }
        public List<StatusUpdateConfig> StatusUpdates { get; private set; }

        public EngagementConfig()
        {
            QuaesturApi = new QuaesturApiConfig();
            RedmineApi = new RedmineApiConfig();
            Database = new ConfigSectionDatabase();
            Assignments = new List<AssignmentConfig>();
            StatusUpdates = new List<StatusUpdateConfig>();
        }

        public override IEnumerable<ConfigSection> ConfigSections
        {
            get
            {
                yield return QuaesturApi;
                yield return RedmineApi;
                yield return Database;
            }
        }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("LogFilePrefix", v => LogFilePrefix = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs
        {
            get
            {
                yield return new SubConfig<AssignmentConfig>(
                    "Assignment",
                    e => new AssignmentConfig(e),
                    v => Assignments.Add(v));
                yield return new SubConfig<StatusUpdateConfig>(
                    "StatusUpdate",
                    e => new StatusUpdateConfig(e),
                    v => StatusUpdates.Add(v));
            } 
        }
    }
}

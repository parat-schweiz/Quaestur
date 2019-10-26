using System;
using System.Collections.Generic;
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

        public EngagementConfig()
        {
            QuaesturApi = new QuaesturApiConfig();
            RedmineApi = new RedmineApiConfig();
            Database = new ConfigSectionDatabase();
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
    }
}

using System;
using System.Collections.Generic;
using BaseLibrary;
using QuaesturApi;
using DiscourseApi;

namespace DiscourseEngagement
{
    public class EngagementConfig : Config
    {
        public QuaesturApiConfig QuaesturApi { get; private set; }
        public DiscourseApiConfig DiscourseApi { get; private set; }
        public string LogFilePrefix { get; private set; }

        public EngagementConfig()
        {
            QuaesturApi = new QuaesturApiConfig();
            DiscourseApi = new DiscourseApiConfig();
        }

        public override IEnumerable<ConfigSection> ConfigSections
        {
            get
            {
                yield return QuaesturApi;
                yield return DiscourseApi;
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

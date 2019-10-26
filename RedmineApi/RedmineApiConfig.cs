using System;
using System.Collections.Generic;
using BaseLibrary;

namespace RedmineApi
{
    public class RedmineApiConfig : ConfigSection
    {
        public string ApiUrl { get; set; }
        public string ApiUsername { get; set; }
        public string ApiKey { get; set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("RedmineApiUrl", v => ApiUrl = v);
                yield return new ConfigItemString("RedmineApiUsername", v => ApiUsername = v);
                yield return new ConfigItemString("RedmineApiKey", v => ApiKey = v);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using BaseLibrary;

namespace DiscourseApi
{
    public class DiscourseApiConfig : ConfigSection
    {
        public string ApiUrl { get; set; }
        public string ApiUsername { get; set; }
        public string ApiKey { get; set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("DiscourseApiUrl", v => ApiUrl = v);
                yield return new ConfigItemString("DiscourseApiUsername", v => ApiUsername = v);
                yield return new ConfigItemString("DiscourseApiKey", v => ApiKey = v);
            }
        }
    }
}

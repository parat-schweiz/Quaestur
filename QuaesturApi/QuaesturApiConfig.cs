using System;
using System.Collections.Generic;
using BaseLibrary;

namespace QuaesturApi
{
    public class QuaesturApiConfig : ConfigSection
    {
        public string ApiUrl { get; set; }
        public string ApiClientId { get; set; }
        public string ApiSecret { get; set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("QuaesturApiUrl", v => ApiUrl = v);
                yield return new ConfigItemString("QuaesturApiClientId", v => ApiClientId = v);
                yield return new ConfigItemString("QuaesturApiSecret", v => ApiSecret = v);
            }
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using BaseLibrary;

namespace SecurityService
{
    public class SecurityServiceConfig : Config
    {
        public ConfigSectionMail Mail { get; private set; }

        public SecurityServiceConfig()
        {
            Mail = new ConfigSectionMail();
        }

        public override IEnumerable<ConfigSection> ConfigSections
        {
            get
            {
                yield return Mail;
            }
        }

        public string SystemMailGpgKeyId { get; set; }
        public string SystemMailGpgKeyPassphrase { get; set; }
        public string GpgHomedir { get; set; }
        public byte[] PresharedKey { get; set; }
        public byte[] SecretKey { get; set; }
        public string BindAddress { get; set; }
        public string LogFilePrefix { get; set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("SystemMailGpgKeyId", v => SystemMailGpgKeyId = v);
                yield return new ConfigItemString("SystemMailGpgKeyPassphrase", v => SystemMailGpgKeyPassphrase = v);
                yield return new ConfigItemString("GpgHomedir", v => GpgHomedir = v);
                yield return new ConfigItemBytes("PresharedKey", v => PresharedKey = v);
                yield return new ConfigItemBytes("SecretKey", v => SecretKey = v);
                yield return new ConfigItemString("BindAddress", v => BindAddress = v);
                yield return new ConfigItemString("LogFilePrefix", v => LogFilePrefix = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using BaseLibrary;

namespace Quaestur
{
    public class QuaesturConfig : Config
    {
        public ConfigSectionDatabase Database { get; private set; }
        public ConfigSectionMail Mail { get; private set; }
        public ConfigSectionSecurityServiceClient SecurityService { get; private set; }

        public QuaesturConfig()
        {
            Database = new ConfigSectionDatabase();
            Mail = new ConfigSectionMail();
            SecurityService = new ConfigSectionSecurityServiceClient();
            MatrixDomains = new List<string>();
        }

        public override IEnumerable<ConfigSection> ConfigSections
        {
            get
            {
                yield return Database;
                yield return Mail;
                yield return SecurityService;
            }
        }

        public string WebSiteAddress { get; private set; }
        public string PingenApiToken { get; private set; }
        public string SiteName { get; private set; }
        public byte[] LinkKey { get; private set; }
        public string LogFilePrefix { get; private set; }
        public List<string> MatrixDomains { get; private set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("WebSiteAddress", v => WebSiteAddress = v);
                yield return new ConfigItemString("PingenApiToken", v => PingenApiToken = v);
                yield return new ConfigItemString("SiteName", v => SiteName = v);
                yield return new ConfigItemBytes("LinkKey", v => LinkKey = v);
                yield return new ConfigItemString("LogFilePrefix", v => LogFilePrefix = v);
                yield return new ConfigMultiItemString("MatrixDomain", v => MatrixDomains.Add(v));
            }
        }
    }
}

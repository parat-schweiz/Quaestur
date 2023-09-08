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
        public ConfigSectionMailCounter MailCounter { get; private set; }
        public ConfigSectionSecurityServiceClient SecurityService { get; private set; }

        public QuaesturConfig()
        {
            Database = new ConfigSectionDatabase();
            Mail = new ConfigSectionMail();
            MailCounter = new ConfigSectionMailCounter();
            SecurityService = new ConfigSectionSecurityServiceClient();
            MatrixDomains = new List<string>();
        }

        public override IEnumerable<ConfigSection> ConfigSections
        {
            get
            {
                yield return Database;
                yield return Mail;
                yield return MailCounter;
                yield return SecurityService;
            }
        }

        public string WebSiteAddress { get; private set; }
        public string PingenApiToken { get; private set; }
        public string SiteName { get; private set; }
        public byte[] LinkKey { get; private set; }
        public string LogFilePrefix { get; private set; }
        public List<string> MatrixDomains { get; private set; }
        public int StartTaskWaitSeconds { get; private set; }
        public int SessionExpiryAbsoluteSeconds { get; private set; }
        public int SessionExpiryRelativeSeconds { get; private set; }
        public byte[] SessionEncryptionKey { get; private set; }
        public byte[] SessionAuthenticationKey { get; private set; }

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
                yield return new ConfigItemInt32("StartTaskWaitSeconds", v => StartTaskWaitSeconds = v);
                yield return new ConfigItemInt32("SessionExpiryAbsoluteSeconds", v => SessionExpiryAbsoluteSeconds = v);
                yield return new ConfigItemInt32("SessionExpiryRelativeSeconds", v => SessionExpiryRelativeSeconds = v);
                yield return new ConfigItemBytes("SessionEncryptionKey", v => SessionEncryptionKey = v);
                yield return new ConfigItemBytes("SessionAuthenticationKey", v => SessionAuthenticationKey = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }
}

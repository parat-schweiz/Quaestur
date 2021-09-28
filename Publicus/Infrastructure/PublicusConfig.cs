using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using BaseLibrary;

namespace Publicus
{
    public class PublicusConfig : Config
    {
        public ConfigSectionDatabase Database { get; private set; }
        public ConfigSectionMail Mail { get; private set; }
        public ConfigSectionMailCounter MailCounter { get; private set; }
        public ConfigSectionSecurityServiceClient SecurityService { get; private set; }
        public List<ConfigSectionOauth2Client> Oauth2Services { get; private set; }

        public PublicusConfig()
        {
            Database = new ConfigSectionDatabase();
            Mail = new ConfigSectionMail();
            MailCounter = new ConfigSectionMailCounter();
            SecurityService = new ConfigSectionSecurityServiceClient();
            Oauth2Services = new List<ConfigSectionOauth2Client>();
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

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("WebSiteAddress", v => WebSiteAddress = v);
                yield return new ConfigItemString("PingenApiToken", v => PingenApiToken = v);
                yield return new ConfigItemString("SiteName", v => SiteName = v);
                yield return new ConfigItemBytes("LinkKey", v => LinkKey = v);
                yield return new ConfigItemString("LogFilePrefix", v => LogFilePrefix = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs
        {
            get
            {
                yield return new SubConfig<ConfigSectionOauth2Client>(
                    ConfigSectionOauth2Client.Tag,
                    e => new ConfigSectionOauth2Client(e),
                    c => Oauth2Services.Add(c));
            }
        }
    }
}

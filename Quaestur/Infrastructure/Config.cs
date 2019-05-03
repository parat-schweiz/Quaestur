using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using BaseLibrary;

namespace Quaestur
{
    public class Config : IMailConfig
    {
		private const string ConfigTag = "Config";
		private const string DatabaseServerTag = "DatabaseServer";
		private const string DatabasePortTag = "DatabasePort";
		private const string DatabaseNameTag = "DatabaseName";
		private const string DatabaseUsernameTag = "DatabaseUsername";
		private const string DatabasePasswordTag = "DatabasePassword";
		private const string AdminMailAddressTag = "AdminMailAddress";
		private const string SystemMailAddressTag = "SystemMailAddress";
        private const string MailServerHostTag = "MailServerHost";
		private const string MailServerPortTag = "MailServerPort";
		private const string MailAccountNameTag = "MailAccountName";
		private const string MailAccountPasswordTag = "MailAccountPassword";
		private const string WebSiteAddressTag = "WebSiteAddress";
        private const string PingenApiTokenTag = "PingenApiToken";
        private const string SiteNameTag = "SiteName";
        private const string LinkKeyTag = "LinkKey";
        private const string SecurityServiceUrlTag = "SecurityServiceUrl";
        private const string SecurityServiceKeyTag = "SecurityServiceKey";
        private const string LogFilePrefixTag = "LogFilePrefix";

        public string DatabaseServer { get; set; }
		public int DatabasePort { get; set; }
		public string DatabaseName { get; set; }
		public string DatabaseUsername { get; set; }
		public string DatabasePassword { get; set; }
		public string AdminMailAddress { get; set; }
		public string SystemMailAddress { get; set; }
        public string MailServerHost { get; set; }
		public int MailServerPort { get; set; }
		public string MailAccountName { get; set; }
		public string MailAccountPassword { get; set; }
		public string WebSiteAddress { get; set; }
        public string PingenApiToken { get; set; }
        public string SiteName { get; set; }
        public byte[] LinkKey { get; set; }
        public string SecurityServiceUrl { get; set; }
        public byte[] SecurityServiceKey { get; set; }
        public string LogFilePrefix { get; set; }

        public Config()
        {
        }

        public void Load(string filename)
		{
			var document = XDocument.Load(filename);
			var root = document.Root;

			DatabaseServer = root.Element(DatabaseServerTag).Value;
			DatabasePort = int.Parse(root.Element(DatabasePortTag).Value);
			DatabaseName = root.Element(DatabaseNameTag).Value;
			DatabaseUsername = root.Element(DatabaseUsernameTag).Value;
			DatabasePassword = root.Element(DatabasePasswordTag).Value;
			AdminMailAddress = root.Element(AdminMailAddressTag).Value;
			SystemMailAddress = root.Element(SystemMailAddressTag).Value;
            MailServerHost = root.Element(MailServerHostTag).Value;
			MailServerPort = int.Parse(root.Element(MailServerPortTag).Value);
			MailAccountName = root.Element(MailAccountNameTag).Value;
			MailAccountPassword = root.Element(MailAccountPasswordTag).Value;
			WebSiteAddress = root.Element(WebSiteAddressTag).Value;
            PingenApiToken = root.Element(PingenApiTokenTag).Value;
            SiteName = root.Element(SiteNameTag).Value;
            LinkKey = root.Element(LinkKeyTag).Value.ParseHexBytes();
            SecurityServiceUrl = root.Element(SecurityServiceUrlTag).Value;
            SecurityServiceKey = root.Element(SecurityServiceKeyTag).Value.ParseHexBytes();
            LogFilePrefix = root.Element(LogFilePrefixTag).Value;
        }

        public void Save(string filename)
		{
			var document = new XDocument();
			var root = new XElement(ConfigTag);
			document.Add(root);

			root.Add(new XElement(DatabaseServerTag, DatabaseServer));
			root.Add(new XElement(DatabasePortTag, DatabasePort));
			root.Add(new XElement(DatabaseNameTag, DatabaseName));
			root.Add(new XElement(DatabaseUsernameTag, DatabaseUsername));
			root.Add(new XElement(DatabasePasswordTag, DatabasePassword));
			root.Add(new XElement(AdminMailAddressTag, AdminMailAddress));
			root.Add(new XElement(SystemMailAddressTag, SystemMailAddress));
            root.Add(new XElement(MailServerHostTag, MailServerHost));
			root.Add(new XElement(MailServerPortTag, MailServerPort));
			root.Add(new XElement(MailAccountNameTag, MailAccountName));
			root.Add(new XElement(MailAccountPasswordTag, MailAccountPassword));
			root.Add(new XElement(WebSiteAddressTag, WebSiteAddress));
            root.Add(new XElement(PingenApiTokenTag, PingenApiToken));
            root.Add(new XElement(SiteNameTag, SiteName));
            root.Add(new XElement(LinkKeyTag, LinkKey.ToHexString()));
            root.Add(new XElement(SecurityServiceUrlTag, SecurityServiceUrl));
            root.Add(new XElement(SecurityServiceKeyTag, SecurityServiceKey.ToHexString()));
            root.Add(new XElement(LogFilePrefixTag, LogFilePrefix));

            document.Save(filename);
		}
    }
}

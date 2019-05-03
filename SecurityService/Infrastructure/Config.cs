using System;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using BaseLibrary;

namespace SecurityService
{
    public class Config : IMailConfig
    {
		private const string ConfigTag = "Config";
		private const string AdminMailAddressTag = "AdminMailAddress";
		private const string SystemMailAddressTag = "SystemMailAddress";
        private const string SystemMailGpgKeyIdTag = "SystemMailGpgKeyId";
        private const string SystemMailGpgKeyPassphraseTag = "SystemMailGpgKeyPassphrase";
        private const string MailServerHostTag = "MailServerHost";
		private const string MailServerPortTag = "MailServerPort";
		private const string MailAccountNameTag = "MailAccountName";
		private const string MailAccountPasswordTag = "MailAccountPassword";
        private const string GpgHomedirTag = "GpgHomedir";
        private const string PresharedKeyTag = "PresharedKey";
        private const string SecretKeyTag = "SecretKey";
        private const string BindAddressTag = "BindAddress";
        private const string LogFilePrefixTag = "LogFilePrefix";

        public string AdminMailAddress { get; set; }
		public string SystemMailAddress { get; set; }
        public string SystemMailGpgKeyId { get; set; }
        public string SystemMailGpgKeyPassphrase { get; set; }
        public string MailServerHost { get; set; }
		public int MailServerPort { get; set; }
		public string MailAccountName { get; set; }
		public string MailAccountPassword { get; set; }
		public string GpgHomedir { get; set; }
        public byte[] PresharedKey { get; set; }
        public byte[] SecretKey { get; set; }
        public string BindAddress { get; set; }
        public string LogFilePrefix { get; set; }

        public Config()
        {
        }

        public void Load(string filename)
		{
			var document = XDocument.Load(filename);
			var root = document.Root;

			AdminMailAddress = root.Element(AdminMailAddressTag).Value;
			SystemMailAddress = root.Element(SystemMailAddressTag).Value;
            SystemMailGpgKeyId = root.Element(SystemMailGpgKeyIdTag).Value;
            SystemMailGpgKeyPassphrase = root.Element(SystemMailGpgKeyPassphraseTag).Value;
            MailServerHost = root.Element(MailServerHostTag).Value;
			MailServerPort = int.Parse(root.Element(MailServerPortTag).Value);
			MailAccountName = root.Element(MailAccountNameTag).Value;
			MailAccountPassword = root.Element(MailAccountPasswordTag).Value;
            GpgHomedir = root.Element(GpgHomedirTag).Value;
            PresharedKey = root.Element(PresharedKeyTag).Value.ParseHexBytes();
            SecretKey = root.Element(SecretKeyTag).Value.ParseHexBytes();
            BindAddress = root.Element(BindAddressTag).Value;
            LogFilePrefix = root.Element(LogFilePrefixTag).Value;
        }

        public void Save(string filename)
		{
			var document = new XDocument();
			var root = new XElement(ConfigTag);
			document.Add(root);

			root.Add(new XElement(AdminMailAddressTag, AdminMailAddress));
			root.Add(new XElement(SystemMailAddressTag, SystemMailAddress));
            root.Add(new XElement(SystemMailGpgKeyIdTag, SystemMailGpgKeyId));
            root.Add(new XElement(SystemMailGpgKeyPassphraseTag, SystemMailGpgKeyPassphrase));
            root.Add(new XElement(MailServerHostTag, MailServerHost));
			root.Add(new XElement(MailServerPortTag, MailServerPort));
			root.Add(new XElement(MailAccountNameTag, MailAccountName));
			root.Add(new XElement(MailAccountPasswordTag, MailAccountPassword));
            root.Add(new XElement(GpgHomedirTag, GpgHomedir));
            root.Add(new XElement(PresharedKeyTag, PresharedKey.ToHexString()));
            root.Add(new XElement(SecretKeyTag, SecretKey.ToHexString()));
            root.Add(new XElement(BindAddressTag, BindAddress));
            root.Add(new XElement(LogFilePrefixTag, LogFilePrefix));

            document.Save(filename);
		}
    }
}

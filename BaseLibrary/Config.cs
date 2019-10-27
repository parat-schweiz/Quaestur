using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;

namespace BaseLibrary
{
    public class ConfigSectionSecurityServiceClient : ConfigSection
    {
        public string SecurityServiceUrl { get; set; }
        public byte[] SecurityServiceKey { get; set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("SecurityServiceUrl", v => SecurityServiceUrl = v);
                yield return new ConfigItemBytes("SecurityServiceKey", v => SecurityServiceKey = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }

    public class ConfigSectionOauth2Client : ConfigSection
    {
        public string OAuth2AuthorizationUrl { get; set; }
        public string OAuth2TokenUrl { get; set; }
        public string OAuth2ApiUrl { get; set; }
        public string OAuth2ClientId { get; set; }
        public string OAuth2ClientSecret { get; set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("OAuth2AuthorizationUrl", v => OAuth2AuthorizationUrl = v);
                yield return new ConfigItemString("OAuth2TokenUrl", v => OAuth2TokenUrl = v);
                yield return new ConfigItemString("OAuth2ApiUrl", v => OAuth2ApiUrl = v);
                yield return new ConfigItemString("OAuth2ClientId", v => OAuth2ClientId = v);
                yield return new ConfigItemString("OAuth2ClientSecret", v => OAuth2ClientSecret = v);
            }
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }

    public class ConfigSectionMail : ConfigSection
    {
        public string MailServerHost { get; set; }
        public int MailServerPort { get; set; }
        public string MailAccountName { get; set; }
        public string MailAccountPassword { get; set; }
        public string AdminMailAddress { get; private set; }
        public string SystemMailAddress { get; private set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get
            {
                yield return new ConfigItemString("MailServerHost", v => MailServerHost = v);
                yield return new ConfigItemInt32("MailServerPort", v => MailServerPort = v);
                yield return new ConfigItemString("MailAccountName", v => MailAccountName = v);
                yield return new ConfigItemString("MailAccountPassword", v => MailAccountPassword = v);
                yield return new ConfigItemString("AdminMailAddress", v => AdminMailAddress = v);
                yield return new ConfigItemString("SystemMailAddress", v => SystemMailAddress = v);
            } 
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }

    public class ConfigSectionDatabase : ConfigSection 
    {
        public string DatabaseServer { get; set; }
        public int DatabasePort { get; set; }
        public string DatabaseName { get; set; }
        public string DatabaseUsername { get; set; }
        public string DatabasePassword { get; set; }

        public override IEnumerable<ConfigItem> ConfigItems
        {
            get 
            {
                yield return new ConfigItemString("DatabaseServer", v => DatabaseServer = v);
                yield return new ConfigItemInt32("DatabasePort", v => DatabasePort = v);
                yield return new ConfigItemString("DatabaseName", v => DatabaseName = v);
                yield return new ConfigItemString("DatabaseUsername", v => DatabaseUsername = v);
                yield return new ConfigItemString("DatabasePassword", v => DatabasePassword = v);
            } 
        }

        public override IEnumerable<SubConfig> SubConfigs => new SubConfig[0];
    }

    public abstract class Config : ConfigSection
    {
        public abstract IEnumerable<ConfigSection> ConfigSections { get; }

        public override void Load(string filename)
        {
            foreach (var configSection in ConfigSections)
            {
                configSection.Load(filename);
            }

            base.Load(filename);
        }
    }

    public abstract class SubConfig
    {
        public string Tag { get; private set; }

        public SubConfig(string tag)
        {
            Tag = tag;
        }

        public abstract void Load(XElement element);
    }

    public class SubConfig<T> : SubConfig
    {
        private Func<XElement, T> _create;
        private Action<T> _assign;

        public SubConfig(string tag, Func<XElement, T> create, Action<T> assign)
            : base(tag)
        {
            _create = create;
            _assign = assign;
        }

        public override void Load(XElement element)
        {
            _assign(_create(element));
        }
    }

    public abstract class ConfigSection
    {
        public abstract IEnumerable<ConfigItem> ConfigItems { get; }

        public abstract IEnumerable<SubConfig> SubConfigs { get; }

        public virtual void Load(string filename)
        {
            var document = XDocument.Load(filename);
            Load(document.Root);
        }

        public virtual void Load(XElement root)
        {
            foreach (var configItem in ConfigItems)
            {
                configItem.Load(root);
            }

            foreach (var subConfig in SubConfigs)
            {
                foreach (var element in root.Elements(subConfig.Tag))
                {
                    subConfig.Load(element); 
                } 
            }
        }
    }

    public abstract class ConfigItem
    {
        public abstract void Load(XElement root);
    }

    public abstract class ConfigItem<T> : ConfigItem
    {
        protected string Tag { get; private set; }
        private Action<T> _assign;
        private bool _required;

        public ConfigItem(string tag, Action<T> assign, bool required)
        {
            Tag = tag;
            _assign = assign;
            _required = required;
        }

        protected abstract T Convert(string value);

        public override void Load(XElement root)
        {
            var elements = root.Elements(Tag);

            if (!elements.Any() && _required)
            {
                throw new XmlException("Config node " + Tag + " not found");
            }
            else if (elements.Count() >= 2)
            {
                throw new XmlException("Config node " + Tag + " ambigous");
            }

            if (elements.Any())
            {
                _assign(Convert(elements.Single().Value));
            }
        }
    }

    public abstract class ConfigMultiItem<T> : ConfigItem
    {
        protected string Tag { get; private set; }
        private Action<T> _add;

        public ConfigMultiItem(string tag, Action<T> add)
        {
            Tag = tag;
            _add = add;
        }

        protected abstract T Convert(string value);

        public override void Load(XElement root)
        {
            var elements = root.Elements(Tag);

            foreach (var element in elements)
            {
                _add(Convert(element.Value));
            }
        }
    }

    public class ConfigItemString : ConfigItem<string>
    {
        public ConfigItemString(string tag, Action<string> assign, bool required = true)
            : base(tag, assign, required)
        {
        }

        protected override string Convert(string value)
        {
            return value;
        }
    }

    public class ConfigMultiItemString : ConfigMultiItem<string>
    {
        public ConfigMultiItemString(string tag, Action<string> add)
            : base(tag, add)
        {
        }

        protected override string Convert(string value)
        {
            return value;
        }
    }

    public class ConfigItemInt32 : ConfigItem<int>
    {
        public ConfigItemInt32(string tag, Action<int> assign, bool required = true)
            : base(tag, assign, required)
        {
        }

        protected override int Convert(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            else
            {
                throw new XmlException("Cannot convert value of config node " + Tag + " to integer"); 
            }
        }
    }

    public class ConfigItemBytes : ConfigItem<byte[]>
    {
        public ConfigItemBytes(string tag, Action<byte[]> assign, bool required = true) 
            : base(tag, assign, required)
        {
        }

        protected override byte[] Convert(string value)
        {
            var bytes = value.TryParseHexBytes();

            if (bytes != null)
            {
                return bytes;
            }
            else
            {
                throw new XmlException("Cannot convert value of config node " + Tag + " to bytes"); 
            }
        }
    }
}

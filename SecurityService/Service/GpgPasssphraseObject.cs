using System;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;

namespace SecurityService
{
    public class GpgPasssphraseObject : SecurityObject
    {
        private const string PassphraseProperty = "passphrase";

        public string Passphrase { get; private set; }

        public GpgPasssphraseObject()
            : base(false)
        {
        }

        public GpgPasssphraseObject(string passphrase)
            : base(true)
        {
            Passphrase = passphrase;
        }

        protected override string ObjectType { get { return "gpgpassphrase"; } }

        protected override void LoadData(JObject json)
        {
            Passphrase = json.Value<string>(PassphraseProperty);
        }

        protected override void SaveData(JObject json)
        {
            json.Add(new JProperty(PassphraseProperty, Passphrase));
        }
    }
}

using System;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using OtpNet;

namespace SecurityService
{
    public class TotpObject : SecurityObject
    {
        private const string SecretProperty = "secret";

        private byte[] _secret;

        public TotpObject()
            : base(false)
        {
        }

        public TotpObject(byte[] secret)
            : base(true)
        {
            _secret = secret;
        }

        public bool Verify(string code)
        {
            var totp = new Totp(_secret);

            return totp.VerifyTotp(code, out long timeStepMatched);
        }

        protected override string ObjectType { get { return "totp"; } }

        protected override void LoadData(JObject json)
        {
            _secret = Convert.FromBase64String(json.Value<string>(SecretProperty));
        }

        protected override void SaveData(JObject json)
        {
            json.Add(new JProperty(SecretProperty, Convert.ToBase64String(_secret)));
        }
    }
}

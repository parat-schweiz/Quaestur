using System;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using OtpNet;

namespace SecurityService
{
    public class TotpObject : SecurityObject
    {
        private const string SecretProperty = "secret";

        public byte[] Secret { get; private set; }

        public TotpObject()
            : base(false)
        {
        }

        public TotpObject(byte[] secret)
            : base(true)
        {
            Secret = secret;
        }

        public bool Verify(string code)
        {
            var totp = new Totp(Secret);

            return totp.VerifyTotp(code, out long timeStepMatched);
        }

        protected override string ObjectType { get { return "totp"; } }

        protected override void LoadData(JObject json)
        {
            Secret = Convert.FromBase64String(json.Value<string>(SecretProperty));
        }

        protected override void SaveData(JObject json)
        {
            json.Add(new JProperty(SecretProperty, Convert.ToBase64String(Secret)));
        }
    }
}

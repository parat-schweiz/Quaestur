using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SecureChannel
{
    public class SecretMessage
    {
        private const string CipherTextProperty = "ciphertext";
        private const string NonceProperty = "nonce";

        public byte[] CipherText { get; private set; }
        public byte[] Nonce { get; private set; }

        public SecretMessage(byte[] message, byte[] nonce)
        {
            CipherText = message;
            Nonce = nonce;
        }

        public SecretMessage(string jsonText)
        {
            var json = JObject.Parse(jsonText);
            CipherText = Convert.FromBase64String(json.Value<string>(CipherTextProperty));
            Nonce = Convert.FromBase64String(json.Value<string>(NonceProperty));
        }

        public string ToJson()
        {
            var json = new JObject();
            json.Add(new JProperty(CipherTextProperty, Convert.ToBase64String(CipherText)));
            json.Add(new JProperty(NonceProperty, Convert.ToBase64String(Nonce)));
            return json.ToString();
        }
    }
}

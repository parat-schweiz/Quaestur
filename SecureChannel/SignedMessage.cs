using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SecureChannel
{
    public class SignedMessage
    {
        private const string MessageProperty = "message";
        private const string SignatureProperty = "signature";

        public byte[] Message { get; private set; }
        public byte[] Signature { get; private set; }

        public SignedMessage(byte[] message, byte[] signature)
        {
            Message = message;
            Signature = signature;
        }

        public SignedMessage(string jsonText)
        {
            var json = JObject.Parse(jsonText);
            Message = Convert.FromBase64String(json.Value<string>(MessageProperty));
            Signature = Convert.FromBase64String(json.Value<string>(SignatureProperty));
        }

        public string ToJson()
        {
            var json = new JObject();
            json.Add(new JProperty(MessageProperty, Convert.ToBase64String(Message)));
            json.Add(new JProperty(SignatureProperty, Convert.ToBase64String(Signature)));
            return json.ToString();
        }
    }
}

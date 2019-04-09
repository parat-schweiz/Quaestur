using System;
using System.Text;
using Newtonsoft.Json.Linq;
using Sodium;

namespace SecurityService
{
    public class Encrypted<T> where T : SecurityObject, new()
    {
        private const string MessageProperty = "message";
        private const string NonceProperty = "nonce";

        private readonly byte[] _message;
        private readonly byte[] _nonce;

        public Encrypted(JObject json)
        {
            _message = Convert.FromBase64String(json.Value<string>(MessageProperty));
            _nonce = Convert.FromBase64String(json.Value<string>(NonceProperty));
        }

        public Encrypted(byte[] data)
            : this(JObject.Parse(Encoding.UTF8.GetString(data)))
        {
        }

        public byte[] ToBinary()
        {
            return Encoding.UTF8.GetBytes(ToJson().ToString());
        }

        public JObject ToJson()
        {
            var json = new JObject();
            json.Add(new JProperty(MessageProperty, Convert.ToBase64String(_message)));
            json.Add(new JProperty(NonceProperty, Convert.ToBase64String(_nonce)));
            return json;
        }

        public Encrypted(T obj, byte[] key)
        {
            _nonce = SecretBox.GenerateNonce();
            _message = SecretBox.Create(obj.ToBinary(), _nonce, key);
        }

        public T Decrypt(byte[] key)
        {
            return SecurityObject.Parse<T>(SecretBox.Open(_message, _nonce, key));
        }
    }
}

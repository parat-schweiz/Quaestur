using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sodium;
using BaseLibrary;

namespace SecureChannel
{
    public abstract class SecureClient
    {
        private readonly byte[] _presharedKey;
        private KeyPair _keyPair;
        private byte[] _sessionKey;
        private long _counter;

        protected Logger Logger { get; private set; }

        protected SecureClient(byte[] presharedKey, Logger logger)
        {
            _presharedKey = presharedKey;
            Logger = logger;
        }

        protected abstract string SendAgree(string request);

        protected abstract string SendRequest(string request);

        public void Agree()
        {
            var initRequest = new JObject();
            initRequest.Add(new JProperty(Protocol.Agree.CommandProperty, Protocol.Agree.InitCommand));
            var initResponse = SendAgree(initRequest);
            var publicKey = Convert.FromBase64String(initResponse.Value<string>(Protocol.Agree.PublicKeyProperty));
            Logger.Info("Security Service: Key agreement initiated");
            _keyPair = PublicKeyBox.GenerateKeyPair();
            var commitRequest = new JObject();
            commitRequest.Add(new JProperty(Protocol.Agree.CommandProperty, Protocol.Agree.CommitCommand));
            commitRequest.Add(new JProperty(Protocol.Agree.PublicKeyProperty, Convert.ToBase64String(_keyPair.PublicKey)));
            var commitResponse = SendAgree(commitRequest);
            var nonce = Convert.FromBase64String(commitResponse.Value<string>(Protocol.Agree.NonceProperty));
            var encryptedCommitment = Convert.FromBase64String(commitResponse.Value<string>(Protocol.Agree.CommitmentProperty));
            var sessionKey = ScalarMult.Mult(_keyPair.PrivateKey, publicKey);
            var commitment = SecretBox.Open(encryptedCommitment, nonce, sessionKey);
            if (Encoding.UTF8.GetString(commitment) != Protocol.Agree.CommitmentValue) throw new InvalidOperationException();
            _sessionKey = sessionKey;
            Logger.Info("Security Service: Key agreement committed");
            _counter = 0;
        }

        private JObject SendAgree(JObject requestJson)
        {
            var token = SecretBox.GenerateNonce().ToHexString();
            requestJson.Add(new JProperty(Protocol.Agree.TokenProperty, token));
            requestJson.Add(new JProperty(Protocol.Agree.TimestampProperty, DateTime.UtcNow));
            var requestMessage = Encoding.UTF8.GetBytes(requestJson.ToString());
            var requestSignature = SecretKeyAuth.Sign(requestMessage, _presharedKey);
            var requestPacket = new SignedMessage(requestMessage, requestSignature);
            var responsePacket = new SignedMessage(SendAgree(requestPacket.ToJson()));

            if (SecretKeyAuth.Verify(responsePacket.Message, responsePacket.Signature, _presharedKey))
            {
                var responseJson = JObject.Parse(Encoding.UTF8.GetString(responsePacket.Message));

                if (responseJson.Value<string>(Protocol.Agree.TokenProperty) == token)
                {
                    return responseJson; 
                }
            }

            throw new InvalidOperationException();
        }

        public JObject Request(JObject request)
        {
            for (int i = 1; true; i++)
            {
                try
                {
                    return RequestInternal(request);
                }
                catch (BaseChannelException exception)
                {
                    if (i <= 1000)
                    {
                        System.Threading.Thread.Sleep(i * 1000);
                        Logger.Warning("Retrying security service key agreement (times: {0})", i);
                        Agree();
                    }
                    else
                    {
                        throw exception;
                    }
                }
            }
        }

        private JObject RequestInternal(JObject request)
        {
            var token = SecretBox.GenerateNonce().ToHexString();
            var requestJson = new JObject();
            requestJson.Add(new JProperty(Protocol.Request.PayloadProperty, request));
            requestJson.Add(new JProperty(Protocol.Request.TokenProperty, token));
            requestJson.Add(new JProperty(Protocol.Request.CounterProperty, _counter));
            _counter++;
            var requestMessage = Encoding.UTF8.GetBytes(requestJson.ToString());
            var nonce = SecretBox.GenerateNonce();
            var requestCipherText = SecretBox.Create(requestMessage, nonce, _sessionKey);
            var requestPacket = new SecretMessage(requestCipherText, nonce);
            var responsePacket = new SecretMessage(SendRequest(requestPacket.ToJson()));
            var responseData = SecretBox.Open(responsePacket.CipherText, responsePacket.Nonce, _sessionKey);
            var responseJson = JObject.Parse(Encoding.UTF8.GetString(responseData));

            if (responseJson.Value<string>(Protocol.Request.TokenProperty) == token)
            {
                return responseJson.Value<JObject>(Protocol.Request.PayloadProperty);
            }

            throw new InvalidOperationException();
        }
    }
}

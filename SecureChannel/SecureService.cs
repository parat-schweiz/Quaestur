using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sodium;

namespace SecureChannel
{
    public abstract class SecureService
    {
        private readonly byte[] _presharedKey;
        private KeyPair _keyPair;
        private byte[] _sessionKey;
        private long _counter;
        private readonly object _lock = new object();

        protected SecureService(byte[] presharedKey)
        {
            _presharedKey = presharedKey;
        }

        protected abstract JObject Process(JObject request);

        protected abstract void Info(string text, params string[] arguments);

        protected abstract void Error(string text, params string[] arguments);

        public string Request(string data)
        {
            lock (_lock)
            {
                try
                {
                    if (_sessionKey == null) throw new SecureChannelException("Session key not present");
                    var requestMessage = new SecretMessage(data);
                    var requestData = SecretBox.Open(requestMessage.CipherText, requestMessage.Nonce, _sessionKey);
                    var request = JObject.Parse(Encoding.UTF8.GetString(requestData));
                    var counter = request.Value<int>(Protocol.Request.CounterProperty);
                    if (_counter != counter) throw new SecureChannelException("Replay counter mismatch");
                    _counter++;
                    var requestPayload = request.Value<JObject>(Protocol.Request.PayloadProperty);
                    var responsePayload = Process(requestPayload);
                    var response = new JObject();
                    response.Add(new JProperty(Protocol.Request.TokenProperty, request.Value<string>(Protocol.Request.TokenProperty)));
                    response.Add(new JProperty(Protocol.Request.PayloadProperty, responsePayload));
                    var responseData = Encoding.UTF8.GetBytes(response.ToString());
                    var nonce = SecretBox.GenerateNonce();
                    var responseMessage = new SecretMessage(SecretBox.Create(responseData, nonce, _sessionKey), nonce);
                    return responseMessage.ToJson();
                }
                catch (SecureChannelException exception)
                {
                    Error("Request security error: {0}", exception.Message);
                    throw;
                }
                catch (JsonException exception)
                {
                    Error("Request error: Malformed data; {0}", exception.Message);
                    throw;
                }
                catch (Exception exception)
                {
                    Error("Request error: {0}", exception.Message);
                    throw;
                }
            }
        }

        public string Agree(string requestData)
        {
            lock (_lock)
            {
                try
                {
                    var request = new SignedMessage(requestData);

                    if (SecretKeyAuth.Verify(request.Message, request.Signature, _presharedKey))
                    {
                        var agreement = JObject.Parse(Encoding.UTF8.GetString(request.Message));
                        var timestamp = agreement.Value<DateTime>(Protocol.Agree.TimestampProperty);

                        if (Math.Abs(DateTime.UtcNow.Subtract(timestamp).TotalSeconds) <= 3d)
                        {
                            var command = agreement.Value<string>(Protocol.Agree.CommandProperty);
                            var token = agreement.Value<string>(Protocol.Agree.TokenProperty);
                            JObject reply = new JObject();
                            reply.Add(new JProperty(Protocol.Agree.ReplyProperty, command));
                            reply.Add(new JProperty(Protocol.Agree.TokenProperty, token));

                            switch (command)
                            {
                                case Protocol.Agree.InitCommand:
                                    _keyPair = PublicKeyBox.GenerateKeyPair();
                                    reply.Add(Protocol.Agree.PublicKeyProperty, Convert.ToBase64String(_keyPair.PublicKey));
                                    Info("Key agreement initiated");
                                    break;
                                case Protocol.Agree.CommitCommand:
                                    var publicKey = Convert.FromBase64String(agreement.Value<string>(Protocol.Agree.PublicKeyProperty));
                                    _sessionKey = ScalarMult.Mult(_keyPair.PrivateKey, publicKey);
                                    _counter = 0;
                                    var nonce = SecretBox.GenerateNonce();
                                    var commitment = SecretBox.Create(Encoding.UTF8.GetBytes(Protocol.Agree.CommitmentValue), nonce, _sessionKey);
                                    reply.Add(Protocol.Agree.NonceProperty, Convert.ToBase64String(nonce));
                                    reply.Add(Protocol.Agree.CommitmentProperty, Convert.ToBase64String(commitment));
                                    Info("Key agreement committed");
                                    break;
                                default:
                                    throw new SecureChannelException("Unsupported command");
                            }

                            var responseData = Encoding.UTF8.GetBytes(reply.ToString());
                            var signature = SecretKeyAuth.Sign(responseData, _presharedKey);
                            var response = new SignedMessage(responseData, signature);
                            return response.ToJson();
                        }
                        else
                        {
                            throw new SecureChannelException("Timestamp mismatch");
                        }
                    }
                    else
                    {
                        throw new SecureChannelException("Signature mismatch");
                    }
                }
                catch (SecureChannelException exception)
                {
                    Error("Key agreement error: Channel failled; {0}", exception.Message);
                    throw;
                }
                catch (JsonException exception)
                {
                    Error("Key agreement error: Malformed data; {0}", exception.Message);
                    throw;
                }
                catch (Exception exception)
                {
                    Error("Key agreement error: {0}", exception.Message);
                    throw;
                }
            }
        }
    }
}

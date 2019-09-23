using System;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using BaseLibrary;

namespace SecurityServiceClient
{
    public class SecurityService
    {
        private SecurityServiceChannel _channel;
        private readonly object _lock = new object();

        public SecurityService(ConfigSectionSecurityServiceClient config)
        {
            _channel = new SecurityServiceChannel(config.SecurityServiceUrl, config.SecurityServiceKey);
            _channel.Agree();
        }

        private void CheckError(JObject reply)
        {
            var result = reply.ValueString(SecurityServiceProtocol.ResultProperty);

            if (result != SecurityServiceProtocol.ResultSuccess)
            {
                throw new Exception("Security service request not failed: " + result);
            }
        }

        public Tuple<int, byte[], byte[]> ExecuteGpg(byte[] input, byte[] passphraseData, string arguments)
        {
            lock (_lock)
            {
                var request = new JObject();
                request.AddProperty(SecurityServiceProtocol.CommandProperty, SecurityServiceProtocol.CommandExecuteGpg);
                request.AddProperty(SecurityServiceProtocol.ArgumentsProperty, arguments);

                if (input != null)
                {
                    request.AddProperty(SecurityServiceProtocol.InputDataProperty, input); 
                }

                if (passphraseData != null)
                {
                    request.AddProperty(SecurityServiceProtocol.PassphraseDataProperty, passphraseData);
                }

                var reply = _channel.Request(request);
                CheckError(reply);

                var exitCode = reply.ValueInt32(SecurityServiceProtocol.ExitCodeProperty);
                var output = reply.ValueBytes(SecurityServiceProtocol.OutputDataProperty);
                var error = reply.ValueBytes(SecurityServiceProtocol.ErrorDataProperty);

                return new Tuple<int, byte[], byte[]>(exitCode, output, error);
            }
        }

        public byte[] SecureGpgPassphrase(string passphrase)
        {
            lock (_lock)
            {
                var request = new JObject();
                request.AddProperty(SecurityServiceProtocol.CommandProperty, SecurityServiceProtocol.CommandSecureGpgPassphrase);
                request.AddProperty(SecurityServiceProtocol.PassphraseProperty, passphrase);
                var reply = _channel.Request(request);
                CheckError(reply);
                return reply.ValueBytes(SecurityServiceProtocol.PassphraseDataProperty);
            }
        }

        public byte[] SecurePassword(string password)
        {
            lock (_lock)
            {
                var request = new JObject();
                request.AddProperty(SecurityServiceProtocol.CommandProperty, SecurityServiceProtocol.CommandSecurePassword);
                request.AddProperty(SecurityServiceProtocol.PasswordProperty, password);
                var reply = _channel.Request(request);
                CheckError(reply);
                return reply.ValueBytes(SecurityServiceProtocol.PasswordDataProperty);
            }
        }

        public bool VerifyPassword(byte[] passwordData, string password)
        {
            lock (_lock)
            {
                var request = new JObject();
                request.AddProperty(SecurityServiceProtocol.CommandProperty, SecurityServiceProtocol.CommandVerifyPassword);
                request.AddProperty(SecurityServiceProtocol.PasswordProperty, password);
                request.AddProperty(SecurityServiceProtocol.PasswordDataProperty, passwordData);
                var reply = _channel.Request(request);
                CheckError(reply);
                return reply.ValueString(SecurityServiceProtocol.VerificationProperty) == SecurityServiceProtocol.VerificationSuccess;
            }
        }

        public byte[] SecureTotp(byte[] secret)
        {
            lock (_lock)
            {
                var request = new JObject();
                request.AddProperty(SecurityServiceProtocol.CommandProperty, SecurityServiceProtocol.CommandSecureTotp);
                request.AddProperty(SecurityServiceProtocol.SecretProperty, secret);
                var reply = _channel.Request(request);
                CheckError(reply);
                return reply.ValueBytes(SecurityServiceProtocol.TotpDataProperty);
            }
        }

        public bool VerifyTotp(byte[] totpData, string code)
        {
            lock (_lock)
            {
                var request = new JObject();
                request.AddProperty(SecurityServiceProtocol.CommandProperty, SecurityServiceProtocol.CommandVerifyTotp);
                request.AddProperty(SecurityServiceProtocol.CodeProperty, code);
                request.AddProperty(SecurityServiceProtocol.TotpDataProperty, totpData);
                var reply = _channel.Request(request);
                CheckError(reply);
                return reply.ValueString(SecurityServiceProtocol.VerificationProperty) == SecurityServiceProtocol.VerificationSuccess;
            }
        }
    }

    public class SecurityServiceChannel : SecureChannel.SecureClient
    {
        private const string AgreeEndpoint = "agree";
        private const string RequestEndpoint = "request";

        private readonly string _apiUrl;

        public SecurityServiceChannel(string apiUrl, byte[] presharedKey)
            : base(presharedKey)
        {
            _apiUrl = apiUrl;
        }

        private string Send(string endpoint, string text)
        {
            var url = string.Join("/", _apiUrl, endpoint);

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(url);
            request.Content = new StringContent(text);

            var client = new HttpClient();
            var waitResponse = client.SendAsync(request);
            waitResponse.Wait();
            var response = waitResponse.Result;

            var waitRead = response.Content.ReadAsStringAsync();
            waitRead.Wait();
            return waitRead.Result;
        }

        protected override string SendAgree(string request)
        {
            return Send(AgreeEndpoint, request);
        }

        protected override string SendRequest(string request)
        {
            return Send(RequestEndpoint, request);
        }
    }
}

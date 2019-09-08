using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuaesturApi
{
    public class UrlParameter
    {
        public string Key { get; private set; }
        public string Value { get; private set; }

        public UrlParameter(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public UrlParameter(string key, int value)
        {
            Key = key;
            Value = value.ToString();
        }
    }

    public class Quaestur
    {
        private readonly string _apiUrl;
        private readonly string _apiSecret;
        private readonly string _apiClientId;

        public Quaestur(string apiUrl, string clientId, string apiSecret)
        {
            _apiUrl = apiUrl;
            _apiClientId = clientId;
            _apiSecret = apiSecret;
        }

        public IEnumerable<Person> GetPersonList()
        {
            var result = Request("api/v2/person/list", HttpMethod.Get, null);
            return result.Values<JObject>().Select(x => new Person(x));
        }

        private JObject Request(string endpoint, HttpMethod method, JObject data, params UrlParameter[] parameters)
        {
            var url = string.Join("/", _apiUrl, endpoint);
            var paramString = string.Join("&", parameters.Select(p => string.Format("{0}={1}", p.Key, p.Value)));

            if (paramString.Length > 0)
            {
                url += "?" + paramString;
            }

            var request = new HttpRequestMessage();
            request.Method = method;
            request.RequestUri = new Uri(url);
            request.Headers.Authorization = new AuthenticationHeaderValue("QAPI2", _apiClientId + " " + _apiSecret);

            if (method == HttpMethod.Post ||
                method == HttpMethod.Put)
            {
                request.Content = new StringContent(data.ToString(), Encoding.UTF8, "application/json");
            }

            var client = new HttpClient();
            var waitResponse = client.SendAsync(request);
            waitResponse.Wait();
            var response = waitResponse.Result;

            var waitRead = response.Content.ReadAsByteArrayAsync();
            waitRead.Wait();
            var responseText = Encoding.UTF8.GetString(waitRead.Result);
            var result = JObject.Parse(responseText);

            if (result.Value<string>("status") != "success")
            {
                throw new IOException(result.Value<string>("error"));
            }

            return result;
        }
    }
}

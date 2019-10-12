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
        private readonly QuaesturApiConfig _config;

        public Quaestur(QuaesturApiConfig config)
        {
            _config = config;
        }

        public IEnumerable<Person> GetPersonList()
        {
            var result = Request("api/v2/person/list", HttpMethod.Get, null);
            return result.Value<JArray>("result").Values<JObject>().Select(x => new Person(x));
        }

        public IEnumerable<PointBudget> GetPointBudgetList()
        {
            var result = Request("api/v2/pointbudget/list", HttpMethod.Get, null);
            return result.Value<JArray>("result").Values<JObject>().Select(x => new PointBudget(x));
        }

        private JObject Request(string endpoint, HttpMethod method, JObject data, params UrlParameter[] parameters)
        {
            var url = string.Join("/", _config.ApiUrl, endpoint);
            var paramString = string.Join("&", parameters.Select(p => string.Format("{0}={1}", p.Key, p.Value)));

            if (paramString.Length > 0)
            {
                url += "?" + paramString;
            }

            var request = new HttpRequestMessage();
            request.Method = method;
            request.RequestUri = new Uri(url);
            request.Headers.Authorization = new AuthenticationHeaderValue("QAPI2", _config.ApiClientId + " " + _config.ApiSecret);

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

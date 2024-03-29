﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BaseLibrary;

namespace QuaesturApi
{
    public class ApiAccessDeniedException : Exception
    { 
    }

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

        public Points AddPoints(
            Guid ownerId,
            Guid budgetId, 
            int amount, 
            string reason, 
            string url,
            DateTime moment,
            PointsReferenceType referenceType,
            Guid referenceId,
            Guid? impersonateId)
        {
            var obj = new JObject();
            obj.Add("ownerid", ownerId);
            obj.Add("budgetid", budgetId);
            obj.Add("amount", amount);
            obj.Add("reason", reason);
            obj.Add("url", url);
            obj.Add("moment", moment.FormatIso());
            obj.Add("referencetype", referenceType.ToString());
            obj.Add("referenceid", referenceId);
            if (impersonateId.HasValue)
                obj.Add("impersonateid", impersonateId.Value);
            var result = Request("/api/v2/points/add", HttpMethod.Post, obj);
            return new Points(result.Value<JObject>("result"));
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
                var error = result.Value<string>("error");
                if (error == "Access denied")
                {
                    throw new ApiAccessDeniedException();
                }
                else
                {
                    throw new IOException(error);
                }
            }

            return result;
        }
    }
}

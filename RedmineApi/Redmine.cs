﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MimeKit;

namespace RedmineApi
{
    public static class StringExtensions
    {
        public static UrlParameter Param(this string key, string value)
        {
            return new UrlParameter(key, value); 
        }

        public static UrlParameter Param(this string key, int value)
        {
            return new UrlParameter(key, value);
        }
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

    public class Redmine
    {
        private readonly RedmineApiConfig _config;

        public Redmine(RedmineApiConfig config)
        {
            _config = config;
        }

        public IEnumerable<User> GetUsers()
        {
            int count = 0;
            int total = int.MaxValue;

            do
            {
                var response = Request<JObject>("/users.json", HttpMethod.Get, null);

                foreach (var user in response.Value<JArray>("users").Values<JObject>())
                {
                    count++;
                    yield return new User(user);
                }

                total = response.Value<int>("total_count");
            } while (count < total);
        }

        private T Request<T>(
            string endpoint, 
            HttpMethod method, 
            JToken data, 
            params UrlParameter[] parameters)
            where T : JContainer
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

            if (!string.IsNullOrEmpty(_config.ApiKey))
            {
                request.Headers.Add("X-Redmine-API-Key", _config.ApiKey);
            }

            if (!string.IsNullOrEmpty(_config.ApiUsername))
            {
                request.Headers.Add("X-Redmine-Switch-User", _config.ApiUsername);
            }

            if (method == HttpMethod.Post ||
                method == HttpMethod.Put)
            {
                request.Content = new StringContent(data.ToString(),Encoding.UTF8, "application/json");
            }

            var client = new HttpClient();
            var waitResponse = client.SendAsync(request);
            waitResponse.Wait();
            var response = waitResponse.Result;

            var waitRead = response.Content.ReadAsByteArrayAsync();
            waitRead.Wait();
            var responseText = Encoding.UTF8.GetString(waitRead.Result);

            if (typeof(T) == typeof(JObject))
            {
                return JObject.Parse(responseText) as T;
            }
            else if (typeof(T) == typeof(JArray))
            {
                return JArray.Parse(responseText) as T;
            }
            else
            {
                throw new NotSupportedException(); 
            }
        }
    }
}

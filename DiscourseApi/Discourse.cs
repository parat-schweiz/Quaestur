using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MimeKit;

namespace DiscourseApi
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

    public class Discourse
    {
        private readonly DiscourseApiConfig _config;

        public Discourse(DiscourseApiConfig config)
        {
            _config = config;
        }

        public void Post(Topic topic, string text)
        {
            var request = new JObject();
            request.Add(new JProperty("topic_id", topic.Id));
            request.Add(new JProperty("raw", text));
            request.Add(new JProperty("archetype", "regular"));
            request.Add(new JProperty("nested_post", true));

            var response = Request<JObject>("posts.json", HttpMethod.Post, request);
        }

        public IEnumerable<string> GetUsers()
        {
            var endpoint = string.Format("/users/by-external/lumi.json");
            var response = Request<JObject>(endpoint, HttpMethod.Get, null);

            //var endpoint = string.Format("/admin/users.json");
            //var response = Request<JArray>(endpoint, HttpMethod.Get, null);
            //var endpoint = string.Format("/admin/users/{0}.json", 3);
            //var response = Request<JObject>(endpoint, HttpMethod.Get, null);
            return null;
        }

        public IEnumerable<string> GetLikes(int postId)
        {
            var endpoint = string.Format("/post_action_users.json?id={0}&post_action_type_id=2", postId);
            var response = Request<JObject>(endpoint, HttpMethod.Get, null);
            return new List<string>(response
                .Value<JArray>("post_action_users")
                .Values<JObject>()
                .Select(o => o.Value<string>("username")));
        }

        public Topic GetTopic(int topicId)
        {
            var endpoint = string.Format("t/{0}.json", topicId);
            var response = Request<JObject>(endpoint, HttpMethod.Get, null);
            return new Topic(response);
        }

        public IEnumerable<Topic> GetTopics()
        {
            bool done = false;

            for (int page = 0; !done; page++)
            {
                var response = Request<JObject>("top/all.json", HttpMethod.Get, null, "page".Param(page));
                var list = response.Value<JObject>("topic_list").Value<JArray>("topics");

                foreach (JObject obj in list)
                {
                    yield return new Topic(obj);
                }

                var perPage = response.Value<JObject>("topic_list").Value<int>("per_page");

                done = list.Count() < perPage;
            }
        }

        private T Request<T>(
            string endpoint, 
            HttpMethod method, 
            JObject data, 
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

            if (_config.ApiKey != null && _config.ApiUsername != null)
            {
                request.Headers.Add("Api-Key", _config.ApiKey);
                request.Headers.Add("Api-Username", _config.ApiUsername);
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

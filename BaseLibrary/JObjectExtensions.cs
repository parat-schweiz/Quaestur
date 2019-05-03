using System;

namespace Newtonsoft.Json.Linq
{
    public static class JObjectExtensions
    {
        public static void AddProperty(this JObject obj, string key, byte[] value)
        {
            obj.AddProperty(key, Convert.ToBase64String(value));
        }

        public static void AddProperty(this JObject obj, string key, string value)
        {
            obj.Add(new JProperty(key, value));
        }

        public static void AddProperty(this JObject obj, string key, bool value)
        {
            obj.Add(new JProperty(key, value));
        }

        public static void AddProperty(this JObject obj, string key, DateTime value)
        {
            obj.Add(new JProperty(key, value));
        }

        public static void AddProperty(this JObject obj, string key, int value)
        {
            obj.Add(new JProperty(key, value));
        }

        public static byte[] ValueBytes(this JObject obj, string key)
        {
            var value = obj.Value<string>(key);

            if (value == null)
            {
                throw new ArgumentNullException("Property " + key + " not present");
            }
            else
            {
                return Convert.FromBase64String(value);
            }
        }

        public static byte[] ValueBytes(this JObject obj, string key, byte[] defaultValue)
        {
            var value = obj.Value<string>(key);

            if (value == null)
            {
                return defaultValue;
            }
            else
            {
                return Convert.FromBase64String(value);
            }
        }

        public static string ValueString(this JObject obj, string key)
        {
            var value = obj.Value<string>(key);

            if (value == null)
            {
                throw new ArgumentNullException("Property " + key + " not present");
            }
            else
            {
                return value;
            }
        }

        public static string ValueString(this JObject obj, string key, string defaultValue)
        {
            var value = obj.Value<string>(key);

            if (value == null)
            {
                return defaultValue;
            }
            else
            {
                return value;
            }
        }

        public static int ValueInt(this JObject obj, string key)
        {
            if (obj.TryGetValue(key, out JToken value))
            {
                return value.ToObject<int>();
            }
            else
            {
                throw new ArgumentNullException("Property " + key + " not present");
            }
        }

        public static int ValueInt(this JObject obj, string key, int defaultValue)
        {
            if (obj.TryGetValue(key, out JToken value))
            {
                return value.ToObject<int>();
            }
            else
            {
                return defaultValue;
            }
        }
    }
}

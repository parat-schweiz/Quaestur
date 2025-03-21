﻿using System;

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

        public static Guid ValueGuid(this JObject obj, string key)
        {
            return Guid.Parse(obj.ValueString(key));
        }

        public static bool TryValueInt32(this JObject obj, string key, out int value)
        {
            try
            {
                value = obj.ValueInt32(key);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        public static bool TryValueByte(this JObject obj, string key, out byte value)
        {
            try
            {
                value = obj.ValueByte(key);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        public static bool TryValueDateTime64(this JObject obj, string key, out DateTime value)
        {
            if (TryValueInt64(obj, key, out long int64value))
            {
                value = new DateTime(int64value);
                return true;
            }
            else
            {
                value = DateTime.MinValue;
                return false;
            }
        }

        public static bool TryValueInt64(this JObject obj, string key, out long value)
        {
            try
            {
                value = obj.ValueInt64(key);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        public static bool TryValueDecimal(this JObject obj, string key, out decimal value)
        {
            if (obj.TryValueString(key, out string valueString))
            {
                return decimal.TryParse(valueString, out value);
            }
            else
            {
                value = 0M;
                return false;
            }
        }

        public static bool TryValueHexBytes(this JObject obj, string key, out byte[] value)
        {
            if (TryValueString(obj, key, out string stringValue))
            {
                value = stringValue.TryParseHexBytes();
                return value != null;
            }
            else
            {
                value = null;
                return false;
            }
        }

        public static bool TryValueString(this JObject obj, string key, out string value)
        {
            try
            {
                value = obj.ValueString(key);
                return true;
            }
            catch
            {
                value = null;
                return false;
            }
        }

        public static bool TryValueDateTime(this JObject obj, string key, out DateTime value)
        {
            try
            {
                value = obj.ValueDateTime(key);
                return true;
            }
            catch
            {
                value = new DateTime(1850, 1, 1);
                return false;
            }
        }

        public static bool TryValueGuid(this JObject obj, string key, out Guid value)
        {
            try
            {
                value = Guid.Parse(obj.ValueString(key));
                return true;
            }
            catch
            {
                value = Guid.Empty;
                return false; 
            }
        }

        public static DateTime ValueDateTime(this JObject obj, string key)
        {
            var value = obj.Value<DateTime>(key);

            if (value == null)
            {
                throw new ArgumentNullException("Property " + key + " not present");
            }
            else
            {
                return value;
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

        public static int ValueInt32(this JObject obj, string key)
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

        public static byte ValueByte(this JObject obj, string key)
        {
            if (obj.TryGetValue(key, out JToken value))
            {
                return value.ToObject<byte>();
            }
            else
            {
                throw new ArgumentNullException("Property " + key + " not present");
            }
        }

        public static long ValueInt64(this JObject obj, string key)
        {
            if (obj.TryGetValue(key, out JToken value))
            {
                return value.ToObject<long>();
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

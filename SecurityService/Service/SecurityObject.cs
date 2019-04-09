using System;
using System.Text;
using Newtonsoft.Json.Linq;

namespace SecurityService
{
    public abstract class SecurityObject
    {
        private const string ObjectTypeProperty = "objecttype";
        private const string ObjectIdProperty = "objectid";

        protected abstract string ObjectType { get; }

        protected abstract void LoadData(JObject json);

        protected abstract void SaveData(JObject json);

        public Guid Id { get; private set; }

        protected SecurityObject(bool newObject)
        {
            if (newObject)
            {
                Id = Guid.NewGuid();
            }
            else
            {
                Id = Guid.Empty;
            }
        }

        public string ToJson()
        {
            var json = new JObject();
            json.Add(new JProperty(ObjectTypeProperty, ObjectType));
            json.Add(new JProperty(ObjectIdProperty, Id));
            SaveData(json);
            return json.ToString();
        }

        public byte[] ToBinary()
        {
            return Encoding.UTF8.GetBytes(ToJson());
        }

        public static T Parse<T>(string jsonText) where T: SecurityObject, new()
        {
            var json = JObject.Parse(jsonText);
            var objectType = json.Value<string>(ObjectTypeProperty);
            var obj = new T();
            if (objectType != obj.ObjectType) throw new ArgumentOutOfRangeException();
            obj.Id = Guid.Parse(json.Value<string>(ObjectIdProperty));
            obj.LoadData(json);
            return obj;
        }

        public static T Parse<T>(byte[] data) where T : SecurityObject, new()
        {
            return Parse<T>(Encoding.UTF8.GetString(data));
        }
    }
}

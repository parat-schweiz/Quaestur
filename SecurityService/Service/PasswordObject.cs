using System;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using BaseLibrary;

namespace SecurityService
{
    public class PasswordObject : SecurityObject
    {
        private const string SaltProperty = "salt";
        private const string DataProperty = "data";
        private const string IterationsProperty = "iterations";
        private const int CurrentNewIterations = 75000;

        private byte[] _salt;
        private byte[] _data;
        private int _iterations;

        public PasswordObject()
            : base(false)
        {
        }

        public PasswordObject(string password)
            : base(true)
        {
            _iterations = CurrentNewIterations;
            _salt = Rng.Get(16);

            using (var pbkdf = new Rfc2898DeriveBytes(password, _salt))
            {
                pbkdf.IterationCount = _iterations;
                _data = pbkdf.GetBytes(16);
            }
        }

        public bool Verify(string password)
        {
            using (var pbkdf = new Rfc2898DeriveBytes(password, _salt))
            {
                pbkdf.IterationCount = _iterations;
                return pbkdf.GetBytes(16).AreEqual(_data);
            }
        }

        protected override string ObjectType { get { return "password"; } }

        protected override void LoadData(JObject json)
        {
            _salt = Convert.FromBase64String(json.Value<string>(SaltProperty));
            _data = Convert.FromBase64String(json.Value<string>(DataProperty));
            _iterations = json.Value<int>(IterationsProperty);
        }

        protected override void SaveData(JObject json)
        {
            json.Add(new JProperty(SaltProperty, Convert.ToBase64String(_salt)));
            json.Add(new JProperty(DataProperty, Convert.ToBase64String(_data)));
            json.Add(new JProperty(IterationsProperty, _iterations));
        }
    }
}

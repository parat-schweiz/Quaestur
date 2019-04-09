using System;
using Newtonsoft.Json.Linq;

namespace SecureChannel
{
    public class SecureServiceMock : SecureService
    {
        public SecureServiceMock(byte[] preshareKey)
            : base(preshareKey)
        {
        }

        protected override void Error(string text, params string[] arguments)
        {
        }

        protected override void Info(string text, params string[] arguments)
        {
        }

        protected override JObject Process(JObject request)
        {
            return request;
        }
    }
}

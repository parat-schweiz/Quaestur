using System;

namespace SecureChannel
{
    public class SecureClientMock : SecureClient
    {
        private SecureServiceMock _server;

        public SecureClientMock(byte[] preshareKey)
            : base(preshareKey)
        {
            _server = new SecureServiceMock(preshareKey);
        }

        protected override string SendAgree(string request)
        {
            return _server.Agree(request);
        }

        protected override string SendRequest(string request)
        {
            return _server.Request(request);
        }
    }
}

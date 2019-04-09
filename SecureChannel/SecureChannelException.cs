 using System;

namespace SecureChannel
{
    public class SecureChannelException : Exception
    {
        public SecureChannelException(string message)
            : base(message)
        {
        }
    }
}

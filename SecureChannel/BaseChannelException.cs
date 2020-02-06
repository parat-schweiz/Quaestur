using System;

namespace SecureChannel
{
    public class BaseChannelException : Exception
    {
        public BaseChannelException(string message)
        : base(message)
        { }
    }
}

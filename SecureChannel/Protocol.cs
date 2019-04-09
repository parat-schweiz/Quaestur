using System;

namespace SecureChannel
{
    public static class Protocol
    {
        public static class Agree
        {
            public const string CommandProperty = "command";
            public const string TimestampProperty = "timestamp";
            public const string TokenProperty = "token";
            public const string ReplyProperty = "reply";
            public const string PublicKeyProperty = "publickey";
            public const string NonceProperty = "nonce";
            public const string CommitmentProperty = "commitment";
            public const string InitCommand = "init";
            public const string CommitCommand = "commit";
            public const string CommitmentValue = "commitment";
        }

        public static class Request
        {
            public const string PayloadProperty = "payload";
            public const string TokenProperty = "token";
            public const string CounterProperty = "counter";
        }
    }
}

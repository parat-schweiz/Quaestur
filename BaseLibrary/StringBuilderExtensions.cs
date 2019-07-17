using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Security.Cryptography;

namespace System
{
    public static class StringBuilderExtensions
    {
        public static void Append(this StringBuilder builder, string format, object[] parameters)
        {
            builder.Append(string.Format(format, parameters));
        }
    }
}

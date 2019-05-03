using System;
using Npgsql;

namespace Publicus
{
    public static class SqlExtensions
    {
        public static byte[] GetBytes(this NpgsqlDataReader reader, int index, int maxLength = 4096)
        {
            byte[] buffer = new byte[maxLength];
            var length = reader.GetBytes(index, 0, buffer, 0, buffer.Length);
            return buffer.Part(0, (int)length);
        }
    }
}

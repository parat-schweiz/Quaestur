using System;
using System.Security;
using System.Security.Cryptography;

namespace Quaestur
{
	public static class Rng
    {
        public static byte[] Get(int count)
		{
			var buffer = new byte[count];

			using (var rng = RandomNumberGenerator.Create())
			{
				rng.GetBytes(buffer);
			}

			return buffer;
		}
    }
}

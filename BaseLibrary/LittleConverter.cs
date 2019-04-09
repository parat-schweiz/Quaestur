using System;

namespace BaseLibrary
{
    public static class LittleConverter
    {
        public static byte[] Reverse(byte[] value)
        {
            var reverted = new byte[value.Length];

            for (int i = 0; i < value.Length; i++)
            {
                reverted[value.Length - i - 1] = value[i];
            }

            return reverted;
        }

        public static byte[] GetBytes(int value)
        {
            return Reverse(BitConverter.GetBytes(value));
        }

        public static byte[] GetBytes(uint value)
        {
            return Reverse(BitConverter.GetBytes(value));
        }

        public static byte[] GetBytes(long value)
        {
            return Reverse(BitConverter.GetBytes(value));
        }

        public static byte[] GetBytes(ulong value)
        {
            return Reverse(BitConverter.GetBytes(value));
        }

        public static byte[] GetBytes(short value)
        {
            return Reverse(BitConverter.GetBytes(value));
        }

        public static byte[] GetBytes(ushort value)
        {
            return Reverse(BitConverter.GetBytes(value));
        }

        public static int ToInt32(byte[] value)
        {
            if (value == null ||
                value.Length != sizeof(int))
                throw new ArgumentException();

            return BitConverter.ToInt32(Reverse(value), 0);
        }

        public static uint ToUInt32(byte[] value)
        {
            if (value == null ||
                value.Length != sizeof(uint))
                throw new ArgumentException();

            return BitConverter.ToUInt32(Reverse(value), 0);
        }

        public static long ToInt64(byte[] value)
        {
            if (value == null ||
                value.Length != sizeof(long))
                throw new ArgumentException();

            return BitConverter.ToInt64(Reverse(value), 0);
        }

        public static ulong ToUInt64(byte[] value)
        {
            if (value == null ||
                value.Length != sizeof(ulong))
                throw new ArgumentException();

            return BitConverter.ToUInt64(Reverse(value), 0);
        }

        public static short ToInt16(byte[] value)
        {
            if (value == null ||
                value.Length != sizeof(short))
                throw new ArgumentException();

            return BitConverter.ToInt16(Reverse(value), 0);
        }

        public static ushort ToUInt16(byte[] value)
        {
            if (value == null ||
                value.Length != sizeof(ushort))
                throw new ArgumentException();

            return BitConverter.ToUInt16(Reverse(value), 0);
        }
    }
}


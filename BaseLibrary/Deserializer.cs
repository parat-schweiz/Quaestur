using System;
using System.IO;
using System.Text;

namespace BaseLibrary
{
    public class Deserializer : IDisposable
    {
        private Stream _stream;

        public Stream BaseStream
        {
            get { return _stream; }
        }

        public Deserializer(Stream stream)
        {
            _stream = stream;
        }

        public Deserializer(byte[] data)
        {
            _stream = new MemoryStream(data);
        }

        public void Dispose()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }
        }

        public byte[] ReadBytes(int count)
        {
            var bytes = new byte[count];
            _stream.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        public int ReadInt32()
        {
            return LittleConverter.ToInt32(ReadBytes(sizeof(int)));
        }

        public uint ReadUInt32()
        {
            return LittleConverter.ToUInt32(ReadBytes(sizeof(uint)));
        }

        public long ReadInt64()
        {
            return LittleConverter.ToInt64(ReadBytes(sizeof(long)));
        }

        public ulong ReadUInt64()
        {
            return LittleConverter.ToUInt64(ReadBytes(sizeof(ulong)));
        }

        public int ReadInt16()
        {
            return LittleConverter.ToInt16(ReadBytes(sizeof(short)));
        }

        public int ReadUint16()
        {
            return LittleConverter.ToUInt16(ReadBytes(sizeof(ushort)));
        }

        public byte ReadByte()
        {
            var bytes = ReadBytes(1);
            return bytes[0];
        }

        public sbyte ReadSByte()
        {
            var bytes = ReadBytes(1);
            return (sbyte)bytes[0];
        }

        public DateTime ReadDateTime()
        {
            return new DateTime(ReadInt64());
        }

        private byte[] TrimZeros(byte[] data)
        {
            int length = data.Length;

            while ((length > 0) && (data[length - 1] == 0))
            {
                length--;
            }

            var trimmed = new byte[length];
            Buffer.BlockCopy(data, 0, trimmed, 0, length);

            return trimmed;
        }

        public string ReadString(int length)
        {
            return Encoding.UTF8.GetString(TrimZeros(ReadBytes(length)));
        }

        public byte[] ReadBytesPrefixed()
        {
            var length = ReadInt32();
            return ReadBytes(length);
        }

        public string ReadStringPrefixed()
        {
            return Encoding.UTF8.GetString(ReadBytesPrefixed());
        }
    }
}


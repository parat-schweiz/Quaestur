using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace BaseLibrary
{
    public static class Aes
    {
        public static byte[] Encrypt(byte[] plaintext, byte[] key)
        {
            if (plaintext == null)
                throw new ArgumentNullException("plaintext");
            if (key == null)
                throw new ArgumentNullException("key");
            if (key.Length != 32)
                throw new ArgumentException("AES key length must be 256 bit.");

            byte[] iv = Rng.Get(16);
            byte[] ciphertext = Encrypt(plaintext, key, iv);

            return iv.Concat(ciphertext);
        }

        public static byte[] Decrypt(byte[] ciphertext, byte[] key)
        {
            if (ciphertext == null)
                throw new ArgumentNullException("ciphertext");
            if (key == null)
                throw new ArgumentNullException("key");
            if (key.Length != 32)
                throw new ArgumentException("AES key length must be 256 bit.");
            if (ciphertext.Length < 16)
                throw new ArgumentException("AES ciphertext length must be at least 16 bytes.");

            byte[] iv = ciphertext.Part(0, 16);
            byte[] data = ciphertext.Part(16);

            return Decrypt(data, key, iv);
        }

        public static byte[] Encrypt(byte[] plaintext, byte[] key, byte[] iv)
        {
            if (plaintext == null)
                throw new ArgumentNullException("plaintext");
            if (key == null)
                throw new ArgumentNullException("key");
            if (iv == null)
                throw new ArgumentNullException("iv");
            if (key.Length != 32)
                throw new ArgumentException("AES key length must be 256 bit.");
            if (iv.Length != 16)
                throw new ArgumentException("AES iv length must be 128 bit.");

            using (var aes = new RijndaelManaged())
            {
                aes.Key = key;
                aes.IV = iv;

                MemoryStream memoryStream = new MemoryStream();
                CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write);
                cryptoStream.Write(plaintext, 0, plaintext.Length);
                cryptoStream.Close();
                memoryStream.Close();

                return memoryStream.ToArray();
            }
        }

        public static byte[] Decrypt(byte[] ciphertext, byte[] key, byte[] iv)
        {
            if (ciphertext == null)
                throw new ArgumentNullException("ciphertext");
            if (key == null)
                throw new ArgumentNullException("key");
            if (iv == null)
                throw new ArgumentNullException("iv");
            if (key.Length != 32)
                throw new ArgumentException("AES key length must be 256 bit.");
            if (iv.Length != 16)
                throw new ArgumentException("AES iv length must be 128 bit.");

            using (var aes = new RijndaelManaged())
            {
                aes.Key = key;
                aes.IV = iv;

                try
                {
                    MemoryStream memoryStream = new MemoryStream();
                    CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write);
                    cryptoStream.Write(ciphertext, 0, ciphertext.Length);
                    cryptoStream.Close();
                    memoryStream.Close();

                    return memoryStream.ToArray();
                }
                catch (CryptographicException)
                {
                    throw new CryptographicException("Cannot decrypt because of wrong key or iv.");
                }
            }
        }
    }
}

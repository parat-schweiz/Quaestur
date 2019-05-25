using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;
using BaseLibrary;

namespace Petitio
{
    public static class UserController
    {
        public static byte[] CreateHash(string password)
        {
            var salt = Rng.Get(16);
            var pre = salt.Concat(Encoding.UTF8.GetBytes(password));
            var sha256 = new SHA256Managed();
            var hash = sha256.ComputeHash(pre);
            return salt.Concat(hash);
        }

        public static bool VerifyHash(byte[] passwordHash, string password)
        {
            var salt = passwordHash.Part(0, 16);
            var hash = passwordHash.Part(16);
            var pre = salt.Concat(Encoding.UTF8.GetBytes(password));
            var sha256 = new SHA256Managed();
            var actual = sha256.ComputeHash(pre);
            return actual.AreEqual(hash);
        }

        public static bool ValidateDisplayName(string displayName)
        {
            if ((displayName.Length < 2) || (displayName.Length > 64))
            {
                return false;
            }

            return true;
        }

        public static bool ValidateUserName(string userName)
        {
            if ((userName.Length < 2) || (userName.Length > 16))
            {
                return false;
            }

            foreach (var c in userName)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool ValidateMailAddress(string mailAddress)
        {
            const string pattern = "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])";
            return Regex.IsMatch(mailAddress, pattern);
        }
    }
}

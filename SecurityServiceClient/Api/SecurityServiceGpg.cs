using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BaseLibrary;

namespace SecurityServiceClient
{
    public class SecurityServiceGpg : Gpg
    {
        private SecurityService _service;

        public SecurityServiceGpg(SecurityService service)
        {
            _service = service;
        }

        private byte[] EncodePassphraseData(string passphrase)
        { 
            if (passphrase == null)
            {
                return null; 
            }
            else if (passphrase.Length == 0)
            {
                return new byte[0]; 
            }
            else
            {
                return Convert.FromBase64String(passphrase);
            }
        }

        public override Tuple<int, string> Execute(Stream input, Stream output, string passphrase, IEnumerable<string> arguments)
        {
            var passphraseData = EncodePassphraseData(passphrase);
            var result = _service.ExecuteGpg(input.ToBytesOrNull(), passphraseData, string.Join(" ", arguments));
            output.Write(result.Item2, 0, result.Item2.Length);
            var errorText = Encoding.UTF8.GetString(result.Item3);
            return new Tuple<int, string>(result.Item1, errorText);
        }
    }
}

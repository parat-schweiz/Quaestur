using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace Quaestur
{
    public enum SignatureType
    {
        Sign,
        ClearSign,
        DetachSign, 
    }

    public class GpgException : Exception
    {
        public GpgException()
        { }

        public GpgException(string message)
          : base(message)
        { }
    }

    public enum GpgStatus
    {
        Success,
        Canceled,
        NoSecretKey,
        NoPublicKey,
        Error
    }

    public class GpgResult
    {
        public GpgStatus Status { get; private set; }
        public string Signer { get; private set; }

        public GpgResult(GpgStatus status, string signer)
        {
            Status = status;
            Signer = signer;
        }

        public void ThrowOnError()
        {
            if (Status != GpgStatus.Success)
            {
                throw new GpgException(Status.ToString());
            }
        }
    }

    public enum GpgCardStatus
    {
        Present,
        CardError,
        Error,
        BrokenPipe,
        CardMissing,
        PinLocked,
        ResetLocked,
        AdminLocked
    }

    public class GpgCardStatusResult
    {
        public GpgCardStatus Status { get; private set; }

        public string Fingerprint { get; private set; }

        public string ActualCardNumber { get; private set; }

        public string ExpectedCardNumber { get; private set; }

        public GpgCardStatusResult(
            GpgCardStatus status,
            string fingerprint = null,
            string actualCardNumber = null,
            string expectedCardNumber = null)
        {
            Status = status;
            Fingerprint = fingerprint;
            ActualCardNumber = actualCardNumber;
            ExpectedCardNumber = expectedCardNumber;
        }
    }

    public enum GpgTrust
    {
        Ultimate,
        Full,
        Marginal,
        Expired,
        Revoked,
        Unknown,
    }

    public enum GpgKeyType
    {
        RSA,
        DSA,
        ELG,
        Unknown
    }

    [Flags]
    public enum GpgKeyUsage
    {
        None = 0,
        Certify = 1,
        Sign = 2,
        Encrypt = 4,
        Authenticate = 8,
    }

    public enum GpgKeyStatus
    {
        Active,
        Expired,
        Revoked,
        Unknown,
    }

    public class GpgUid
    {
        public string Name { get; private set; } 

        public string Mail { get; private set; }

        public GpgTrust Trust { get; private set; }

        public GpgUid(string name, string mail, GpgTrust trust)
        {
            Name = name;
            Mail = mail;
            Trust = trust;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", Name, Mail, Trust);
        }
    }

    public class GpgSub
    {
        public GpgKeyType Type { get; private set; }

        public int Bits { get; private set; }

        public DateTime Created { get; private set; }

        public GpgKeyUsage Usage { get; private set; }

        public DateTime Expiry { get; private set; }

        public GpgKeyStatus Status { get; private set; }

        public GpgSub(GpgKeyType type, int bits, DateTime created, GpgKeyUsage usage, DateTime expiry, GpgKeyStatus status)
        {
            Type = type;
            Bits = bits;
            Created = created;
            Usage = usage;
            Expiry = expiry;
            Status = status;
        }

        public override string ToString()
        {
            return string.Format("sub {0}/{1} {2} {3} {4} {5}", Type, Bits, Created, GpgWrapper.PrintKeyUsage(Usage), Expiry, Status);
        }
    }

    public class GpgKey : GpgSub
    {
        public string Id { get; private set; }

        public List<GpgUid> Uids { get; private set; }

        public List<GpgSub> Subs { get; private set; }

        public GpgKey(string id, GpgKeyType type, int bits, DateTime created, GpgKeyUsage usage, DateTime expiry, GpgKeyStatus status)
            : base(type, bits, created, usage, expiry, status)
        {
            Id = id;
            Uids = new List<GpgUid>();
            Subs = new List<GpgSub>();
        }

        public override string ToString()
        {
            return string.Format("{0} {1}/{2} {3} {4} {5} {6}", Id, Type, Bits, Created, GpgWrapper.PrintKeyUsage(Usage), Expiry, Status);
        }
    }

    public class GpgWrapper
    {
        public const string LinuxGpgBinaryPath = "/usr/bin/gpg2";

        private readonly string _gpgPath;

        private readonly string _homedirArg;

        public GpgWrapper(string gpgPath, string homedir)
        {
            _gpgPath = gpgPath;

            if (string.IsNullOrEmpty(homedir))
            {
                _homedirArg = string.Empty;
            }
            else
            {
                _homedirArg = string.Format(" --homedir {0} ", homedir);
            }
        }

        public GpgResult EncryptAndSign(string input, out string output, string recpientId, string localUser = null, bool armor = false, string passphrase = null)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                using (var outputStream = new MemoryStream())
                {
                    var result = EncryptAndSign(inputStream, outputStream, recpientId, localUser, armor, passphrase);
                    output = Encoding.UTF8.GetString(outputStream.ToArray());
                    return result;
                }
            }
        }

        public GpgResult EncryptAndSign(Stream input, Stream output, string recpientId, string localUser = null, bool armor = false, string passphrase = null)
        {
            var args = new List<string>();
            args.Add("--encrypt");
            args.Add("--recipient " + recpientId);
            args.Add("--sign");

            if (localUser != null)
            {
                args.Add("--local-user " + localUser);
            }

            if (armor)
            {
                args.Add("--armor");
            }

            if (!string.IsNullOrEmpty(passphrase))
            {
                args.Add("--pinentry-mode loopback --passphrase " + passphrase);
            }

            return Execute(input, output, args.ToArray());
        }

        public GpgResult Encrypt(string input, out string output, string recpientId, bool armor = false)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                using (var outputStream = new MemoryStream())
                {
                    var result = Encrypt(inputStream, outputStream, recpientId, armor);
                    output = Encoding.UTF8.GetString(outputStream.ToArray());
                    return result;
                }
            }
        }

        public GpgResult Encrypt(Stream input, Stream output, string recpientId, bool armor = false)
        {
            return Execute(input, output, "--encrypt", "--recipient " + recpientId, armor ? "--armor" : "");
        }

        public GpgResult Decrypt(string input, out string output, string passphrase = null)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                using (var outputStream = new MemoryStream())
                {
                    var result = Decrypt(inputStream, outputStream, passphrase);
                    output = Encoding.UTF8.GetString(outputStream.ToArray());
                    return result; 
                } 
            }
        }

        public GpgResult Decrypt(Stream input, Stream output, string passphrase = null)
        {
            return Execute(input, output, "--decrypt", string.IsNullOrEmpty(passphrase) ? string.Empty : "--pinentry-mode loopback --passphrase " + passphrase);
        }

        public GpgResult Sign(string input, out string output, string localUser = null, SignatureType type = SignatureType.Sign, bool armor = false, string passphrase = null)
        {
            using (var inputStream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            {
                using (var outputStream = new MemoryStream())
                {
                    var result = Sign(inputStream, outputStream, localUser, type, armor, passphrase);
                    output = Encoding.UTF8.GetString(outputStream.ToArray());
                    return result; 
                } 
            }
        }

        public GpgResult Sign(Stream input, Stream output, string localUser = null, SignatureType type = SignatureType.Sign, bool armor = false, string passphrase = null)
        {
            var args = new List<string>();

            if (localUser != null)
            {
                args.Add("--local-user " + localUser);
            }

            switch (type)
            {
                case SignatureType.Sign:
                    args.Add("--sign");
                    break;
                case SignatureType.ClearSign:
                    args.Add("--clearsign");
                    break;
                case SignatureType.DetachSign:
                    args.Add("--detach-sign");
                    break;
                default:
                    throw new NotSupportedException(); 
            }

            if (armor)
            {
                args.Add("--armor");
            }

            if (!string.IsNullOrEmpty(passphrase))
            {
                args.Add("--pinentry-mode loopback --passphrase " + passphrase);
            }

            return Execute(input, output, args.ToArray());
        }

        public GpgResult Verify(Stream input, Stream output)
        {
            return Execute(input, output, "--verify");
        }

        private GpgResult Execute(Stream input, Stream output, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + string.Join(" ", arguments));
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);

            var inputWriter = new Thread(() =>
                {
                    var inputBuffer = new byte[128];
                    var inputLength = 0;

                    do
                    {
                        inputLength = input.Read(inputBuffer, 0, inputBuffer.Length);
                        process.StandardInput.BaseStream.Write(inputBuffer, 0, inputLength);
                    }
                    while (inputLength > 0);

                    process.StandardInput.Close();
                });

            inputWriter.Start();

            var outputBuffer = new byte[128];
            var outputLength = 0;

            do
            {
                outputLength = process.StandardOutput.BaseStream.Read(outputBuffer, 0, outputBuffer.Length);
                output.Write(outputBuffer, 0, outputLength);
            }
            while (outputLength > 0);

            process.WaitForExit();
            var text = process.StandardError.ReadToEnd();

            if (process.ExitCode != 0)
            {
                return ParseBadResult(text);
            }
            else
            {
                return ParseGoodResult(text);
            }
        }

        private GpgResult ParseGoodResult(string text)
        {
            if (text.Contains("gpg: Good signature from"))
            {
                var matches = Regex.Matches(text.Replace("\n", " "), @"gpg\: Signature made [a-zA-Z0-9 \:]+ using [A-Z]+ key (?:ID ){0,1}([0-9A-F]+)");

                if (matches.Count == 1)
                {
                    var signer = matches[0].Groups[1].Value;
                    return new GpgResult(GpgStatus.Success, signer);
                }
                else
                {
                    return new GpgResult(GpgStatus.Success, null);
                }
            }
            else
            {
                return new GpgResult(GpgStatus.Success, null);
            }
        }

        private GpgResult ParseBadResult(string text)
        {
            if (text.Contains("gpg: cancelled by user"))
            {
                return new GpgResult(GpgStatus.Canceled, null);
            }
            else if (text.Contains("gpg: decryption failed: No secret key"))
            {
                return new GpgResult(GpgStatus.NoSecretKey, null);
            }
            else if (text.Contains("gpg: [stdin]: encryption failed: No public key"))
            {
                return new GpgResult(GpgStatus.NoPublicKey, null);
            }
            else if (text.Contains("gpg: [stdin]: encryption failed: Unusable public key"))
            {
                return new GpgResult(GpgStatus.NoPublicKey, null);
            }
            else
            {
                return new GpgResult(GpgStatus.Error, null);
            }
        }

        public IEnumerable<GpgKey> ImportKeys(string path)
        {
            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + "--batch --import " + path);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);
            process.WaitForExit();

            return process.StandardError.ReadToEnd()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => GetMatchGroup(l, "^gpg: key ([0-9A-F]+).*$", 1))
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .Select(k => List(k).FirstOrDefault())
                .Where(k => k != null);
        }

        public IEnumerable<GpgKey> ImportKeys(byte[] data)
        {
            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + "--batch --import");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);
            process.StandardInput.BaseStream.Write(data, 0, data.Length);
            process.StandardInput.Close();
            process.WaitForExit();

            return process.StandardError.ReadToEnd()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => GetMatchGroup(l, "^gpg: key ([0-9A-F]+).*$", 1))
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .Select(k => List(k).FirstOrDefault())
                .Where(k => k != null);
        }

        private static string GetMatchGroup(string input, string pattern, int group)
        {
            var match = Regex.Match(input, pattern);

            if (match.Success && match.Groups.Count > group)
            {
                return match.Groups[group].Value;
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<string> GetKeygrips(string fingerprint)
        {
            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + "--with-keygrip -K " + fingerprint );
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);
            process.WaitForExit();

            var outputText = process.StandardOutput.ReadToEnd();
            var errorText = process.StandardError.ReadToEnd();

            var matchesGrips = Regex.Matches(outputText, @"^ +Keygrip = ([A-F0-9]+)$", RegexOptions.Multiline);

            if (matchesGrips.Count < 1)
            {
                throw new GpgException("Cannot determine keygrips.");
            }

            foreach (Match match in matchesGrips)
            {
                yield return match.Groups[1].Value;
            }
        }

        public string ExportKey(string keySelect, bool armor = true)
        {
            var args = new List<string>();
            if (armor) args.Add("--armor");
            args.Add("--export");
            if (!string.IsNullOrEmpty(keySelect)) args.Add(keySelect);

            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + string.Join(" ", args));
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);
            process.WaitForExit();

            var outputText = process.StandardOutput.ReadToEnd();
            var errorText = process.StandardError.ReadToEnd();

            if (errorText.Trim() != string.Empty)
            {
                throw new GpgException("Error on key export: " + errorText);
            }

            return outputText;
        }

        public byte[] ExportKeyBinary(string keySelect)
        {
            var args = new List<string>();
            args.Add("--export");
            if (!string.IsNullOrEmpty(keySelect)) args.Add(keySelect);

            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + string.Join(" ", args));
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);
            process.WaitForExit();

            using (var output = new MemoryStream())
            {
                var buffer = new byte[1024];
                var count = 1;

                while (count > 0)
                {
                    count = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);
                    output.Write(buffer, 0, count);
                }

                var errorText = process.StandardError.ReadToEnd();

                if (errorText.Trim() != string.Empty)
                {
                    throw new GpgException("Error on key export: " + errorText);
                }

                return output.ToArray();
            }
        }

        private static void SetLanguageEnglish(ProcessStartInfo startInfo)
        {
            if (startInfo.EnvironmentVariables.ContainsKey("Lang"))
            {
                startInfo.EnvironmentVariables["Lang"] = "en";
            }
            else
            {
                startInfo.EnvironmentVariables.Add("Lang", "en");
            }
        }

        public IEnumerable<GpgKey> ImportKey(string keyData)
        {
            return ImportKeys(Encoding.UTF8.GetBytes(keyData));
        }

        public GpgCardStatusResult CardStatus()
        {
            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + "--card-status");
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var start = DateTime.Now;
            var process = Process.Start(startInfo);

            while (!process.HasExited)
            {
                if (DateTime.Now.Subtract(start).TotalSeconds > 5d)
                {
                    process.Kill();
                    return new GpgCardStatusResult(GpgCardStatus.Error);
                }

                Thread.Sleep(50);
            }

            var outputText = process.StandardOutput.ReadToEnd();
            var errorText = process.StandardError.ReadToEnd();

            var matchCounter = Regex.Match(outputText, @"^PIN retry counter : (\d) (\d) (\d)$", RegexOptions.Multiline);
            var matchKey = Regex.Match(outputText, @"^Signature key \.+: ([0-9A-F]{4} [0-9A-F]{4} [0-9A-F]{4} [0-9A-F]{4} [0-9A-F]{4}  [0-9A-F]{4} [0-9A-F]{4} [0-9A-F]{4} [0-9A-F]{4} [0-9A-F]{4})$", RegexOptions.Multiline);
            var matchActualCardNo = Regex.Match(outputText, @"^Application ID \.+: [A-F0-9]{16}([A-F0-9]{12})[A-F0-9]{4}$", RegexOptions.Multiline);
            var matchExpectedCardNo = Regex.Match(outputText, @"^ +card-no: ([A-F0-9]{4} [A-F0-9]{8})$", RegexOptions.Multiline);

            if (matchCounter.Success && matchKey.Success && matchActualCardNo.Success)
            {
                var fingerprint = matchKey.Groups[1].Value.Replace(" ", "");
                var retryCounterPin = int.Parse(matchCounter.Groups[1].Value);
                var retryCounterReset = int.Parse(matchCounter.Groups[2].Value);
                var retryCounterAdmin = int.Parse(matchCounter.Groups[3].Value);
                var actualCardNo = matchActualCardNo.Groups[1].Value;
                var expectedCardNo = matchExpectedCardNo.Groups[1].Value.Replace(" ", "");
                GpgCardStatus status;

                if (retryCounterPin > 0)
                {
                    status = GpgCardStatus.Present;
                }
                else if (retryCounterReset > 0)
                {
                    status = GpgCardStatus.PinLocked;
                }
                else if (retryCounterAdmin > 0)
                {
                    status = GpgCardStatus.ResetLocked;
                }
                else
                {
                    status = GpgCardStatus.AdminLocked;
                }

                return new GpgCardStatusResult(status, fingerprint, actualCardNo, expectedCardNo);
            }
            else
            {
                if (errorText.Contains(@"gpg: selecting openpgp failed: Card not present"))
                {
                    return new GpgCardStatusResult(GpgCardStatus.CardMissing);
                }
                else if (errorText.Contains(@"gpg: selecting openpgp failed: Card error"))
                {
                    return new GpgCardStatusResult(GpgCardStatus.CardError);
                }
                else if (errorText.Contains(@"gpg: selecting openpgp failed: Broken pipe"))
                {
                    return new GpgCardStatusResult(GpgCardStatus.BrokenPipe);
                }
                else
                {
                    return new GpgCardStatusResult(GpgCardStatus.Error);
                }
            }
        }

        public void AddUidToKey(string keyId, string name, string mail)
        {

            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + string.Format("--quick-adduid {0} '{1} <{2}>'", keyId, name, mail));
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            if (startInfo.EnvironmentVariables.ContainsKey("Lang"))
            {
                startInfo.EnvironmentVariables["Lang"] = "en";
            }
            else
            {
                startInfo.EnvironmentVariables.Add("Lang", "en");
            }

            var process = Process.Start(startInfo);

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new GpgException("Error adding uid to key: " + process.StandardError.ReadToEnd());
            }
        }

        public string GenerateKey(IEnumerable<Tuple<string, string>> uids)
        {
            if (uids == null || uids.Count() < 1)
            {
                throw new ArgumentException("uids cannot be null or empty");
            }

            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + "--batch --gen-key");
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);

            process.StandardInput.WriteLine("%echo Generating key...");
            process.StandardInput.WriteLine("Key-Type: 1");
            process.StandardInput.WriteLine("Key-Length: 4096");
            process.StandardInput.WriteLine("Subkey-Type: 1");
            process.StandardInput.WriteLine("Subkey-Length: 4096");
            process.StandardInput.WriteLine("Name-Real: " + uids.ElementAt(0).Item1);
            process.StandardInput.WriteLine("Name-Email: " + uids.ElementAt(0).Item2);
            process.StandardInput.WriteLine("Expire-Date: 0");
            process.StandardInput.WriteLine("%no-protection");
            process.StandardInput.WriteLine("%commit");
            process.StandardInput.WriteLine("%echo Key created successfully.");
            process.StandardInput.Close();

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                var output = process.StandardError.ReadToEnd();

                if (output.Contains("Key created successfully."))
                {
                    var match = Regex.Match(output, "key ([0-9A-F]+) marked as ultimately trusted");

                    if (match.Success)
                    {
                        var keyId = match.Groups[1].Value;

                        foreach (var uid in uids.Skip(1))
                        {
                            AddUidToKey(keyId, uid.Item1, uid.Item2);
                        }

                        return keyId;
                    }
                    else
                    {
                        throw new GpgException("Cannot parse output of key generation: " + output);
                    }
                }
                else
                {
                    throw new GpgException("Cannot parse output of key generation: " + output);
                }
            }
            else
            {
                throw new GpgException("Error in key generation: " + process.StandardError.ReadToEnd());
            }
        }

        public GpgTrust ParseTrust(string text)
        {
            switch (text.Trim())
            {
                case "ultimate":
                    return GpgTrust.Ultimate;
                case "full":
                    return GpgTrust.Full;
                case "marginal":
                    return GpgTrust.Marginal;
                case "revoked":
                    return GpgTrust.Revoked;
                case "expired":
                    return GpgTrust.Expired;
                default:
                    return GpgTrust.Unknown;
            }
        }

        public GpgKeyType ParseKeyType(string type)
        {
            switch (type)
            {
                case "rsa":
                    return GpgKeyType.RSA;
                case "dsa":
                    return GpgKeyType.DSA;
                case "elg":
                    return GpgKeyType.ELG;
                default:
                    return GpgKeyType.Unknown;
            }
        }

        public GpgKeyStatus ParseKeyStatus(string status)
        {
            switch (status)
            {
                case "expired":
                    return GpgKeyStatus.Expired;
                case "revoked":
                    return GpgKeyStatus.Revoked;
                case "expires":
                case "":
                    return GpgKeyStatus.Active;
                default:
                    return GpgKeyStatus.Unknown;
            }
        }

        public GpgKeyUsage ParseUsage(string text)
        {
            GpgKeyUsage usage = GpgKeyUsage.None;

            foreach (char c in text)
            {
                switch (c)
                {
                    case 'C':
                        usage |= GpgKeyUsage.Certify;
                        break;
                    case 'S':
                        usage |= GpgKeyUsage.Sign;
                        break;
                    case 'E':
                        usage |= GpgKeyUsage.Encrypt;
                        break;
                    case 'A':
                        usage |= GpgKeyUsage.Authenticate;
                        break;
                    default:
                        break;
                }
            }

            return usage;
        }

        public static string PrintKeyUsage(GpgKeyUsage usage)
        {
            string text = string.Empty;

            if (usage.HasFlag(GpgKeyUsage.Certify))
            {
                text += "C";
            }

            if (usage.HasFlag(GpgKeyUsage.Sign))
            {
                text += "S";
            }


            if (usage.HasFlag(GpgKeyUsage.Encrypt))
            {
                text += "E";
            }


            if (usage.HasFlag(GpgKeyUsage.Authenticate))
            {
                text += "A";
            }

            return text;
        }

        public IEnumerable<GpgKey> List(string keySelect = null, bool privateKeysOnly = false)
        {
            var args = new List<string>();
            args.Add(privateKeysOnly ? "-K" : "-k");
            if (!string.IsNullOrEmpty(keySelect)) args.Add(keySelect);

            var startInfo = new ProcessStartInfo(_gpgPath, _homedirArg + string.Join(" ", args));
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);

            var output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();

            var lines = new Queue<string>(output.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            GpgKey key = null;

            while (lines.Count > 0)
            {
                var line = lines.Dequeue();

                if (line.StartsWith("/") && line.EndsWith(".gpg")) { } // ignore line
                else if (line.StartsWith("------") && line.EndsWith("------")) { } // ignore line
                else if (line.StartsWith("sec") || line.StartsWith("pub"))
                {
                    var match = Regex.Match(line, @"^(?:(?:sec )|(?:sec> )|(?:pub )) +([a-z]+)([0-9]+) ([0-9]{4})-([0-9]{2})-([0-9]{2}) \[([A-Z]+)\](?: \[([a-z]+): ([0-9]{4})-([0-9]{2})-([0-9]{2})\])?$");

                    if (match.Success)
                    {
                        var type = ParseKeyType(match.Groups[1].Value);
                        var bits = int.Parse(match.Groups[2].Value);
                        var createdYear = int.Parse(match.Groups[3].Value);
                        var createdMonth = int.Parse(match.Groups[4].Value);
                        var createdDay = int.Parse(match.Groups[5].Value);
                        var created = new DateTime(createdYear, createdMonth, createdDay);
                        var usage = ParseUsage(match.Groups[6].Value);
                        var id = lines.Dequeue().Trim();
                        var expiry = DateTime.MaxValue;
                        var status = GpgKeyStatus.Active;

                        if ((match.Groups.Count >= 11) &&
                            (match.Groups[7].Value != null) &&
                            (match.Groups[7].Value != string.Empty))
                        {
                            status = ParseKeyStatus(match.Groups[7].Value);
                            var expiryYear = int.Parse(match.Groups[8].Value);
                            var expiryMonth = int.Parse(match.Groups[9].Value);
                            var expiryDay = int.Parse(match.Groups[10].Value);
                            expiry = new DateTime(expiryYear, expiryMonth, expiryDay);
                        }

                        key = new GpgKey(id, type, bits, created, usage, expiry, status);
                    }
                    else
                    {
                        throw new GpgException("Cannot parse list line: " + line);
                    }
                }
                else if (line.StartsWith("uid"))
                {
                    if (line.Contains("jpeg image of size"))
                    {
                        continue;
                    }

                    var match = Regex.Match(line, @"^uid +\[([a-z ]+)\] (.+?)(?: \<([a-zA-Z0-9\.\-_]+@[a-zA-Z0-9\.\-]+)\>){0,1}$");

                    if (match.Success)
                    {
                        var trust = ParseTrust(match.Groups[1].Value);
                        var name = match.Groups[2].Value;
                        var mail = match.Groups[3].Value;

                        var uid = new GpgUid(name, mail, trust);

                        if (key == null)
                        {
                            throw new GpgException("Key null when parsing sub.");
                        }

                        key.Uids.Add(uid);
                    }
                    else
                    {
                        throw new GpgException("Cannot parse list line: " + line);
                    }
                }
                else if (line.StartsWith("sub") || line.StartsWith("ssb"))
                {
                    var match = Regex.Match(line, @"^(?:(?:sub )|(?:ssb )|(?:ssb> )) +([a-z]+)([0-9]+) ([0-9]{4})-([0-9]{2})-([0-9]{2}) \[([A-Z]*)\](?: \[([a-z]+): ([0-9]{4})-([0-9]{2})-([0-9]{2})\])?$");

                    if (match.Success)
                    {
                        var type = ParseKeyType(match.Groups[1].Value);
                        var bits = int.Parse(match.Groups[2].Value);
                        var createdYear = int.Parse(match.Groups[3].Value);
                        var createdMonth = int.Parse(match.Groups[4].Value);
                        var createdDay = int.Parse(match.Groups[5].Value);
                        var created = new DateTime(createdYear, createdMonth, createdDay);
                        var usage = ParseUsage(match.Groups[6].Value);
                        var expiry = DateTime.MaxValue;
                        var status = GpgKeyStatus.Active;

                        if ((match.Groups.Count >= 11) &&
                            (match.Groups[7].Value != null) &&
                            (match.Groups[7].Value != string.Empty))
                        {
                            status = ParseKeyStatus(match.Groups[7].Value);
                            var expiryYear = int.Parse(match.Groups[8].Value);
                            var expiryMonth = int.Parse(match.Groups[9].Value);
                            var expiryDay = int.Parse(match.Groups[10].Value);
                            expiry = new DateTime(expiryYear, expiryMonth, expiryDay);
                        }

                        var sub = new GpgSub(type, bits, created, usage, expiry, status);

                        if (key == null)
                        {
                            throw new GpgException("Key null when parsing sub.");
                        }

                        key.Subs.Add(sub);
                    }
                    else
                    {
                        throw new GpgException("Cannot parse list line: " + line);
                    }
                }
                else if (line.Trim() == string.Empty)
                {
                    if (key != null)
                    {
                        yield return key;
                        key = null;
                    }
                }
                else if (line.Trim().StartsWith("Card serial no"))
                {
                    // ignore that
                }
                else
                {
                    throw new GpgException("Cannot parse list line: " + line);
                }
            }
        }
    }
}

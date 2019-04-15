using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Text.RegularExpressions;

namespace BaseLibrary
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
            return string.Format("sub {0}/{1} {2} {3} {4} {5}", Type, Bits, Created, Gpg.PrintKeyUsage(Usage), Expiry, Status);
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
            return string.Format("{0} {1}/{2} {3} {4} {5} {6}", Id, Type, Bits, Created, Gpg.PrintKeyUsage(Usage), Expiry, Status);
        }
    }

    public class LocalGpg : Gpg
    {
        public const string LinuxGpgBinaryPath = "/usr/bin/gpg2";

        private readonly string _gpgPath;

        private readonly string _homedirArg;

        public LocalGpg(string gpgPath, string homedir)
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

        public override Tuple<int, string> Execute(Stream input, Stream output, string passphrase, IEnumerable<string> arguments)
        {
            var argumentsText = _homedirArg + string.Join(" ", arguments);

            if (!string.IsNullOrEmpty(passphrase))
            {
                argumentsText += " --pinentry-mode loopback --passphrase " + passphrase;
            }

            var startInfo = new ProcessStartInfo(_gpgPath, argumentsText);
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardInput = true;
            startInfo.RedirectStandardError = true;
            startInfo.CreateNoWindow = true;
            SetLanguageEnglish(startInfo);

            var process = Process.Start(startInfo);

            var inputWriter = new Thread(() =>
            {
                if (input != null)
                {
                    var inputBuffer = new byte[128];
                    var inputLength = 0;

                    do
                    {
                        inputLength = input.Read(inputBuffer, 0, inputBuffer.Length);
                        process.StandardInput.BaseStream.Write(inputBuffer, 0, inputLength);
                    }
                    while (inputLength > 0);
                }

                process.StandardInput.Close();
            });

            inputWriter.Start();

            var outputBuffer = new byte[128];
            var outputLength = 0;

            do
            {
                outputLength = process.StandardOutput.BaseStream.Read(outputBuffer, 0, outputBuffer.Length);

                if (output != null)
                {
                    output.Write(outputBuffer, 0, outputLength);
                }
            }
            while (outputLength > 0);

            process.WaitForExit();
            var text = process.StandardError.ReadToEnd();

            return new Tuple<int, string>(process.ExitCode, text);
        }
    }

    public abstract class Gpg
    {
        public abstract Tuple<int, string> Execute(Stream input, Stream output, string passphrase, IEnumerable<string> arguments);

        public Tuple<int, string> Execute(Stream input, Stream output, string passphrase, params string[] arguments)
        {
            return Execute(input, output, passphrase, arguments as IEnumerable<string>);
        }

        public Tuple<int, string, byte[]> Execute(Stream input, IEnumerable<string> arguments)
        {
            using (var outputStream = new MemoryStream())
            {
                var result = Execute(input, outputStream, null, arguments);
                return new Tuple<int, string, byte[]>(result.Item1, result.Item2, outputStream.ToArray());
            }
        }

        public Tuple<int, string, byte[]> ExecuteWithPassphrase(Stream input, string passphrase, IEnumerable<string> arguments)
        {
            using (var outputStream = new MemoryStream())
            {
                var result = Execute(input, outputStream, passphrase, arguments);
                return new Tuple<int, string, byte[]>(result.Item1, result.Item2, outputStream.ToArray());
            }
        }

        public Tuple<int, string, byte[]> Execute(Stream input, string[] arguments)
        {
            return Execute(input, arguments as IEnumerable<string>);
        }

        public Tuple<int, string, byte[]> Execute(byte[] input, IEnumerable<string> arguments)
        {
            if (input != null)
            {
                using (var inputStream = new MemoryStream(input))
                {
                    return Execute(inputStream, arguments);
                }
            }
            else
            {
                return Execute(null as Stream, arguments);
            }
        }

        public Tuple<int, string, byte[]> ExecuteWithPassphrase(byte[] input, string passphrase, IEnumerable<string> arguments)
        {
            if (input != null)
            {
                using (var inputStream = new MemoryStream(input))
                {
                    return ExecuteWithPassphrase(inputStream, passphrase, arguments);
                }
            }
            else
            {
                return ExecuteWithPassphrase(null as Stream, passphrase, arguments);
            }
        }

        public Tuple<int, string, byte[]> Execute(byte[] input, params string[] arguments)
        {
            return Execute(input, arguments as IEnumerable<string>);
        }

        public Tuple<int, string, byte[]> ExecuteWithPassphrase(byte[] input, string passphrase, params string[] arguments)
        {
            return ExecuteWithPassphrase(input, passphrase, arguments as IEnumerable<string>);
        }

        public Tuple<int, string, string> Execute(string input, IEnumerable<string> arguments)
        {
            if (input != null)
            {
                var result = Execute(Encoding.UTF8.GetBytes(input), arguments);
                return new Tuple<int, string, string>(result.Item1, result.Item2, Encoding.UTF8.GetString(result.Item3));
            }
            else
            {
                var result = Execute(null as byte[], arguments);
                return new Tuple<int, string, string>(result.Item1, result.Item2, Encoding.UTF8.GetString(result.Item3));
            }
        }

        public Tuple<int, string, string> Execute(string input, params string[] arguments)
        {
            return Execute(input, arguments as IEnumerable<string>);
        }

        public Tuple<int, string, string> Execute(IEnumerable<string> arguments)
        {
            var result = Execute(null as byte[], arguments);
            return new Tuple<int, string, string>(result.Item1, result.Item2, Encoding.UTF8.GetString(result.Item3));
        }

        public Tuple<int, string, string> Execute(params string[] arguments)
        {
            return Execute(arguments as IEnumerable<string>); 
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

        public GpgResult EncryptAndSign(byte[] input, out byte[] output, string recpientId, string localUser = null, bool armor = false, string passphrase = null)
        {
            using (var inputStream = new MemoryStream(input))
            {
                using (var outputStream = new MemoryStream())
                {
                    var result = EncryptAndSign(inputStream, outputStream, recpientId, localUser, armor, passphrase);
                    output = outputStream.ToArray();
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

            return ExecuteResult(input, output, passphrase, args.ToArray());
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
            return ExecuteResult(input, output, null, "--encrypt", "--recipient " + recpientId, armor ? "--armor" : "");
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
            return ExecuteResult(input, output, null, "--decrypt", string.IsNullOrEmpty(passphrase) ? string.Empty : "--pinentry-mode loopback --passphrase " + passphrase);
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

        public GpgResult Sign(byte[] input, out byte[] output, string localUser = null, SignatureType type = SignatureType.Sign, bool armor = false, string passphrase = null)
        {
            using (var inputStream = new MemoryStream(input))
            {
                using (var outputStream = new MemoryStream())
                {
                    var result = Sign(inputStream, outputStream, localUser, type, armor, passphrase);
                    output = outputStream.ToArray();
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

            return ExecuteResult(input, output, passphrase, args.ToArray());
        }

        public GpgResult Verify(Stream input, Stream output)
        {
            return ExecuteResult(input, output, null, "--verify");
        }

        private GpgResult ExecuteResult(Stream input, Stream output, string password, params string[] arguments)
        {
            var result = Execute(input, output, password, arguments);

            if (result.Item1 != 0)
            {
                return ParseBadResult(result.Item2);
            }
            else
            {
                return ParseGoodResult(result.Item2);
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
            var result = Execute("--batch --import " + path);

            return result.Item2
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => GetMatchGroup(l, "^gpg: key ([0-9A-F]+).*$", 1))
                .Where(k => !string.IsNullOrEmpty(k))
                .Distinct()
                .Select(k => List(k).FirstOrDefault())
                .Where(k => k != null);
        }

        public IEnumerable<GpgKey> ImportKeys(byte[] data)
        {
            var result = Execute(data, "--batch --import");

            return result.Item2
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
            var result = Execute("--with-keygrip --list-secret-keys " + fingerprint);
            var outputText = result.Item3;

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

            var result = Execute(args);
            var outputText = result.Item3;

            if (result.Item2.Trim() != string.Empty)
            {
                throw new GpgException("Error on key export: " + result.Item2);
            }

            return outputText;
        }

        public byte[] ExportKeyBinary(string keySelect)
        {
            var args = new List<string>();
            args.Add("--export");
            if (!string.IsNullOrEmpty(keySelect)) args.Add(keySelect);

            using (var output = new MemoryStream())
            {
                var result = Execute(null, output, null, args);

                if (result.Item2.Trim() != string.Empty)
                {
                    throw new GpgException("Error on key export: " + result.Item2);
                }

                return output.ToArray();
            }
        }

        public IEnumerable<GpgKey> ImportKey(string keyData)
        {
            return ImportKeys(Encoding.UTF8.GetBytes(keyData));
        }

        public GpgCardStatusResult CardStatus()
        {
            var result = Execute("--card-status");
            var outputText = result.Item3;
            var errorText = result.Item2;

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
            var result = Execute(string.Format("--quick-adduid {0} '{1} <{2}>'", keyId, name, mail));

            if (result.Item1 != 0)
            {
                throw new GpgException("Error adding uid to key: " + result.Item2);
            }
        }

        public string GenerateKey(IEnumerable<Tuple<string, string>> uids)
        {
            if (uids == null || !uids.Any())
            {
                throw new ArgumentException("uids cannot be null or empty");
            }

            using (var input = new MemoryStream())
            {
                var writer = new StreamWriter(input);

                writer.WriteLine("%echo Generating key...");
                writer.WriteLine("Key-Type: 1");
                writer.WriteLine("Key-Length: 4096");
                writer.WriteLine("Subkey-Type: 1");
                writer.WriteLine("Subkey-Length: 4096");
                writer.WriteLine("Name-Real: " + uids.ElementAt(0).Item1);
                writer.WriteLine("Name-Email: " + uids.ElementAt(0).Item2);
                writer.WriteLine("Expire-Date: 0");
                writer.WriteLine("%no-protection");
                writer.WriteLine("%commit");
                writer.WriteLine("%echo Key created successfully.");

                var result = Execute("--batch --gen-key");

                if (result.Item1 == 0)
                {
                    var output = result.Item2;

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
                    throw new GpgException("Error in key generation: " + result.Item2);
                }
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
            args.Add(privateKeysOnly ? "--list-secret-keys" : "--list-keys");
            if (!string.IsNullOrEmpty(keySelect)) args.Add(keySelect);

            var result = Execute(args);
            var outputText = result.Item3;

            var lines = new Queue<string>(outputText.Split(new string[] { Environment.NewLine }, StringSplitOptions.None));
            GpgKey key = null;

            while (lines.Count > 0)
            {
                var line = lines.Dequeue();

                if (line.StartsWith("/", StringComparison.Ordinal) && line.EndsWith(".gpg", StringComparison.Ordinal)) { } // ignore line
                else if (line.StartsWith("------", StringComparison.Ordinal) && line.EndsWith("------", StringComparison.Ordinal)) { } // ignore line
                else if (line.StartsWith("sec", StringComparison.Ordinal) || line.StartsWith("pub", StringComparison.Ordinal))
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
                else if (line.StartsWith("uid", StringComparison.Ordinal))
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
                else if (line.StartsWith("sub", StringComparison.Ordinal) || line.StartsWith("ssb", StringComparison.Ordinal))
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
                else if (line.Trim().StartsWith("Card serial no", StringComparison.Ordinal))
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
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using Newtonsoft.Json.Linq;
using BaseLibrary;
using SecurityServiceClient;

namespace SecurityService
{
    public class SecurityService : SecureChannel.SecureService
    {
        private Logger _logger;
        private readonly byte[] _secretKey;
        private Gpg _gpg;

        public SecurityService(Logger logger, Gpg gpg, byte[] presharedKey, byte[] secretKey)
            : base(presharedKey)
        {
            _logger = logger;
            _gpg = gpg;
            _secretKey = secretKey;
        }

        protected override void Error(string text, params string[] arguments)
        {
            _logger.Error(text, arguments);
        }

        protected override void Info(string text, params string[] arguments)
        {
            _logger.Info(text, arguments);
        }

        protected override JObject Process(JObject request)
        {
            var command = request.ValueString(SecurityServiceProtocol.CommandProperty);
            var reply = new JObject();
            reply.AddProperty(SecurityServiceProtocol.CommandProperty, command);

            try
            {
                switch (command)
                {
                    case SecurityServiceProtocol.CommandSecurePassword:
                        SecurePassword(request, reply);
                        break;
                    case SecurityServiceProtocol.CommandVerifyPassword:
                        VerifyPassword(request, reply);
                        break;
                    case SecurityServiceProtocol.CommandSecureTotp:
                        SecureTotp(request, reply);
                        break;
                    case SecurityServiceProtocol.CommandVerifyTotp:
                        VerifyTotp(request, reply);
                        break;
                    case SecurityServiceProtocol.CommandSecureGpgPassphrase:
                        SecureGpgPassphrase(request, reply);
                        break;
                    case SecurityServiceProtocol.CommandExecuteGpg:
                        ExecuteGpg(request, reply);
                        break;
                    default:
                        reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultErrorUnknownCommand);
                        break;
                }
            }
            catch (Exception exception)
            {
                _logger.Error("Execution error at command {0}: {1}", command, exception.Message);
                reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultErrorMalFormedRequestData);
            }

            return reply;
        }

        private Tuple<string, Guid> GetPassphrase(JObject request)
        {
            var encryptedPassphraseData = request.ValueBytes(SecurityServiceProtocol.PassphraseDataProperty, null);

            if (encryptedPassphraseData == null)
            {
                return null;
            }
            else if (encryptedPassphraseData.Length == 0)
            {
                return new Tuple<string, Guid>(Global.Config.SystemMailGpgKeyPassphrase, Guid.Empty);
            }
            else
            {
                var encryptedPassphrase = new Encrypted<GpgPasssphraseObject>(encryptedPassphraseData);
                var passphraseObject = encryptedPassphrase.Decrypt(_secretKey);
                return new Tuple<string, Guid>(passphraseObject.Passphrase, passphraseObject.Id);
            }
        }

        private static string EscapeRegex(string input)
        {
            return input
                .Replace(@"\", @"\\")
                .Replace(@"/", @"\/")
                .Replace(@".", @"\.")
                .Replace(@"*", @"\*")
                .Replace(@"+", @"\+")
                .Replace(@"?", @"\?")
                .Replace(@"|", @"\|")
                .Replace(@"(", @"\)")
                .Replace(@"[", @"\]")
                .Replace(@"{", @"\}")
                .Replace(@"-", @"\-");
        }

        private const string KeyIdRegex = @"[a-fA-F0-9]+";
        private const string OptionalKeyIdRegex = @"(?: [a-fA-F0-9]+)?";

        private static IEnumerable<string> AllowedArgumentRegexes
        {
            get 
            {
                yield return EscapeRegex("--sign");
                yield return EscapeRegex("--detach-sign");
                yield return EscapeRegex("--encrypt");
                yield return EscapeRegex("--decrypt");
                yield return EscapeRegex("--verify");
                yield return EscapeRegex("--list-keys") + OptionalKeyIdRegex;
                yield return EscapeRegex("--list-signatures") + OptionalKeyIdRegex;
                yield return EscapeRegex("--list-secret-keys") + OptionalKeyIdRegex;
                yield return EscapeRegex("--fingerprint") + OptionalKeyIdRegex;
                yield return EscapeRegex("--armor");
                yield return EscapeRegex("--recipient") + OptionalKeyIdRegex;
                yield return EscapeRegex("--local-user") + OptionalKeyIdRegex;
                yield return EscapeRegex("--local-user SYSTEM_MAIL_KEY_ID");
                yield return EscapeRegex("--export") + OptionalKeyIdRegex;
                yield return EscapeRegex("--import");
                yield return EscapeRegex("--batch");
            }
        }

        private void CheckArgument(string argument)
        {
            argument = argument.Trim();

            if (argument != string.Empty)
            {
                argument = "--" + argument;

                foreach (var pattern in AllowedArgumentRegexes)
                {
                    if (Regex.IsMatch(argument, "^" + pattern + "$"))
                    {
                        return;
                    }
                }

                throw new InvalidOperationException("Argument now allowed: " + argument);
            }
        }

        private void ArgumentFilter(string arguments)
        {
            arguments = " " + arguments;
            var argumentList = arguments.Split(new string[] { " --" }, StringSplitOptions.None);

            foreach (var argument in argumentList)
            {
                CheckArgument(argument);
            }
        }

        private void ExecuteGpg(JObject request, JObject reply)
        {
            var arguments = request.ValueString(SecurityServiceProtocol.ArgumentsProperty);
            _logger.Info("Executing GPG {0}", arguments);

            ArgumentFilter(arguments);
            var passphraseResult = GetPassphrase(request);

            if (passphraseResult != null && 
                passphraseResult.Item2.Equals(Guid.Empty) && 
                !arguments.Contains(GpgPrivateKeyInfo.SystemMailKeyId))
            {
                throw new InvalidOperationException("Usage of system mail key without usage of system mail key id"); 
            }
            else if ((passphraseResult == null ||
                     !passphraseResult.Item2.Equals(Guid.Empty)) &&
                     arguments.Contains(GpgPrivateKeyInfo.SystemMailKeyId))
            {
                throw new InvalidOperationException("Usage of system mail key id without system mail key statement");
            }

            arguments = arguments.Replace(GpgPrivateKeyInfo.SystemMailKeyId, Global.Config.SystemMailGpgKeyId);
            var input = request.ValueBytes(SecurityServiceProtocol.InputDataProperty, null);
            var result = _gpg.ExecuteWithPassphrase(input, passphraseResult?.Item1, arguments);
            reply.AddProperty(SecurityServiceProtocol.ExitCodeProperty, result.Item1);
            reply.AddProperty(SecurityServiceProtocol.OutputDataProperty, result.Item3);
            reply.AddProperty(SecurityServiceProtocol.ErrorDataProperty, Encoding.UTF8.GetBytes(result.Item2));
            reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultSuccess);

            if (passphraseResult?.Item1 != null)
            {
                _logger.Info("Executed GPG {0} width passphrase {1}", arguments, passphraseResult.Item2);
            }
            else
            {
                _logger.Info("Executed GPG {0}", arguments);
            }
        }

        private void SecureGpgPassphrase(JObject request, JObject reply)
        {
            var passphrase = request.ValueString(SecurityServiceProtocol.PassphraseProperty);
            var passphraseObject = new GpgPasssphraseObject(passphrase);
            var encryptedPassphrase = new Encrypted<GpgPasssphraseObject>(passphraseObject, _secretKey);
            reply.AddProperty(SecurityServiceProtocol.PassphraseDataProperty, Convert.ToBase64String(encryptedPassphrase.ToBinary()));
            reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultSuccess);
            _logger.Info("Secured new GPG passphrase {0}", passphraseObject.Id);
        }

        private void SecurePassword(JObject request, JObject reply)
        {
            var password = request.ValueString(SecurityServiceProtocol.PasswordProperty);
            var passwordObject = new PasswordObject(password);
            var encryptedPassword = new Encrypted<PasswordObject>(passwordObject, _secretKey);
            reply.AddProperty(SecurityServiceProtocol.PasswordDataProperty, encryptedPassword.ToBinary());
            reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultSuccess);
            _logger.Info("Secured new password {0}", passwordObject.Id);
        }

        private void SecureTotp(JObject request, JObject reply)
        {
            var secret = request.ValueBytes(SecurityServiceProtocol.SecretProperty);
            var totpObject = new TotpObject(secret);
            var encryptedTotp = new Encrypted<TotpObject>(totpObject, _secretKey);
            reply.AddProperty(SecurityServiceProtocol.TotpDataProperty, encryptedTotp.ToBinary());
            reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultSuccess);
            _logger.Info("Secured new TOTP secret {0}", totpObject.Id);
        }

        private void VerifyTotp(JObject request, JObject reply)
        {
            var code = request.ValueString(SecurityServiceProtocol.CodeProperty);
            var encryptedTotpData = request.ValueBytes(SecurityServiceProtocol.TotpDataProperty);
            var encryptedTotp = new Encrypted<TotpObject>(encryptedTotpData);
            var totpObject = encryptedTotp.Decrypt(_secretKey);
            Global.Throttle.Check(totpObject.Id.ToString(), SecurityThrottleType.Totp);

            if (totpObject.Verify(code))
            {
                reply.AddProperty(SecurityServiceProtocol.VerificationProperty, SecurityServiceProtocol.VerificationSuccess);
                _logger.Info("TOTP verification for {0} successful", totpObject.Id);
            }
            else
            {
                Global.Throttle.Fail(totpObject.Id.ToString(), SecurityThrottleType.Totp);
                _logger.Info("TOTP verification for {0} failed", totpObject.Id);
                reply.AddProperty(SecurityServiceProtocol.VerificationProperty, SecurityServiceProtocol.VerificationFailure);
            }

            reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultSuccess);
        }

        private void VerifyPassword(JObject request, JObject reply)
        {
            var password = request.ValueString(SecurityServiceProtocol.PasswordProperty);
            var encryptedPassworData = request.ValueBytes(SecurityServiceProtocol.PasswordDataProperty);
            var encryptedPassword = new Encrypted<PasswordObject>(encryptedPassworData);
            var passwordObject = encryptedPassword.Decrypt(_secretKey);
            Global.Throttle.Check(passwordObject.Id.ToString(), SecurityThrottleType.Password);

            if (passwordObject.Verify(password))
            {
                reply.AddProperty(SecurityServiceProtocol.VerificationProperty, SecurityServiceProtocol.VerificationSuccess);
                _logger.Info("Password verification for {0} successful", passwordObject.Id);
            }
            else
            {
                Global.Throttle.Fail(passwordObject.Id.ToString(), SecurityThrottleType.Password);
                _logger.Info("Password verification for {0} failed", passwordObject.Id);
                reply.AddProperty(SecurityServiceProtocol.VerificationProperty, SecurityServiceProtocol.VerificationFailure);
            }

            reply.AddProperty(SecurityServiceProtocol.ResultProperty, SecurityServiceProtocol.ResultSuccess);
        }
    }
}

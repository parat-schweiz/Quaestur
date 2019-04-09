using System;
namespace SecurityServiceClient
{
    public static class SecurityServiceProtocol
    {
        public const string CommandProperty = "command";
        public const string CommandSecurePassword = "securepassword";
        public const string CommandVerifyPassword = "verifypassword";
        public const string CommandSecureTotp = "securetotp";
        public const string CommandVerifyTotp = "verifytotp";
        public const string CommandSecureGpgPassphrase = "securegpgpassphrase";
        public const string CommandExecuteGpg = "executegpg";
        public const string PasswordProperty = "password";
        public const string PassphraseProperty = "passphrase";
        public const string CodeProperty = "code";
        public const string SecretProperty = "secret";
        public const string TotpDataProperty = "totpdata";
        public const string PasswordDataProperty = "passworddata";
        public const string PassphraseDataProperty = "passphrasedata";
        public const string InputDataProperty = "inputdata";
        public const string OutputDataProperty = "outputdata";
        public const string ArgumentsProperty = "arguments";
        public const string ErrorDataProperty = "errordata";
        public const string ExitCodeProperty = "exitcode";
        public const string VerificationProperty = "verification";
        public const string VerificationSuccess = "success";
        public const string VerificationFailure = "failure";
        public const string ResultProperty = "result";
        public const string ResultSuccess = "success";
        public const string ResultErrorUnknownCommand = "errorunknowncommand";
        public const string ResultErrorMalFormedRequestData = "errormalformedrequestdata";
    }
}

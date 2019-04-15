using System;
using System.Linq;

namespace Quaestur
{
    public static class Model
    {
        public static int CurrentVersion = 12;

        public static void Install(IDatabase database)
        {
            CreateAllTables(database);
            Migrate(database);
            CheckPaymentParameters(database);
        }

        private static void CreateAllTables(IDatabase database)
        {
            database.CreateTable<Meta>();
            database.CreateTable<Country>();
            database.CreateTable<State>();
            database.CreateTable<Person>();
            database.CreateTable<PostalAddress>();
            database.CreateTable<PublicKey>();
            database.CreateTable<ServiceAddress>();
            database.CreateTable<Organization>();
            database.CreateTable<Group>();
            database.CreateTable<Role>();
            database.CreateTable<Permission>();
            database.CreateTable<RoleAssignment>();
            database.CreateTable<MembershipType>();
            database.CreateTable<PaymentParameter>();
            database.CreateTable<Membership>();
            database.CreateTable<Tag>();
            database.CreateTable<TagAssignment>();
            database.CreateTable<MailingElement>();
            database.CreateTable<Mailing>();
            database.CreateTable<Sending>();
            database.CreateTable<Document>();
            database.CreateTable<Bill>();
            database.CreateTable<BillSendingTemplate>();
            database.CreateTable<Export>();
            database.CreateTable<JournalEntry>();
            database.CreateTable<Phrase>();
            database.CreateTable<PhraseTranslation>();
            database.CreateTable<SystemWideSettings>();
            database.CreateTable<Oauth2Client>();
            database.CreateTable<Oauth2Session>();
            database.CreateTable<Oauth2Authorization>();
            database.CreateTable<SearchSettings>();
            database.CreateTable<SendingTemplate>();
            database.CreateTable<SendingTemplateLanguage>();
            database.CreateTable<BallotTemplate>();
            database.CreateTable<Ballot>();
            database.CreateTable<BallotPaper>();
            database.CreateTable<LoginLink>();
        }

        private static void Migrate(IDatabase database)
        {
            var meta = database.Query<Meta>().SingleOrDefault();

            if (meta == null)
            {
                meta = new Meta(Guid.NewGuid());
            }

            while (meta.Version.Value < CurrentVersion)
            {
                meta.Version.Value++;
                Migrate(database, meta.Version.Value);
            }

            database.Save(meta);
        }

        private static void Migrate(IDatabase database, int version)
        {
            switch (version)
            {
                case 2:
                    database.AddColumn<BillSendingTemplate>(bst => bst.SendingMode);
                    break;
                case 3:
                    database.AddColumn<Person>(p => p.Deleted);
                    break;
                case 4:
                    database.AddColumn<Person>(p => p.TwoFactorSecret);
                    break;
                case 5:
                    database.AddColumn<Membership>(m => m.HasVotingRight);
                    break;
                case 6:
                    database.AddColumn<Oauth2Client>(c => c.RequireTwoFactor);
                    break;
                case 7:
                    database.AddColumn<Oauth2Client>(c => c.Access);
                    break;
                case 8:
                    database.AddColumn<Person>(p => p.PasswordType);
                    break;
                case 9:
                    UpdatePasswordTypes(database);
                    break;
                case 10:
                    SecureTotpSecrets(database);
                    break;
                case 11:
                    database.ModifyColumnType<Group>(g => g.GpgKeyPassphrase);
                    break;
                case 12:
                    EncryptGpgPassphrases(database);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void EncryptGpgPassphrases(IDatabase database)
        {
            foreach (var group in database.Query<Group>())
            {
                var passphraseData = Global.Security.SecureGpgPassphrase(group.GpgKeyPassphrase.Value);
                group.GpgKeyPassphrase.Value = Convert.ToBase64String(passphraseData);
                database.Save(group);
            }
        }

        private static void SecureTotpSecrets(IDatabase database)
        {
            foreach (var person in database.Query<Person>())
            {
                if (person.TwoFactorSecret.Value != null)
                {
                    var totpData = Global.Security.SecureTotp(person.TwoFactorSecret.Value);
                    person.TwoFactorSecret.Value = totpData;
                    database.Save(person);
                }
            }
        }

        private static void UpdatePasswordTypes(IDatabase database)
        { 
            foreach (var person in database.Query<Person>())
            {
                if (person.PasswordType.Value == PasswordType.None &&
                    person.PasswordHash.Value != null)
                {
                    person.PasswordType.Value = PasswordType.Local;
                    database.Save(person);
                }
            }
        }

        private static void CheckPaymentParameters(IDatabase database)
        {
            foreach (var membershipType in database.Query<MembershipType>())
            {
                var model = membershipType.CreatePaymentModel();

                if (model != null)
                {
                    foreach (var parameterType in model.ParameterTypes)
                    {
                        if (!membershipType.PaymentParameters
                            .Any(p => p.Key.Value == parameterType.Key))
                        {
                            var newParameter = new PaymentParameter(Guid.NewGuid());
                            newParameter.Key.Value = parameterType.Key;
                            newParameter.Value.Value = parameterType.DefaultValue;
                            newParameter.Type.Value = membershipType;
                            database.Save(newParameter);
                        }
                    }

                    foreach (var parameter in membershipType.PaymentParameters)
                    {
                        if (!model.ParameterTypes.Any(
                            pt => pt.Key == parameter.Key))
                        {
                            parameter.Delete(database); 
                        } 
                    }
                }
            }
        }
    }
}

using System;
using System.Linq;

namespace Quaestur
{
    public static class Model
    {
        public static int CurrentVersion = 7;

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
                default:
                    throw new NotSupportedException();
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

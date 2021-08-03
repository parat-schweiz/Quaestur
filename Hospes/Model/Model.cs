using System;
using System.Linq;
using SiteLibrary;

namespace Hospes
{
    public static class Model
    {
        public static int CurrentVersion = 1;

        public static void Install(IDatabase database)
        {
            CreateAllTables(database);
            Migrate(database);
        }

        private static void CreateAllTables(IDatabase database)
        {
            Global.Log.Notice("Checking tables...");

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
            database.CreateTable<Membership>();
            database.CreateTable<Tag>();
            database.CreateTable<TagAssignment>();
            database.CreateTable<MailingElement>();
            database.CreateTable<Mailing>();
            database.CreateTable<Sending>();
            database.CreateTable<Export>();
            database.CreateTable<JournalEntry>();
            database.CreateTable<Phrase>();
            database.CreateTable<PhraseTranslation>();
            database.CreateTable<SystemWideSettings>();
            database.CreateTable<Oauth2Client>();
            database.CreateTable<Oauth2Session>();
            database.CreateTable<Oauth2Authorization>();
            database.CreateTable<SearchSettings>();
            database.CreateTable<LoginLink>();
            database.CreateTable<MailTemplate>();
            database.CreateTable<MailTemplateAssignment>();
            database.CreateTable<LatexTemplate>();
            database.CreateTable<LatexTemplateAssignment>();
            database.CreateTable<ApiClient>();
            database.CreateTable<ApiPermission>();
            database.CreateTable<Sequence>();
            database.CreateTable<ReservedUserName>();
            database.CreateTable<SystemWideFile>();

            Global.Log.Notice("Tables ok.");
        }

        private static void Migrate(IDatabase database)
        {
            Global.Log.Notice("Checking migrations...");

            var meta = database.Query<Meta>().SingleOrDefault();

            if (meta == null)
            {
                meta = new Meta(Guid.NewGuid());
                database.Save(meta);
            }

            while (meta.Version.Value < CurrentVersion)
            {
                Global.Log.Notice("Migrating to version {0}.", (meta.Version.Value + 1));

                using (var transaction = database.BeginTransaction())
                {
                    meta.Version.Value++;
                    Migrate(database, meta.Version.Value);
                    database.Save(meta);
                    transaction.Commit();
                }

                Global.Log.Notice("Migration applied.");
            }

            Global.Log.Notice("Migrations done.");
        }

        private static void Migrate(IDatabase database, int version)
        {
            switch (version)
            {
                case 0:
                    database.AddColumn<Organization>(o => o.BillName);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void SetNextPersonNumber(IDatabase database)
        {
            var sequence = database.Query<Sequence>().SingleOrDefault();

            if (sequence == null)
            {
                sequence = new Sequence(Guid.NewGuid());
                sequence.NextPersonNumber.Value =
                    database.Query<Person>().Max(p => p.Number.Value) + 1;
                database.Save(sequence);
            }
        }
    }
}

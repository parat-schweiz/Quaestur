using System;
using System.Linq;

namespace Publicus
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
            database.CreateTable<Meta>();
            database.CreateTable<Country>();
            database.CreateTable<State>();
            database.CreateTable<Contact>();
            database.CreateTable<PostalAddress>();
            database.CreateTable<PublicKey>();
            database.CreateTable<ServiceAddress>();
            database.CreateTable<Feed>();
            database.CreateTable<Group>();
            database.CreateTable<Role>();
            database.CreateTable<User>();
            database.CreateTable<Permission>();
            database.CreateTable<MasterRole>();
            database.CreateTable<RoleAssignment>();
            database.CreateTable<Subscription>();
            database.CreateTable<Tag>();
            database.CreateTable<TagAssignment>();
            database.CreateTable<MailingElement>();
            database.CreateTable<Mailing>();
            database.CreateTable<Sending>();
            database.CreateTable<Document>();
            database.CreateTable<Export>();
            database.CreateTable<JournalEntry>();
            database.CreateTable<Phrase>();
            database.CreateTable<PhraseTranslation>();
            database.CreateTable<SystemWideSettings>();
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
                case 1:
                    //database.AddColumn<Template>(bst => bst.SendingMode);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

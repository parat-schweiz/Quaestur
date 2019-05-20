using System;
using System.Linq;
using SiteLibrary;

namespace Scriba
{
    public static class Model
    {
        public static int CurrentVersion = 2;

        public static void Install(IDatabase database)
        {
            CreateAllTables(database);
            Migrate(database);
        }

        private static void CreateAllTables(IDatabase database)
        {
            database.CreateTable<Meta>();
            database.CreateTable<Feed>();
            database.CreateTable<Group>();
            database.CreateTable<Role>();
            database.CreateTable<User>();
            database.CreateTable<Permission>();
            database.CreateTable<MasterRole>();
            database.CreateTable<RoleAssignment>();
            database.CreateTable<Tag>();
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
                    break;
                case 2:
                    database.ModifyColumnType<Group>(g => g.GpgKeyPassphrase);
                    break;
                case 3:
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
    }
}

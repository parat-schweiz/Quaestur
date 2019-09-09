using System;
using System.Linq;
using SiteLibrary;

namespace Census
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
            database.CreateTable<User>();
            database.CreateTable<Organization>();
            database.CreateTable<Group>();
            database.CreateTable<Role>();
            database.CreateTable<MasterRole>();
            database.CreateTable<RoleAssignment>();
            database.CreateTable<Permission>();
            database.CreateTable<Questionaire>();
            database.CreateTable<Variable>();
            database.CreateTable<Section>();
            database.CreateTable<Question>();
            database.CreateTable<Option>();
            database.CreateTable<Phrase>();
            database.CreateTable<PhraseTranslation>();
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
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

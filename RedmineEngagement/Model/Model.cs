using System;
using System.Linq;
using SiteLibrary;
using BaseLibrary;

namespace RedmineEngagement
{
    public static class Model
    {
        public static int CurrentVersion = 1;

        public static void Install(IDatabase database, Logger logger)
        {
            CreateAllTables(database, logger);
            Migrate(database, logger);
        }

        private static void CreateAllTables(IDatabase database, Logger logger)
        {
            logger.Info("Checking tables...");

            database.CreateTable<Meta>();
            database.CreateTable<Phrase>();
            database.CreateTable<PhraseTranslation>();
            database.CreateTable<Person>();
            database.CreateTable<Issue>();
            database.CreateTable<Assignment>();

            logger.Info("Tables ok.");
        }

        private static void Migrate(IDatabase database, Logger logger)
        {
            logger.Info("Checking migrations...");

            var meta = database.Query<Meta>().SingleOrDefault();

            if (meta == null)
            {
                meta = new Meta(Guid.NewGuid());
            }

            while (meta.Version.Value < CurrentVersion)
            {
                logger.Info("Migrating to version {0}.", (meta.Version.Value + 1));

                using (var transaction = database.BeginTransaction())
                {
                    meta.Version.Value++;
                    Migrate(database, meta.Version.Value);
                    database.Save(meta);
                    transaction.Commit();
                }

                logger.Info("Migration applied.");
            }

            logger.Info("Migrations done.");
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

using System;
using System.Linq;
using SiteLibrary;
using BaseLibrary;

namespace DiscourseEngagement
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
            logger.Notice("Checking tables...");

            database.CreateTable<Meta>();
            database.CreateTable<Phrase>();
            database.CreateTable<PhraseTranslation>();
            database.CreateTable<Person>();
            database.CreateTable<Topic>();
            database.CreateTable<Post>();
            database.CreateTable<Like>();

            logger.Notice("Tables ok.");
        }

        private static void Migrate(IDatabase database, Logger logger)
        {
            logger.Notice("Checking migrations...");

            var meta = database.Query<Meta>().SingleOrDefault();

            if (meta == null)
            {
                meta = new Meta(Guid.NewGuid());
            }

            while (meta.Version.Value < CurrentVersion)
            {
                logger.Notice("Migrating to version {0}.", (meta.Version.Value + 1));

                using (var transaction = database.BeginTransaction())
                {
                    meta.Version.Value++;
                    Migrate(database, meta.Version.Value);
                    database.Save(meta);
                    transaction.Commit();
                }

                logger.Notice("Migration applied.");
            }

            logger.Notice("Migrations done.");
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

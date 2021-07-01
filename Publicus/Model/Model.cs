using System;
using System.Linq;
using SiteLibrary;

namespace Publicus
{
    public static class Model
    {
        public static int CurrentVersion = 5;

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
            database.CreateTable<SystemWideFile>();
            database.CreateTable<Petition>();
            database.CreateTable<MailTemplate>();
            database.CreateTable<MailTemplateAssignment>();
            database.CreateTable<LatexTemplate>();
            database.CreateTable<LatexTemplateAssignment>();
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
                case 4:
                    database.AddColumn<Contact>(c => c.ExpiryDate);
                    break;
                case 5:
                    database.AddColumn<Contact>(c => c.Position);
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

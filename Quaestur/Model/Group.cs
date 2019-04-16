using System;
using System.Collections.Generic;

namespace Quaestur
{
    public class Group : DatabaseObject
    {
        public ForeignKeyField<Organization, Group> Organization { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public MultiLanguageStringField MailName { get; private set; }
        public MultiLanguageStringField MailAddress { get; private set; }
        public StringField GpgKeyId { get; private set; }
        public StringField GpgKeyPassphrase { get; private set; }
        public List<Role> Roles { get; private set; }

        public Group() : this(Guid.Empty)
        {
        }

		public Group(Guid id) : base(id)
        {
            Organization = new ForeignKeyField<Organization, Group>(this, "organizationid", false, o => o.Groups);
            Name = new MultiLanguageStringField(this, "name");
            MailName = new MultiLanguageStringField(this, "mailname");
            MailAddress = new MultiLanguageStringField(this, "mailaddress");
            GpgKeyId = new StringField(this, "gpgkeyid", 256);
            GpgKeyPassphrase = new StringField(this, "gpgkeypassphrase", 4096);
            Roles = new List<Role>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Role>("groupid", Id.Value, () => Roles);
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var ballotTemplate in database.Query<BallotTemplate>(DC.Equal("organizerid", Id.Value)))
            {
                ballotTemplate.Delete(database);
            }

            foreach (var role in Roles)
            {
                role.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Organization.ToString() + " / " + Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Organization.GetText(translator) + " / " + Name.Value[translator.Language];
        }
    }
}

using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public class Group : DatabaseObject
    {
        public ForeignKeyField<Queue, Group> Queue { get; private set; }
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
            Queue = new ForeignKeyField<Queue, Group>(this, "queueid", false, o => o.Groups);
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

        public override void Delete(IDatabase db)
        {
            foreach (var role in Roles)
            {
                role.Delete(db);
            }

            db.Delete(this);
        }

        public override string ToString()
        {
            return Queue.ToString() + " / " + Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Queue.GetText(translator) + " / " + Name.Value[translator.Language];
        }
    }
}

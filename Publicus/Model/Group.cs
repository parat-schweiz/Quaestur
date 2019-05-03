using System;
using System.Collections.Generic;

namespace Publicus
{
    public class Group : DatabaseObject
    {
        public ForeignKeyField<Feed, Group> Feed { get; private set; }
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
            Feed = new ForeignKeyField<Feed, Group>(this, "feedid", false, o => o.Groups);
            Name = new MultiLanguageStringField(this, "name");
            MailName = new MultiLanguageStringField(this, "mailname");
            MailAddress = new MultiLanguageStringField(this, "mailaddress");
            GpgKeyId = new StringField(this, "gpgkeyid", 256);
            GpgKeyPassphrase = new StringField(this, "gpgkeypassphrase", 256);
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
            return Feed.ToString() + " / " + Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Feed.GetText(translator) + " / " + Name.Value[translator.Language];
        }
    }
}

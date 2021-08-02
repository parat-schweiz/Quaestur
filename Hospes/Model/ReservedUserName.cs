using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class ReservedUserName : DatabaseObject
    {
		public StringField UserName { get; set; }
        public Field<Guid> UserId { get; set; }

        public ReservedUserName() : this(Guid.Empty)
        {
        }

        public ReservedUserName(Guid id) : base(id)
        {
            UserName = new StringField(this, "username", 256);
            UserId = new Field<Guid>(this, "userid", Guid.Empty);
        }

        public override string ToString()
        {
            return UserName.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return UserName.Value;
        }
    }
}

using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class RoleAssignment : DatabaseObject
    {
        public ForeignKeyField<Person, RoleAssignment> Person { get; set; }
        public ForeignKeyField<Role, RoleAssignment> Role { get; set; }

        public RoleAssignment() : this(Guid.Empty)
        {
        }

		public RoleAssignment(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, RoleAssignment>(this, "personid", false, p => p.RoleAssignments);
            Role = new ForeignKeyField<Role, RoleAssignment>(this, "roleid", false, null);
        }

        public override string GetText(Translator translator)
        {
            throw new NotSupportedException();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Publicus
{
    public class RoleAssignment : DatabaseObject
    {
        public ForeignKeyField<MasterRole, RoleAssignment> MasterRole { get; set; }
        public ForeignKeyField<Role, RoleAssignment> Role { get; set; }

        public RoleAssignment() : this(Guid.Empty)
        {
        }

		public RoleAssignment(Guid id) : base(id)
        {
            MasterRole = new ForeignKeyField<MasterRole, RoleAssignment>(this, "masterroleid", false, mr => mr.RoleAssignments);
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

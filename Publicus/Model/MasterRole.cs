using System;
using System.Collections.Generic;

namespace Publicus
{
    public class MasterRole : DatabaseObject
    {
        public MultiLanguageStringField Name { get; private set; }
        public List<RoleAssignment> RoleAssignments { get; private set; }

        public MasterRole() : this(Guid.Empty)
        {
        }

		public MasterRole(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            RoleAssignments = new List<RoleAssignment>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get 
            {
                yield return new MultiCascade<RoleAssignment>("masterroleid", Id.Value, () => RoleAssignments);
            } 
        }

        public override void Delete(IDatabase database)
        {
            foreach (var roleAssignment in database.Query<RoleAssignment>(DC.Equal("masterroleid", Id.Value)))
            {
                database.Delete(roleAssignment);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

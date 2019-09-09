using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public class Role : DatabaseObject
    {
        public ForeignKeyField<Group, Role> Group { get; set; }
        public MultiLanguageStringField Name { get; set; }
        public List<Permission> Permissions { get; set; }

        public Role() : this(Guid.Empty)
        {
        }

		public Role(Guid id) : base(id)
        {
            Group = new ForeignKeyField<Group, Role>(this, "groupid", false, g => g.Roles);
            Name = new MultiLanguageStringField(this, "name");
            Permissions = new List<Permission>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Permission>("roleid", Id.Value, () => Permissions); 
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var roleAssignment in database.Query<RoleAssignment>(DC.Equal("roleid", Id.Value)))
            {
                roleAssignment.Delete(database);
            }

            foreach (var permission in database.Query<Permission>(DC.Equal("roleid", Id.Value)))
            {
                permission.Delete(database); 
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Group.ToString() + " / " + Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Group.GetText(translator) + " / " + Name.Value[translator.Language];
        }
    }
}

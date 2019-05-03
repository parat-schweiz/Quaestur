using System;
using System.Collections.Generic;

namespace Publicus
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

        public override void Delete(IDatabase db)
        {
            foreach (var roleAssignment in db.Query<RoleAssignment>(DC.Equal("roleid", Id.Value)))
            {
                db.Delete(roleAssignment);
            }

            foreach (var permission in db.Query<Permission>(DC.Equal("roleid", Id.Value)))
            {
                db.Delete(permission); 
            }

            db.Delete(this);
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

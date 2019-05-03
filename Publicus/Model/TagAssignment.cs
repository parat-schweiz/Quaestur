using System;
using System.Collections.Generic;

namespace Publicus
{
    public class TagAssignment : DatabaseObject
    {
        public ForeignKeyField<Contact, TagAssignment> Contact { get; set; }
        public ForeignKeyField<Tag, TagAssignment> Tag { get; set; }

        public TagAssignment() : this(Guid.Empty)
        {
        }

		public TagAssignment(Guid id) : base(id)
        {
            Contact = new ForeignKeyField<Contact, TagAssignment>(this, "contactid", false, p => p.TagAssignments);
            Tag = new ForeignKeyField<Tag, TagAssignment>(this, "tagid", false, null);
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

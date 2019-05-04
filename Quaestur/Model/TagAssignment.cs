using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class TagAssignment : DatabaseObject
    {
        public ForeignKeyField<Person, TagAssignment> Person { get; set; }
        public ForeignKeyField<Tag, TagAssignment> Tag { get; set; }

        public TagAssignment() : this(Guid.Empty)
        {
        }

		public TagAssignment(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, TagAssignment>(this, "personid", false, p => p.TagAssignments);
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

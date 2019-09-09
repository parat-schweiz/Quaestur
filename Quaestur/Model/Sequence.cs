using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class Sequence : DatabaseObject
    {
		public Field<int> NextPersonNumber { get; set; }

        public Sequence() : this(Guid.Empty)
        {
        }

        public Sequence(Guid id) : base(id)
        {
            NextPersonNumber = new Field<int>(this, "nextpersonnumber", 1);
        }

        public override string ToString()
        {
            return typeof(Sequence).Name;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return typeof(Sequence).Name;
        }
    }
}

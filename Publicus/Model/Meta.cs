using System;
using System.Collections.Generic;

namespace Publicus
{
    public class Meta : DatabaseObject
    {
        public Field<int> Version { get; private set; }

        public Meta() : this(Guid.Empty)
        {
        }

        public Meta(Guid id) : base(id)
        {
            Version = new Field<int>(this, "version", Model.CurrentVersion);
        }

        public override string GetText(Translator translator)
        {
            throw new NotImplementedException();
        }

        public override void Delete(IDatabase database)
        {
            throw new NotImplementedException();
        }
    }
}

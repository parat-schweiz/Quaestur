using System;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public class SystemWideSettings : DatabaseObject
    {
		public StringField Currency { get; private set; }

        public SystemWideSettings() : this(Guid.Empty)
        {
        }

		public SystemWideSettings(Guid id) : base(id)
        {
            Currency = new StringField(this, "currency", 256);
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

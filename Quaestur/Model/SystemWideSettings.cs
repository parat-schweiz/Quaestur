using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class SystemWideSettings : DatabaseObject
    {
		public StringField Currency { get; private set; }
        public Field<int> CreditsPerCurrency { get; private set; }
        public Field<int> CreditsDecayAgeDays { get; private set; }

        public SystemWideSettings() : this(Guid.Empty)
        {
        }

		public SystemWideSettings(Guid id) : base(id)
        {
            Currency = new StringField(this, "currency", 256);
            CreditsPerCurrency = new Field<int>(this, "creditspercurrency", 100);
            CreditsDecayAgeDays = new Field<int>(this, "creditsdecayage", 730);
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

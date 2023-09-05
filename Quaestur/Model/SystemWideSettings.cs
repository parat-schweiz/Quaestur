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
        public Field<int> PointsDataPreservationYears { get; private set; }
        public Field<int> PointsTallyDataPreservationYears { get; private set; }
        public Field<int> CreditsDataPreservationYears { get; private set; }
        public Field<int> JournalPreservationDays { get; private set; }
        public Field<int> MailingPreservationDays { get; private set; }
        public Field<int> BallotPreservationDays { get; private set; }

        public SystemWideSettings() : this(Guid.Empty)
        {
        }

		public SystemWideSettings(Guid id) : base(id)
        {
            Currency = new StringField(this, "currency", 256);
            CreditsPerCurrency = new Field<int>(this, "creditspercurrency", 100);
            CreditsDecayAgeDays = new Field<int>(this, "creditsdecayage", 730);
            PointsDataPreservationYears = new Field<int>(this, "pointsdataperservationyears", 3);
            PointsTallyDataPreservationYears = new Field<int>(this, "pointstallydataperservationyears", 10);
            CreditsDataPreservationYears = new Field<int>(this, "creditsdatapreservationyears", 3);
            JournalPreservationDays = new Field<int>(this, "journalpreservationdays", 730);
            MailingPreservationDays = new Field<int>(this, "mailingpreservationdays", 730);
            BallotPreservationDays = new Field<int>(this, "ballotpreservationdays", 730);
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

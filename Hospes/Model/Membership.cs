using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class Membership : DatabaseObject
    {
        public ForeignKeyField<Person, Membership> Person { get; private set; }
        public ForeignKeyField<Organization, Membership> Organization { get; private set; }
        public ForeignKeyField<MembershipType, Membership> Type { get; private set; }
        public Field<DateTime> StartDate { get; private set; }
        public FieldNull<DateTime> EndDate { get; private set; }

        public Membership() : this(Guid.Empty)
        {
        }

		public Membership(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, Membership>(this, "personid", false, p => p.Memberships);
            Organization = new ForeignKeyField<Organization, Membership>(this, "organizationid", false, null);
            Type = new ForeignKeyField<MembershipType, Membership>(this, "membershiptypeid", false, null);
            StartDate = new Field<DateTime>(this, "startdate", DateTime.UtcNow);
            EndDate = new FieldNull<DateTime>(this, "enddate");
        }

        public override string ToString()
        {
            return "Membership in " + Organization.Value.Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return translator.Get(
                "Membership.Text",
                "Textual representation of of membership (respective to person)",
                "in {0}",
                Organization.Value.GetText(translator));
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public bool IsActive
        {
            get 
            {
                return DateTime.UtcNow.Date >= StartDate.Value.Date &&
                       (!EndDate.Value.HasValue || DateTime.UtcNow.Date <= EndDate.Value.Value.Date);
            }
        }
    }
}

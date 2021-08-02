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
        public FieldNull<bool> HasVotingRight { get; private set; }

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
            HasVotingRight = new FieldNull<bool>(this, "hasvotingright");
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
            foreach (var bill in database.Query<Bill>(DC.Equal("membershipid", Id.Value)))
            {
                bill.Delete(database);
            }

            foreach (var ballotpaper in database.Query<BallotPaper>(DC.Equal("memberid", Id.Value)))
            {
                ballotpaper.Delete(database);
            }

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

        public void UpdateVotingRight(IDatabase database)
        {
            if (Type.Value.Rights.Value.HasFlag(MembershipRight.Voting))
            {
                HasVotingRight.Value = HasVotingRightInternal(database, CollectionModel.Direct);
            }
            else
            {
                HasVotingRight.Value = false;
            }
        }

        private bool HasVotingRightInternal(IDatabase database, CollectionModel last)
        {
            switch (Type.Value.Collection.Value)
            {
                case CollectionModel.None:
                    return true;
                case CollectionModel.Direct:
                    var model = Type.Value.CreatePaymentModel(database);
                    return model == null || model.HasVotingRight(this);
                case CollectionModel.ByParent:
                    if (last == CollectionModel.BySub) return false;
                    return Person.Value.Memberships
                        .Any(m => m.Organization.Value.Children.Contains(Organization.Value) &&
                            m.HasVotingRightInternal(database, CollectionModel.ByParent));
                case CollectionModel.BySub:
                    if (last == CollectionModel.ByParent) return false;
                    return Person.Value.Memberships
                        .Any(m => Organization.Value.Children.Contains(m.Organization.Value) &&
                            m.HasVotingRightInternal(database, CollectionModel.BySub));
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

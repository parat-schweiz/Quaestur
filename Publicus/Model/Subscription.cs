using System;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public class Subscription : DatabaseObject
    {
        public ForeignKeyField<Contact, Subscription> Contact { get; set; }
        public ForeignKeyField<Feed, Subscription> Feed { get; set; }
        public Field<DateTime> StartDate { get; set; }
        public FieldNull<DateTime> EndDate { get; set; }

        public Subscription() : this(Guid.Empty)
        {
        }

		public Subscription(Guid id) : base(id)
        {
            Contact = new ForeignKeyField<Contact, Subscription>(this, "contactid", false, p => p.Subscriptions);
            Feed = new ForeignKeyField<Feed, Subscription>(this, "feedid", false, null);
            StartDate = new Field<DateTime>(this, "startdate", DateTime.UtcNow);
            EndDate = new FieldNull<DateTime>(this, "enddate");
        }

        public override string GetText(Translator translator)
        {
            return translator.Get(
                "Subscription.Text",
                "Textual representation of of subscription (respective to contact)",
                "in {0}",
                Feed.GetText(translator));
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

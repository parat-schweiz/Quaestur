using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public class Ticket : DatabaseObject
    {
        public ForeignKeyField<Queue, Ticket> Queue { get; private set; }
        public StringField Number { get; private set; }
        public StringField Subject { get; private set; }
        public List<Article> Articles { get; private set; }

        public Ticket() : this(Guid.Empty)
        {
        }

        public Ticket(Guid id) : base(id)
        {
            Queue = new ForeignKeyField<Queue, Ticket>(this, "queueid", false, null);
            Number = new StringField(this, "number", 64);
            Subject = new StringField(this, "subject", 1024);
            Articles = new List<Article>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get 
            {
                yield return new MultiCascade<Article>("ticketid", Id.Value, () => Articles);
            }
        }

        public override string ToString()
        {
            return Number.Value;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var address in database.Query<Article>(DC.Equal("ticketid", Id.Value)))
            {
                address.Delete(database);
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Number.Value;
        }
    }
}

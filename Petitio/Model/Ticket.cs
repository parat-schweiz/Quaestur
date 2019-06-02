using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public enum TicketStatus
    {
        New = 0,
        Pending = 1,
        Closed = 2,
    }

    public static class TicketStatusExtensions
    {
        public static string Translate(this TicketStatus status, Translator translator)
        {
            switch (status)
            {
                case TicketStatus.New:
                    return translator.Get("Enum.TicketStatus.New", "New value in the ticket status enum", "New");
                case TicketStatus.Pending:
                    return translator.Get("Enum.TicketStatus.Pending", "Pending value in the ticket status enum", "Pending");
                case TicketStatus.Closed:
                    return translator.Get("Enum.TicketStatus.Closed", "Closed value in the ticket status enum", "Closed");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Ticket : DatabaseObject
    {
        public ForeignKeyField<Queue, Ticket> Queue { get; private set; }
        public StringField Number { get; private set; }
        public StringField Subject { get; private set; }
        public EnumField<TicketStatus> Status { get; private set; }
        public List<Article> Articles { get; private set; }
        public StringField Error { get; private set; }

        public DateTime Created
        {
            get
            {
                if (Articles.Count > 0)
                {
                    return Articles.Min(a => a.CreatedDate);
                }
                else
                {
                    return new DateTime(1970, 1, 1); 
                }
            }
        }

        public DateTime LastUpdate
        {
            get
            {
                if (Articles.Count > 0)
                {
                    return Articles.Max(a => a.CreatedDate);
                }
                else
                {
                    return new DateTime(1970, 1, 1);
                }
            }
        }

        public Ticket() : this(Guid.Empty)
        {
        }

        public Ticket(Guid id) : base(id)
        {
            Queue = new ForeignKeyField<Queue, Ticket>(this, "queueid", false, null);
            Number = new StringField(this, "number", 64);
            Subject = new StringField(this, "subject", 1024);
            Status = new EnumField<TicketStatus>(this, "status", TicketStatus.New, TicketStatusExtensions.Translate);
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

using System;
using System.Collections.Generic;

namespace Publicus
{
    public enum SendingStatus
    {
        Created,
        Sent,
        Failed 
    }

    public static class SendingStatusExtensions
    {
        public static string Translate(this SendingStatus status, Translator translator)
        { 
            switch (status)
            {
                case SendingStatus.Created:
                    return translator.Get("Enum.SendingStatus.Created", "Created value of the sending status enum", "Created");
                case SendingStatus.Sent:
                    return translator.Get("Enum.SendingStatus.Sent", "Sent value of the sending status enum", "Sent");
                case SendingStatus.Failed:
                    return translator.Get("Enum.SendingStatus.Failed", "Failed value of the sending status enum", "Failed");
                default:
                    throw new NotSupportedException(); 
            }
        }
    }

    public class Sending : DatabaseObject
    {
		public ForeignKeyField<Mailing, Sending> Mailing { get; set; }
        public ForeignKeyField<ServiceAddress, Sending> Address { get; set; }
        public EnumField<SendingStatus> Status { get; set; }
        public FieldNull<DateTime> SentDate { get; set; }
        public StringNullField FailureMessage { get; set; }

        public Sending() : this(Guid.Empty)
        {
        }

        public Sending(Guid id) : base(id)
        {
            Mailing = new ForeignKeyField<Mailing, Sending>(this, "mailingid", false, null);
            Address = new ForeignKeyField<ServiceAddress, Sending>(this, "addressid", false, null);
            Status = new EnumField<SendingStatus>(this, "status", SendingStatus.Created, SendingStatusExtensions.Translate);
            SentDate = new FieldNull<DateTime>(this, "sentdate");
            FailureMessage = new StringNullField(this, "failuremessage", 256);
        }

        public override string GetText(Translator translator)
        {
            throw new NotSupportedException();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

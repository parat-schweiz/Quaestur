using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public enum MembershipTaskType
    {
        Billing = 0,
        PaymentParameterUpdate = 1,
    }

    public static class MembershipTaskTypeExtensions
    {
        public static string Translate(this MembershipTaskType type, Translator translator)
        {
            switch (type)
            {
                case MembershipTaskType.Billing:
                    return translator.Get("Enum.MembershipTaskType.Billing", "Billing value in the membership task type enum", "Billing");
                case MembershipTaskType.PaymentParameterUpdate:
                    return translator.Get("Enum.MembershipTaskType.PaymentParameterUpdate", "Payment parameter update value in the membership task type enum", "Payment parameter update");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum MembershipTaskStatus
    {
        New = 0,
        Hold = 1,
        Ready = 2,
        Successful = 3,
        Invalidated = 4,
        Failed = 5,
    }

    public static class MembershipTaskStatusExtensions
    {
        public static string Translate(this MembershipTaskStatus status, Translator translator)
        {
            switch (status)
            {
                case MembershipTaskStatus.New:
                    return translator.Get("Enum.MembershipTaskStatus.New", "New value in the membership task status enum", "New");
                case MembershipTaskStatus.Hold:
                    return translator.Get("Enum.MembershipTaskStatus.Hold", "Hold value in the membership task status enum", "Hold");
                case MembershipTaskStatus.Ready:
                    return translator.Get("Enum.MembershipTaskStatus.Ready", "Ready value in the membership task status enum", "Ready");
                case MembershipTaskStatus.Successful:
                    return translator.Get("Enum.MembershipTaskStatus.Successful", "Successful value in the membership task status enum", "Successful");
                case MembershipTaskStatus.Invalidated:
                    return translator.Get("Enum.MembershipTaskStatus.Invalidated", "Invalidated value in the membership task status enum", "Invalidated");
                case MembershipTaskStatus.Failed:
                    return translator.Get("Enum.MembershipTaskStatus.Failed", "Failed value in the membership task status enum", "Failed");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class MembershipTask : DatabaseObject
    {
		public ForeignKeyField<Membership, MembershipTask> Membership { get; private set; }
        public Field<MembershipTaskType> Type { get; private set; }
        public Field<MembershipTaskStatus> Status { get; private set; }
        public DateTimeField Created { get; private set; }
        public DateTimeField Modifed { get; private set; }
        public StringField Message { get; private set; }
        public StringField Error { get; private set; }

        public MembershipTask() : this(Guid.Empty)
        {
        }

        public MembershipTask(Guid id) : base(id)
        {
            Membership = new ForeignKeyField<Membership, MembershipTask>(this, "membershipid", false, null);
            Type = new Field<MembershipTaskType>(this, "type", MembershipTaskType.Billing);
            Status = new Field<MembershipTaskStatus>(this, "status", MembershipTaskStatus.New);
            Created = new DateTimeField(this, "created", DateTime.UtcNow);
            Modifed = new DateTimeField(this, "modifed", DateTime.UtcNow);
            Message = new StringField(this, "message", 4096, AllowStringType.SimpleText);
            Error = new StringField(this, "error", 32768, AllowStringType.SimpleText);
        }

        public override string ToString()
        {
            return Membership.Value.ToString() + " / " + Type.Value.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Membership.GetText(translator) + " / " + Type.Value.Translate(translator);
        }

        public IMembershipTask Create(IDatabase database)
        { 
            switch (Type.Value)
            {
                case MembershipTaskType.Billing:
                    return new BillingRemindOrSettleTask(database, Membership.Value);
                case MembershipTaskType.PaymentParameterUpdate:
                    return new PaymentParameterUpdateReminderTask(database, Membership.Value);
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public interface IMembershipTask
    {
        void Execute();

        string Title(Translator translator);

        string Validate();
    }
}

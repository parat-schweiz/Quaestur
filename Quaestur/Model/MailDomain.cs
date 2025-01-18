using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public enum MailDomainType
    {
        Private = 0,
        Hoster = 1,
    }

    public static class MailDomainTypeExtensions
    {
        public static string Translate(this MailDomainType type, Translator translator)
        {
            switch (type)
            {
                case MailDomainType.Private:
                    return translator.Get("Enum.MailDomainType.Private", "Value 'Private' in MailDomainType enum", "Private");
                case MailDomainType.Hoster:
                    return translator.Get("Enum.MailDomainType.Hoster", "Value 'Hoster' in MailDomainType enum", "Hoster");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class MailDomain : DatabaseObject
    {
        public StringField Value { get; private set; }
        public EnumField<MailDomainType> Type { get; private set; }
        public Field<long> Checked { get; private set; }
        public Field<long> Subscribed { get; private set; }
        public Field<long> Unsubscribed { get; private set; }

        public MailDomain() : this(Guid.Empty)
        {
        }

        public MailDomain(Guid id) : base(id)
        {
            Value = new StringField(this, "value", 256);
            Type = new EnumField<MailDomainType>(this, "type", MailDomainType.Hoster, MailDomainTypeExtensions.Translate);
            Checked = new Field<long>(this, "checked", 0);
            Subscribed = new Field<long>(this, "subscribed", 0);
            Unsubscribed = new Field<long>(this, "unsubscribed", 0);
        }

        public override string ToString()
        {
            return Value.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Value.Value;
        }
    }
}

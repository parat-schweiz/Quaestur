using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public enum ParticipantType
    { 
        From = 0,
        To = 1,
        CC = 2,
        BCC = 3,
    }

    public static class ParticipantTypeExtensions
    {
        public static string Translate(this ParticipantType type, Translator translator)
        {
            switch (type)
            {
                case ParticipantType.From:
                    return translator.Get("Enum.ParticipantType.From", "From value in the participant type enum", "From");
                case ParticipantType.To:
                    return translator.Get("Enum.ParticipantType.To", "To value in the participant type enum", "To");
                case ParticipantType.CC:
                    return translator.Get("Enum.ParticipantType.CC", "CC value in the participant type enum", "CC");
                case ParticipantType.BCC:
                    return translator.Get("Enum.ParticipantType.BCC", "BCC value in the participant type enum", "BCC");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Participant : DatabaseObject
    {
        public ForeignKeyField<Article, Participant> Article { get; private set; }
        public EnumField<ParticipantType> Type { get; private set; }
        public ForeignKeyField<Contact, Participant> Contact { get; private set; }
        public StringField Name { get; private set; }
        public StringField Address { get; private set; }

        public Participant() : this(Guid.Empty)
        {
        }

        public Participant(Guid id) : base(id)
        {
            Article = new ForeignKeyField<Article, Participant>(this, "articleid", false, a => a.Participants);
            Type = new EnumField<ParticipantType>(this, "type", ParticipantType.From, ParticipantTypeExtensions.Translate);
            Contact = new ForeignKeyField<Contact, Participant>(this, "contact", true, null);
            Name = new StringField(this, "name", 512);
            Address = new StringField(this, "address", 512);
        }

        public override string ToString()
        {
            return Name.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value;
        }
    }
}

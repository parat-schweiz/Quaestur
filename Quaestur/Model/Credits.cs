using System;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class Credits : DatabaseObject
    {
        public ForeignKeyField<Person, Credits> Owner { get; private set; }
        public FieldDateTime Moment { get; private set; }
        public Field<int> Amount { get; private set; }
        public StringField Reason { get; private set; }
        public StringField Url { get; private set; }
        public EnumField<InteractionReferenceType> ReferenceType { get; private set; }
        public Field<Guid> ReferenceId { get; private set; }

        public Credits() : this(Guid.Empty)
        {
        }

        public Credits(Guid id) : base(id)
        {
            Owner = new ForeignKeyField<Person, Credits>(this, "ownerid", false, null);
            Moment = new FieldDateTime(this, "moment", DateTime.UtcNow);
            Amount = new Field<int>(this, "amount", 0);
            Reason = new StringField(this, "reason", 4096, AllowStringType.SimpleText);
            Url = new StringField(this, "url", 2048, AllowStringType.SimpleText);
            ReferenceType = new EnumField<InteractionReferenceType>(this, "referencetype", InteractionReferenceType.None, InteractionReferenceTypeExtensions.Translate);
            ReferenceId = new Field<Guid>(this, "referenceid", Guid.Empty);
        }

        public override string ToString()
        {
            return Reason.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Reason.Value;
        }
    }
}
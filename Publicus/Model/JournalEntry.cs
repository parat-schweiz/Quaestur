using System;
using System.Collections.Generic;

namespace Publicus
{
    public class JournalEntry : DatabaseObject
    {
		public ForeignKeyField<Contact, JournalEntry> Contact { get; private set; }
        public Field<DateTime> Moment { get; private set; }
        public StringField Subject { get; private set; }
        public StringField Text { get; private set; }

        public JournalEntry() : this(Guid.Empty)
        {
        }

        public JournalEntry(Guid id) : base(id)
        {
            Contact = new ForeignKeyField<Contact, JournalEntry>(this, "contactid", false, null);
            Moment = new Field<DateTime>(this, "moment", DateTime.UtcNow);
            Subject = new StringField(this, "subject", 256);
            Text = new StringField(this, "text", 32768);
        }

        public override string GetText(Translator translator)
        {
            return translator.Get(
                "JournalEntry.Text",
                "Textual representation of journal entry",
                "{0} changed {1} at {2}: {3}",
                Subject.GetText(translator),
                Contact.GetText(translator),
                Moment.GetText(translator),
                Text.Value);
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

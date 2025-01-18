using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class JournalEntry : DatabaseObject
    {
		public ForeignKeyField<Person, JournalEntry> Person { get; private set; }
        public DateTimeField Moment { get; private set; }
        public StringField Subject { get; private set; }
        public StringField Text { get; private set; }

        public JournalEntry() : this(Guid.Empty)
        {
        }

        public JournalEntry(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, JournalEntry>(this, "personid", false, null);
            Moment = new DateTimeField(this, "moment", DateTime.UtcNow);
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
                Person.GetText(translator),
                Moment.GetText(translator),
                Text.Value);
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

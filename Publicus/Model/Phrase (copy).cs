using System;
using System.Collections.Generic;

namespace Quaestur
{
    public enum Language
    {
        English = 0,
        German = 1,
        French = 2,
        Italian = 3,
    }

    public class Phrase : DatabaseObject
    {
        public Field<Guid> TextId { get; set; }
        public EnumField<Language> Language { get; set; }
		public StringField Text { get; set; }

        public Phrase() : this(Guid.Empty)
        {
        }

		public Phrase(Guid id) : base(id)
        {
            TextId = new Field<Guid>(this, "textid", Guid.NewGuid());
            Language = new EnumField<Language>(this, "language", Quaestur.Language.English);
            Text = new StringField(this, "text", 4096);
        }
    }
}

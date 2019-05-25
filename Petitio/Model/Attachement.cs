using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public class Attachement : DatabaseObject
    {
        public ForeignKeyField<Article, Attachement> Article { get; private set; }
        public StringField Filename { get; private set; }
        public StringField ContentType { get; private set; }
        public ByteArrayField Data { get; private set; }

        public Attachement() : this(Guid.Empty)
        {
        }

        public Attachement(Guid id) : base(id)
        {
            Article = new ForeignKeyField<Article, Attachement>(this, "articleid", false, a => a.Attachements);
            Filename = new StringField(this, "filename", 1024);
            ContentType = new StringField(this, "contenttype", 256);
            Data = new ByteArrayField(this, "data", false);
        }

        public override string ToString()
        {
            return Filename.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Filename.Value;
        }
    }
}

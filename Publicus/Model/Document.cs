using System;
using System.Collections.Generic;

namespace Publicus
{
    public enum DocumentType
    {
        Other = 0,
        Verification = 1,
    }

    public static class DocumentTypeExtensions
    {
        public static string Translate(this DocumentType type, Translator translator)
        {
            switch (type)
            {
                case DocumentType.Other:
                    return translator.Get("Enum.DocumentType.Other", "Value 'Other' in DocumentType enum", "Other");
                case DocumentType.Verification:
                    return translator.Get("Enum.DocumentType.Verification", "Value 'Verification' in DocumentType enum", "Verification");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Document : DatabaseObject
    {
        public ForeignKeyField<Contact, Document> Contact { get; private set; }
        public ForeignKeyField<Contact, Document> Verifier { get; private set; }
        public Field<DateTime> CreatedDate { get; private set; }
        public StringField FileName { get; private set; }
        public StringField ContentType { get; private set; }
        public FieldClass<byte[]> Data { get; private set; }
        public EnumField<DocumentType> Type { get; private set; }

        public Document() : this(Guid.Empty)
        {
        }

        public Document(Guid id) : base(id)
        {
            Contact = new ForeignKeyField<Contact, Document>(this, "contactid", false, null);
            Verifier = new ForeignKeyField<Contact, Document>(this, "verifierid", true, null);
            CreatedDate = new Field<DateTime>(this, "createddate", DateTime.UtcNow);
            FileName = new StringField(this, "filename", 512);
            ContentType = new StringField(this, "contenttype", 128);
            Data = new FieldClass<byte[]>(this, "data", false);
            Type = new EnumField<DocumentType>(this, "documenttype", DocumentType.Other, DocumentTypeExtensions.Translate);
        }

        public override string GetText(Translator translator)
        {
            return string.Format("{0} ({1})", FileName.Value, Type.Value.Translate(translator));
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

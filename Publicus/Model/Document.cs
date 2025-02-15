using System;
using System.Collections.Generic;
using SiteLibrary;

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
        public DateField CreatedDate { get; private set; }
        public StringField FileName { get; private set; }
        public StringField ContentType { get; private set; }
        public ByteArrayField Data { get; private set; }
        public EnumField<DocumentType> Type { get; private set; }

        public Document() : this(Guid.Empty)
        {
        }

        public Document(Guid id) : base(id)
        {
            Contact = new ForeignKeyField<Contact, Document>(this, "contactid", false, null);
            Verifier = new ForeignKeyField<Contact, Document>(this, "verifierid", true, null);
            CreatedDate = new DateField(this, "createddate", DateTime.UtcNow);
            FileName = new StringField(this, "filename", 512);
            ContentType = new StringField(this, "contenttype", 128);
            Data = new ByteArrayField(this, "data", false);
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

using System;
using System.Collections.Generic;

namespace Publicus
{
    public enum MailingElementType
    {
        Header,
        Footer,
    }

    public static class MailingElementTypeExtensions
    {
        public static string Translate(this MailingElementType type, Translator translator)
        {
            switch (type)
            {
                case MailingElementType.Header:
                    return translator.Get("Enum.MailingElementType.Header", "Header value in the mailing element type enum", "Header");
                case MailingElementType.Footer:
                    return translator.Get("Enum.MailingElementType.Footer", "Footer value in the mailing element type enum", "Footer");
                default:
                    throw new NotSupportedException(); 
            } 
        } 
    }

    public class MailingElement : DatabaseObject
    {
        public ForeignKeyField<Feed, MailingElement> Owner { get; private set; }
		public StringField Name { get; private set; }
        public StringField HtmlText { get; private set; }
        public StringField PlainText { get; private set; }
        public EnumField<MailingElementType> Type { get; private set; }

        public MailingElement() : this(Guid.Empty)
        {
        }

        public MailingElement(Guid id) : base(id)
        {
            Owner = new ForeignKeyField<Feed, MailingElement>(this, "ownerid", false, null);
            Name = new StringField(this, "name", 256);
            HtmlText = new StringField(this, "htmltext", 33554432, AllowStringType.SafeHtml);
            PlainText = new StringField(this, "plaintext", 33554432, AllowStringType.SafeHtml);
            Type = new EnumField<MailingElementType>(this, "elementtype", MailingElementType.Header, MailingElementTypeExtensions.Translate);
        }

        public override void Delete(IDatabase database)
        {
            foreach (var element in database.Query<Mailing>(DC.Equal("headerid", Id.Value)))
            {
                element.Delete(database);
            }

            foreach (var element in database.Query<Mailing>(DC.Equal("footerid", Id.Value)))
            {
                element.Delete(database);
            }

            database.Delete(this); 
        }

        public override string ToString()
        {
            return Name.Value;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value;
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public enum SystemWideFileType
    {
        HeaderImage = 0,
        Favicon = 1,
    }

    public static class SystemWideFileTypeExtensions
    {
        public static string Translate(this SystemWideFileType type, Translator translator)
        {
            switch (type)
            {
                case SystemWideFileType.HeaderImage:
                    return translator.Get("Enum.SystemWideFileType.HeaderImage", "Header image value of the system wide file type enum", "Header image");
                case SystemWideFileType.Favicon:
                    return translator.Get("Enum.SystemWideFileType.Favicon", "Favicon value of the system wide file type enum", "Favicon");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class SystemWideFile : DatabaseObject
    {
        public EnumField<SystemWideFileType> Type { get; private set; }
        public StringField FileName { get; private set; }
        public StringField ContentType { get; private set; }
        public ByteArrayField Data { get; private set; }

        public SystemWideFile() : this(Guid.Empty)
        {
        }

		public SystemWideFile(Guid id) : base(id)
        {
            Type = new EnumField<SystemWideFileType>(this, "type", SystemWideFileType.HeaderImage, SystemWideFileTypeExtensions.Translate);
            FileName = new StringField(this, "filename", 256, AllowStringType.SimpleText);
            ContentType = new StringField(this, "contenttype", 256, AllowStringType.SimpleText);
            Data = new ByteArrayField(this, "data", false);
        }

        public override string GetText(Translator translator)
        {
            return Type.Value.Translate(translator);
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

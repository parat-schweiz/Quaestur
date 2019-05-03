using System;
using System.Collections.Generic;

namespace Publicus
{
    [Flags]
    public enum TagMode
    {
        None = 0,
        Manual = 1,
        Self = 2,
        Default = 4,
    }

    public static class TagModeExtensions
    {
        public static string Translate(this TagMode mode, Translator translator)
        {
            switch (mode)
            {
                case TagMode.None:
                    return translator.Get("Enum.TagMode.None", "None value in the tag mode flag enum", "None");
                case TagMode.Default:
                    return translator.Get("Enum.TagMode.Default", "Default value in the tag mode flag enum", "Default");
                case TagMode.Manual:
                    return translator.Get("Enum.TagMode.Manual", "Manual value in the tag mode flag enum", "Manual");
                case TagMode.Self:
                    return translator.Get("Enum.TagMode.Self", "Self value in the tag mode flag enum", "Self");
                default:
                    throw new NotSupportedException(); 
            }
        }
    }

    [Flags]
    public enum TagUsage
    { 
        None = 0,
        Mailing = 1,
    }

    public static class TagUsageExtensions
    {
        public static string Translate(this TagUsage usage, Translator translator)
        {
            switch (usage)
            {
                case TagUsage.None:
                    return translator.Get("Enum.TagUsage.None", "None value in the tag usage flag enum", "None");
                case TagUsage.Mailing:
                    return translator.Get("Enum.TagUsage.Mailing", "Mailing value in the tag usage flag enum", "Mailing");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Tag : DatabaseObject
    {
		public MultiLanguageStringField Name { get; set; }
        public EnumField<TagMode> Mode { get; set; }
        public EnumField<TagUsage> Usage { get; set; }

        public Tag() : this(Guid.Empty)
        {
        }

		public Tag(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Mode = new EnumField<TagMode>(this, "mode", TagMode.None, TagModeExtensions.Translate);
            Usage = new EnumField<TagUsage>(this, "usage", TagUsage.None, TagUsageExtensions.Translate);
        }

        public override void Delete(IDatabase database)
        {
            foreach (var mailing in database.Query<Mailing>(DC.Equal("recipienttagid", Id.Value)))
            {
                mailing.RecipientTag.Value = null;
                database.Save(mailing);
            }

            foreach (var tagAssignment in database.Query<TagAssignment>(DC.Equal("tagid", Id.Value)))
            {
                tagAssignment.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

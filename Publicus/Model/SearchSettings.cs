using System;
using System.Collections.Generic;

namespace Publicus
{
    [Flags]
    public enum ContactColumns
    {
        None = 0,
        Organization = 1,
        Name = 2,
        Street = 4,
        Place = 8,
        State = 16,
        Mail = 32,
        Phone = 64,
        Subscriptions = 128,
        Tags = 256,
        Default = Organization | Name | Place | State,
    }

    public static class ContactColumnsExtensions
    {
        public static IEnumerable<ContactColumns> Flags
        {
            get
            {
                var flags = new List<ContactColumns>();
                flags.Add(ContactColumns.Organization);
                flags.Add(ContactColumns.Name);
                flags.Add(ContactColumns.Street);
                flags.Add(ContactColumns.Place);
                flags.Add(ContactColumns.State);
                flags.Add(ContactColumns.Mail);
                flags.Add(ContactColumns.Phone);
                flags.Add(ContactColumns.Subscriptions);
                flags.Add(ContactColumns.Tags);
                return flags;
            }
        }

        public static string TranslateFlag(this ContactColumns value, Translator translator)
        {
            if (value == ContactColumns.None)
            {
                return ContactColumns.None.Translate(translator);
            }
            else
            {
                var list = new List<string>();

                foreach (var flag in Flags)
                {
                    if (value.HasFlag(flag))
                        list.Add(flag.Translate(translator));
                }

                return string.Join(", ", list);
            }
        }

        public static string Translate(this ContactColumns value, Translator translator)
        {
            switch (value)
            {
                case ContactColumns.None:
                    return translator.Get("Enum.ContactColumns.None", "Value 'None' in ContactColumns enum", "None");
                case ContactColumns.Organization:
                    return translator.Get("Enum.ContactColumns.Organization", "Value 'Organization' in ContactColumns enum", "Organization");
                case ContactColumns.Name:
                    return translator.Get("Enum.ContactColumns.Name", "Value 'Name' in ContactColumns enum", "Name");
                case ContactColumns.Street:
                    return translator.Get("Enum.ContactColumns.Street", "Value 'Street' in ContactColumns enum", "Street");
                case ContactColumns.Place:
                    return translator.Get("Enum.ContactColumns.Place", "Value 'Place' in ContactColumns enum", "Place");
                case ContactColumns.State:
                    return translator.Get("Enum.ContactColumns.State", "Value 'State' in ContactColumns enum", "State");
                case ContactColumns.Mail:
                    return translator.Get("Enum.ContactColumns.Mail", "Value 'Mail' in ContactColumns enum", "Mail");
                case ContactColumns.Phone:
                    return translator.Get("Enum.ContactColumns.Phone", "Value 'Phone' in ContactColumns enum", "Phone");
                case ContactColumns.Subscriptions:
                    return translator.Get("Enum.ContactColumns.Subscriptions", "Value 'Subscriptions' in ContactColumns enum", "Subscriptions");
                case ContactColumns.Tags:
                    return translator.Get("Enum.ContactColumns.Tags", "Value 'Tags' in ContactColumns enum", "Tags");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class SearchSettings : DatabaseObject
    {
        public ForeignKeyField<User, SearchSettings> User { get; private set; }
        public StringField Name { get; private set; }
        public ForeignKeyField<Feed, SearchSettings> FilterSubscription { get; private set; }
        public ForeignKeyField<Tag, SearchSettings> FilterTag { get; private set; }
        public StringField FilterText { get; private set; }
        public Field<int> ItemsPerPage { get; private set; }
        public Field<int> CurrentPage { get; private set; }
        public EnumField<ContactColumns> Columns { get; private set; }

        public SearchSettings() : this(Guid.Empty)
        {
        }

        public SearchSettings(Guid id) : base(id)
        {
            User = new ForeignKeyField<User, SearchSettings>(this, "userid", false, null);
            Name = new StringField(this, "name", 256);
            FilterSubscription = new ForeignKeyField<Feed, SearchSettings>(this, "filtersubscription", true, null);
            FilterTag = new ForeignKeyField<Tag, SearchSettings>(this, "filtertag", true, null);
            FilterText = new StringField(this, "filtertext", 256);
            ItemsPerPage = new Field<int>(this, "itemsperpage", 20);
            CurrentPage = new Field<int>(this, "currentpage", 0);
            Columns = new EnumField<ContactColumns>(this, "columns", ContactColumns.Default, ContactColumnsExtensions.TranslateFlag);
        }

        public override string ToString()
        {
            return Name.Value;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }
    }
}

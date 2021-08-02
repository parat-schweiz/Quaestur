using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    [Flags]
    public enum PersonColumns
    { 
        None = 0,
        Number = 1,
        User = 2,
        Name = 4,
        Street = 8,
        Place = 16,
        State = 32,
        Mail = 64,
        Phone = 128,
        Memberships = 256,
        Roles = 512,
        Tags = 1024,
        VotingRight = 2048,
        Default = User | Name | Place | State,
    }

    public static class PersonColumnsExtensions
    {
        public static IEnumerable<PersonColumns> Flags
        {
            get
            {
                var flags = new List<PersonColumns>();
                flags.Add(PersonColumns.Number);
                flags.Add(PersonColumns.User);
                flags.Add(PersonColumns.Name);
                flags.Add(PersonColumns.Street);
                flags.Add(PersonColumns.Place);
                flags.Add(PersonColumns.State);
                flags.Add(PersonColumns.Mail);
                flags.Add(PersonColumns.Phone);
                flags.Add(PersonColumns.Memberships);
                flags.Add(PersonColumns.Roles);
                flags.Add(PersonColumns.Tags);
                flags.Add(PersonColumns.VotingRight);
                return flags;
            }
        }

        public static string TranslateFlag(this PersonColumns value, Translator translator)
        {
            if (value == PersonColumns.None)
            {
                return PersonColumns.None.Translate(translator);
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

        public static string Translate(this PersonColumns value, Translator translator)
        {
            switch (value)
            {
                case PersonColumns.None:
                    return translator.Get("Enum.PersonColumns.None", "Value 'None' in PersonColumns enum", "None");
                case PersonColumns.Number:
                    return translator.Get("Enum.PersonColumns.Number", "Value 'Number' in PersonColumns enum", "Number");
                case PersonColumns.User:
                    return translator.Get("Enum.PersonColumns.User", "Value 'User' in PersonColumns enum", "User");
                case PersonColumns.Name:
                    return translator.Get("Enum.PersonColumns.Name", "Value 'Name' in PersonColumns enum", "Name");
                case PersonColumns.Street:
                    return translator.Get("Enum.PersonColumns.Street", "Value 'Street' in PersonColumns enum", "Street");
                case PersonColumns.Place:
                    return translator.Get("Enum.PersonColumns.Place", "Value 'Place' in PersonColumns enum", "Place");
                case PersonColumns.State:
                    return translator.Get("Enum.PersonColumns.State", "Value 'State' in PersonColumns enum", "State");
                case PersonColumns.Mail:
                    return translator.Get("Enum.PersonColumns.Mail", "Value 'Mail' in PersonColumns enum", "Mail");
                case PersonColumns.Phone:
                    return translator.Get("Enum.PersonColumns.Phone", "Value 'Phone' in PersonColumns enum", "Phone");
                case PersonColumns.Memberships:
                    return translator.Get("Enum.PersonColumns.Memberships", "Value 'Memberships' in PersonColumns enum", "Memberships");
                case PersonColumns.Roles:
                    return translator.Get("Enum.PersonColumns.Roles", "Value 'Roles' in PersonColumns enum", "Roles");
                case PersonColumns.Tags:
                    return translator.Get("Enum.PersonColumns.Tags", "Value 'Tags' in PersonColumns enum", "Tags");
                case PersonColumns.VotingRight:
                    return translator.Get("Enum.PersonColumns.VotingRight", "Value 'Voting right' in PersonColumns enum", "Voting right");
                default:
                    throw new NotSupportedException();
            } 
        }
    }

    public class SearchSettings : DatabaseObject
    {
        public ForeignKeyField<Person, SearchSettings> Person { get; private set; }
        public StringField Name { get; private set; }
        public ForeignKeyField<MembershipType, SearchSettings> FilterMembership { get; private set; }
        public ForeignKeyField<Tag, SearchSettings> FilterTag { get; private set; }
        public StringField FilterText { get; private set; }
        public Field<int> ItemsPerPage { get; private set; }
        public Field<int> CurrentPage { get; private set; }
        public EnumField<PersonColumns> Columns { get; private set; }

        public SearchSettings() : this(Guid.Empty)
        {
        }

        public SearchSettings(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, SearchSettings>(this, "personid", false, null);
            Name = new StringField(this, "name", 256);
            FilterMembership = new ForeignKeyField<MembershipType, SearchSettings>(this, "filtermembership", true, null);
            FilterTag = new ForeignKeyField<Tag, SearchSettings>(this, "filtertag", true, null);
            FilterText = new StringField(this, "filtertext", 256);
            ItemsPerPage = new Field<int>(this, "itemsperpage", 20);
            CurrentPage = new Field<int>(this, "currentpage", 0);
            Columns = new EnumField<PersonColumns>(this, "columns", PersonColumns.Default, PersonColumnsExtensions.TranslateFlag);
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

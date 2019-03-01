using System;
using System.Collections.Generic;

namespace Quaestur
{
    public class SearchSettings : DatabaseObject
    {
        public ForeignKeyField<Person, SearchSettings> Person { get; private set; }
        public StringField Name { get; private set; }
        public StringField FilterText { get; private set; }
        public Field<int> ItemsPerPage { get; private set; }
        public Field<int> CurrentPage { get; private set; }
        public Field<bool> ShowNumber { get; private set; }
        public Field<bool> ShowUser { get; private set; }
        public Field<bool> ShowName { get; private set; }
        public Field<bool> ShowStreet { get; private set; }
        public Field<bool> ShowPlace { get; private set; }
        public Field<bool> ShowState { get; private set; }
        public Field<bool> ShowMail { get; private set; }
        public Field<bool> ShowPhone { get; private set; }

        public SearchSettings() : this(Guid.Empty)
        {
        }

        public SearchSettings(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, SearchSettings>(this, "personid", false, null);
            Name = new StringField(this, "name", 256);
            FilterText = new StringField(this, "filtertext", 256);
            ItemsPerPage = new Field<int>(this, "itemsperpage", 20);
            CurrentPage = new Field<int>(this, "currentpage", 0);
            ShowNumber = new Field<bool>(this, "shownumber", false);
            ShowUser = new Field<bool>(this, "showuser", true);
            ShowName = new Field<bool>(this, "showname", true);
            ShowStreet = new Field<bool>(this, "showstreet", true);
            ShowPlace = new Field<bool>(this, "showplace", true);
            ShowState = new Field<bool>(this, "showstate", false);
            ShowMail = new Field<bool>(this, "showmail", false);
            ShowPhone = new Field<bool>(this, "showphone", false);
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

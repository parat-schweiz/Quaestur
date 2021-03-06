﻿using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class Export : DatabaseObject
    {
        public StringField Name { get; private set; }
        public ForeignKeyField<Organization, Export> SelectOrganization { get; private set; }
        public ForeignKeyField<Tag, Export> SelectTag { get; private set; }
        public EnumNullField<Language> SelectLanguage { get; private set; }
        public StringListField ExportColumns { get; private set; }

        public Export() : this(Guid.Empty)
        {
        }

        public Export(Guid id) : base(id)
        {
            Name = new StringField(this, "name", 256);
            SelectOrganization = new ForeignKeyField<Organization, Export>(this, "selectorganizationid", true, null);
            SelectTag = new ForeignKeyField<Tag, Export>(this, "selecttagid", true, null);
            SelectLanguage = new EnumNullField<Language>(this, "selectlanguage", LanguageExtensions.Translate);
            ExportColumns = new StringListField(this, "exportcolumns");
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

﻿using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class UniversalDocument : TemplateDocument
    {
        private readonly IDatabase _database;
        private readonly Translator _translator;
        private readonly Person _person;
        private readonly string _texTemplate;

        public UniversalDocument(IDatabase database, Translator translator, Person person, string texTemplate)
        {
            _database = database;
            _translator = translator;
            _person = person;
            _texTemplate = texTemplate;
        }

        protected override string TexTemplate
        {
            get { return _texTemplate; }
        }

        protected override Templator GetTemplator()
        {
            return new Templator(new PersonContentProvider(_database, _translator, _person));
        }
    }
}

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class UniversalDocument : TemplateDocument
    {
        private readonly Translator _translator;
        private readonly Person _person;
        private readonly string _texTemplate;

        public UniversalDocument(Translator translator, Person person, string texTemplate)
        {
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
            return new Templator(new PersonContentProvider(_translator, _person));
        }
    }
}

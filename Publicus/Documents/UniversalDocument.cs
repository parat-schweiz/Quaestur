using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public class UniversalDocument : TemplateDocument
    {
        private readonly Translator _translator;
        private readonly Contact _contact;
        private readonly string _texTemplate;

        public UniversalDocument(Translator translator, Contact contact, string texTemplate)
        {
            _translator = translator;
            _contact = contact;
            _texTemplate = texTemplate;
        }

        protected override string TexTemplate
        {
            get { return _texTemplate; }
        }

        protected override Templator GetTemplator()
        {
            return new Templator(new ContactContentProvider(_translator, _contact));
        }
    }
}

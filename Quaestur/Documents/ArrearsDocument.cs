using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class ArrearsDocument : TemplateDocument
    {
        private readonly IDatabase _database;
        private readonly Translator _translator;
        private readonly Organization _organization;
        private readonly Person _person;
        private readonly IEnumerable<Bill> _bills;
        private readonly string _texTemplate;

        public ArrearsDocument(IDatabase database, Translator translator, Organization organization, Person person, IEnumerable<Bill> bills, string texTemplate)
        {
            _database = database;
            _translator = translator;
            _organization = organization;
            _person = person;
            _bills = bills;
            _texTemplate = texTemplate;
        }

        protected override string TexTemplate
        {
            get { return _texTemplate; }
        }

        protected override Templator GetTemplator()
        {
            return new Templator(
                new PersonContentProvider(_translator, _person),
                new ArrearsContentProvider(_database, _translator, _person, _bills));
        }

        public override IEnumerable<Tuple<string, byte[]>> Files
        {
            get
            {
                var message = _translator.Get(
                    "Arrears.Document.QrBill.Message",
                    "QR bill message on the arrears document",
                    "Arrears");
                var amount = _bills.Sum(b => b.Amount) - _person.CurrentPrepayment(_database);
                yield return new Tuple<string, byte[]>("qrcode.png",
                    SwissQrBill.Create(_database, _translator, _organization, _person, amount, message));
            }
        }
    }
}

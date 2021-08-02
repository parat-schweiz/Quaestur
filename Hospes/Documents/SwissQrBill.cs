using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using BaseLibrary;
using QRCoder;
using SiteLibrary;

namespace Hospes
{
    public class SwissQrBillBuilder
    {
        private StringBuilder _text;

        public SwissQrBillBuilder()
        {
            _text = new StringBuilder();
        }

        public void Add(string line, int length)
        {
            if (line.Length > length)
            {
                line = line.Substring(0, length); 
            }

            _text.AppendLine(line);
        }

        public void Add()
        {
            _text.AppendLine();
        }

        public override string ToString()
        {
            return _text.ToString();
        }
    }

    public class SwissQrBill
    {
        public static byte[] Create(IDatabase database, Translator translator, Organization organization, Person person, decimal amount, string message)
        {
            var qrBill = new SwissQrBill(database, translator, organization, person, amount, message);
            return qrBill.CreateImage();
        }

        private IDatabase _database;
        private Translator _translator;
        private Organization _organization;
        private Person _person;
        private decimal _amount;
        private string _message;

        public SwissQrBill(IDatabase database, Translator translator, Organization organization, Person person, decimal amount, string message)
        {
            _database = database;
            _translator = translator;
            _organization = organization;
            _person = person;
            _amount = amount;
            _message = message; 
        }

        private string RemoveWhitespace(string text)
        {
            return text
                .Replace(" ", string.Empty)
                .Replace("\t", string.Empty)
                .Replace("\n", string.Empty);
        }

        private string Text
        {
            get
            {
                if (_person.PrimaryPostalAddress == null)
                    throw new InvalidOperationException("Cannot create QR bill for lack of primary postal address of " + _person.ShortHand);

                var settings = _database.Query<SystemWideSettings>().Single();

                var text = new SwissQrBillBuilder();

                //QRCH.Header
                text.Add("SPC", 3); //QRType
                text.Add("0200", 4); //Version
                text.Add("1", 1); //Coding

                //QRCH.CdtrInf
                text.Add(RemoveWhitespace(_organization.BillIban.Value), 21); //IBAN

                //QRCH.CdtrInf.Cdtr
                text.Add("K", 1); //AdrTp
                text.Add(_organization.BillName.Value[_translator.Language], 70); //Name
                text.Add(_organization.BillStreet.Value[_translator.Language], 70); //StrtNmOrAdrLine1
                text.Add(_organization.BillLocation.Value[_translator.Language], 70); //BldgNbOrAdrLine2
                text.Add(); //PstCd
                text.Add(); //TwnNm
                text.Add(_organization.BillCountry.Value.Code.Value, 2); //Ctry

                //QRCH.UltmtCdtr
                text.Add(); //AdrTp
                text.Add(); //Name
                text.Add(); //StrtNmOrAdrLine1
                text.Add(); //BldgNbOrAdrLine2
                text.Add(); //PstCd
                text.Add(); //TwnNm
                text.Add(); //Ctry

                //QRCH.CcyAmt
                text.Add(_amount.FormatMoney(), 12); //Amt
                text.Add(settings.Currency.Value, 3); //Ccy

                //QRCH.UltmtDbtr
                text.Add("K", 1); //AdrTp
                text.Add(_person.ShortHand, 70); //Name
                text.Add(_person.PrimaryPostalAddress.StreetOrPostOfficeBox, 70); //StrtNmOrAdrLine1
                text.Add(_person.PrimaryPostalAddress.PlaceWithPostalCode, 70); //BldgNbOrAdrLine2
                text.Add(); //PstCd
                text.Add(); //TwnNm
                text.Add(_person.PrimaryPostalAddress.Country.Value.Code.Value, 2); //Ctry

                //QRCH.RmtInf
                text.Add("NON", 4); //Tp
                text.Add(); //Ref

                //QRCH.AddInf
                text.Add(_message, 140); //Ustrd
                text.Add("EPD", 3); //Trailer
                text.Add(); //StrdBkgInf

                return text.ToString();
            }
        }

        public byte[] CreateImage()
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(Text, QRCodeGenerator.ECCLevel.M);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(25);

            using (var stream = new MemoryStream())
            {
                qrCodeImage.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }
    }
}

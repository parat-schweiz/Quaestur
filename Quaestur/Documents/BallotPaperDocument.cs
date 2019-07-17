using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;
using SiteLibrary;

namespace Quaestur
{
    public class BallotPaperDocument : TemplateDocument, IContentProvider
    {
        private readonly Translator _translator;
        private readonly IDatabase _database;
        private readonly BallotPaper _ballotPaper;
        private readonly Membership _membership;
        private readonly Person _person;
        private readonly Ballot _ballot;
        private readonly BallotTemplate _template;

        public Bill Bill { get; private set; }

        public BallotPaperDocument(Translator translator, IDatabase db, BallotPaper ballotPaper)
        {
            _translator = translator;
            _database = db;
            _ballotPaper = ballotPaper;
            _membership = _ballotPaper.Member.Value;
            _person = _membership.Person.Value;
            _ballot = _ballotPaper.Ballot.Value;
            _template = _ballot.Template.Value;
        }

        public override bool Prepare()
        {
            return true;
        }

        protected override string TexTemplate
        {
            get { return _template.GetBallotPaper(_database, _translator.Language).Text.Value; }
        }

        protected override Templator GetTemplator()
        {
            return new Templator(
                new PersonContentProvider(_translator, _person),
                new BallotPaperContentProvider(_translator, _ballotPaper),
                this);
        }

        public string Prefix
        {
            get { return "BallotPaperDocument"; } 
        }

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "BallotPaperDocument.Questions":
                    return _ballot.Questions.Value[_translator.Language];
                case "BallotPaperDocument.Code":
                    return _ballotPaper.ComputeCode().ToHexStringGroupFour();
                case "BallotPaperDocument.VerificationLink":
                    return CreateVerificationLink();
                default:
                    throw new NotSupportedException();
            }
        }

        private string CreateVerificationLink()
        {
            return string.Format("{0}/ballotpaper/verify/{1}/{2}",
                Global.Config.WebSiteAddress,
                _ballotPaper.Id.Value,
                _ballotPaper.ComputeCode().ToHexString());
        }

        private byte[] CreateVerificationLinkQrImage()
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(CreateVerificationLink(), QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);

            using (var stream = new MemoryStream())
            {
                qrCodeImage.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        public override IEnumerable<Tuple<string, byte[]>> Files
        {
            get 
            {
                yield return new Tuple<string, byte[]>("qrcode.png", CreateVerificationLinkQrImage());
            } 
        }
    }
}

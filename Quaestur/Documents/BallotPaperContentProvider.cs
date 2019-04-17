using System;
using System.Linq;
using System.Collections.Generic;

namespace Quaestur
{
    public class BallotPaperContentProvider : IContentProvider
    {
        private readonly Translator _translator;
        private readonly BallotPaper _ballotPaper;

        public BallotPaperContentProvider(Translator translator, BallotPaper ballotPaper)
        {
            _translator = translator;
            _ballotPaper = ballotPaper;
        }

        public string Prefix
        {
            get { return "BallotPaper"; } 
        }

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "BallotPaper.BallotName":
                    return _ballotPaper.Ballot.Value.GetText(_translator);
                case "BallotPaper.DownloadLink":
                    return string.Format("{0}/ballotpaper", Global.Config.WebSiteAddress);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

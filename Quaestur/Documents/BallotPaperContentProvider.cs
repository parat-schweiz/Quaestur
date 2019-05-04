using System;
using System.Linq;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

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
                case "BallotPaper.Ballot.Date.Short":
                    return _translator.FormatShortDate(_ballotPaper.Ballot.Value.EndDate.Value);
                case "BallotPaper.Ballot.Date.Long":
                    return _translator.FormatLongDate(_ballotPaper.Ballot.Value.EndDate.Value);
                case "BallotPaper.Ballot.Name":
                    return _ballotPaper.Ballot.Value.GetText(_translator);
                case "BallotPaper.Ballot.AnnouncementText":
                    return _ballotPaper.Ballot.Value.AnnouncementText.Value[_translator.Language];
                case "BallotPaper.DownloadLink":
                    return string.Format("{0}/ballotpaper", Global.Config.WebSiteAddress);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

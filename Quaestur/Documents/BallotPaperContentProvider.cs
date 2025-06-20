using System;
using System.Linq;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;
using RedmineApi;
using System.Text;

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
                case "BallotPaper.Motions":
                    return CreateMotions();
                default:
                    throw new InvalidOperationException(
                        "Variable " + variable + " not known in provider " + Prefix);
            }
        }

        private string CreateMotions()
        {
            var redmine = new Redmine(Global.Config.RedmineApiConfig);
            var ballot = _ballotPaper.Ballot.Value;

            if ((!ballot.RedmineProject.Value.HasValue) ||
                (!ballot.RedmineVersion.Value.HasValue) ||
                (!ballot.RedmineStatus.Value.HasValue))
            {
                return "Redmine project, version or status not set for ballot.";
            }

            try
            {
                var issues = redmine
                    .GetIssues(ballot.RedmineProject.Value.Value)
                    .Where(i => (i.Status?.Id == ballot.RedmineStatus.Value.Value) &&
                                (i.Version?.Id == ballot.RedmineVersion.Value.Value))
                    .OrderBy(i => i.Id)
                    .ToList();
                var text = new StringBuilder();
                text.AppendLine("<ul>");

                foreach (var issue in issues.Where(i => !i.ParentId.HasValue))
                {
                    AddIssue(text, issue, issues);
                }

                text.AppendLine("</ul>");
                return text.ToString();
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        private static bool IsElection(Issue issue)
        {
            var category = issue.Category.Name.ToLowerInvariant();
            return category.Contains("wahl") ||
                   category.Contains("election") ||
                   category.Contains("éléction");
        }

        private static void AddIssue(StringBuilder text, Issue issue, IEnumerable<Issue> allIssues)
        {
            if (IsElection(issue))
            {
                AddElection(text, issue, allIssues);
            }
            else
            {
                AddVoting(text, issue, allIssues);
            }
        }

        private static void AddElection(StringBuilder text, Issue issue, IEnumerable<Issue> allIssues)
        {
            var discussionUrl = issue.CustomFields.FirstOrDefault(f => f.Name == "Diskussion")?.Values?.FirstOrDefault();
            var issueUrl = string.Format(
                "{0}/issues/{1}",
                Global.Config.RedmineApiConfig.ApiUrl,
                issue.Id);
            if (!string.IsNullOrEmpty(discussionUrl))
            {
                text.AppendLine(string.Format(
                    "<li>Geschäft {0}: <a href=\"{1}\">{2}</a> - <a href=\"{3}\">Diskussion zum Antrag</a></li>",
                    issue.Id,
                    issueUrl,
                    issue.Subject,
                    discussionUrl));
            }
            else
            {
                text.AppendLine(string.Format(
                    "<li>Geschäft {0}: <a href=\"{1}\">{2}</a></li>",
                    issue.Id,
                    issue.Subject,
                    issueUrl));
            }
        }

        private static void AddVoting(StringBuilder text, Issue issue, IEnumerable<Issue> allIssues)
        {
            var discussionUrl = issue.CustomFields.FirstOrDefault(f => f.Name == "Diskussion")?.Values?.FirstOrDefault();
            var issueUrl = string.Format(
                "{0}/issues/{1}",
                Global.Config.RedmineApiConfig.ApiUrl,
                issue.Id);
            if (!string.IsNullOrEmpty(discussionUrl))
            {
                text.AppendLine(string.Format(
                    "<li>Antrag {0}: <a href=\"{1}\">{2}</a> - <a href=\"{3}\">Diskussion zum Antrag</a></li>",
                    issue.Id,
                    issueUrl,
                    issue.Subject,
                    discussionUrl));
            }
            else
            {
                text.AppendLine(string.Format(
                    "<li>Antrag {0}: <a href=\"{1}\">{2}</a></li>",
                    issue.Id,
                    issue.Subject,
                    issueUrl));
            }
        }
    }
}

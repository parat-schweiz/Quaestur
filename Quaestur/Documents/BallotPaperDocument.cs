using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using QRCoder;
using SiteLibrary;
using RedmineApi;

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
        private readonly List<Issue> _issues;

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

            var redmine = new Redmine(Global.Config.RedmineApiConfig);

            if (_ballot.RedmineProject.Value.HasValue &&
                _ballot.RedmineVersion.Value.HasValue &&
                _ballot.RedmineStatus.Value.HasValue)
            {
                try
                {
                    _issues = redmine
                        .GetIssues(_ballot.RedmineProject.Value.Value)
                        .Where(i => (i.Status?.Id == _ballot.RedmineStatus.Value.Value) &&
                                    (i.Version?.Id == _ballot.RedmineVersion.Value.Value))
                        .OrderBy(i => i.Id)
                        .ToList();
                }
                catch (Exception exception)
                {
                    Global.Log.Warning("Could not get issues for ballot {0} due to {1}", _ballot, exception.Message);
                }
            }
        }

        public override bool Prepare()
        {
            return true;
        }

        protected override string TexTemplate
        {
            get { return _template.BallotPapers.Value(_database, _translator.Language).Text.Value; }
        }

        protected override Templator GetTemplator()
        {
            return new Templator(
                new PersonContentProvider(_database, _translator, _person),
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
                case "BallotPaperDocument.Motions":
                    return CreateMotions();
                default:
                    throw new InvalidOperationException(
                        "Variable " + variable + " not known in provider " + Prefix);
            }
        }

        private string GetUserValue(IEnumerable<User> users, string value)
        {
            if (int.TryParse(value, out int id))
            {
                var user = users.SingleOrDefault(u => u.Id == id);
                if (user != null)
                {
                    if (!string.IsNullOrEmpty(user.Firstname) && !string.IsNullOrEmpty(user.Lastname))
                    {
                        return user.Firstname + " " + user.Lastname;
                    }
                    else if (!string.IsNullOrEmpty(user.Firstname))
                    {
                        return user.Firstname;
                    }
                    else if (!string.IsNullOrEmpty(user.Lastname))
                    {
                        return user.Lastname;
                    }
                    else
                    {
                        return user.Username;
                    }
                }
            }
            return value;
        }

        private string GetIssuePoster(IEnumerable<User> users, Issue issue)
        {
            var customField = issue.CustomFields.SingleOrDefault(c => c.Name == "Antragsteller");
            if (customField != null)
            {
                var values = customField.Values
                    .Select(v => GetUserValue(users, v))
                    .OrderBy(v => v)
                    .ToList();
                if (values.Count == 1)
                {
                    return values.Single();
                }
                else
                {
                    return string.Join(", ", values.Take(values.Count - 1)) + " sowie " + values.Last();
                }
            }
            return issue.Author?.Name ?? "Unbekannt";
        }

        private string CreateMotions()
        {
            var redmine = new Redmine(Global.Config.RedmineApiConfig);
            var users = redmine.GetUsers().ToList();

            if (!_issues.Any())
            {
                return "No redmine issues for ballot available.";
            }

            var text = new StringBuilder();
            var issueCounter = 1;

            foreach (var issue in _issues.Where(i => !i.ParentId.HasValue))
            {
                AddIssue(users, _issues, text, issueCounter, issue);
                issueCounter++;
            }

            return text.ToString();
        }

        private void AddIssue(IEnumerable<User> users, IEnumerable<Issue> allIssues, StringBuilder text, int issueCounter, Issue issue)
        { 
            if (IsElection(issue))
            {
                AddElection(users, allIssues, text, issueCounter, issue);
            }
            else
            {
                AddVoting(users, allIssues, text, issueCounter, issue);
            }
        }

        private void AddElection(IEnumerable<User> users, IEnumerable<Issue> allIssues, StringBuilder text, int issueCounter, Issue issue)
        {
            var candidates = allIssues
                .Where(i => i.ParentId.HasValue && (i.ParentId.Value == issue.Id))
                .Where(i => i.Status.Id == issue.Status.Id)
                .OrderBy(i => i.Id)
                .ToList();
            var shortUrl = string.Format(
                "https://a.parat.swiss/{0}",
                issue.Id);
            var qrFilename = string.Format(
                "qr{0}.png",
                issue.Id);

            if (candidates.Any())
            {
                text.AppendLine(@"\begin{samepage}");
                text.AppendLine(@"\section*{Wahl §§§: $$$}"
                                .Replace("§§§", issueCounter.ToString())
                                .Replace("$$$", issue.Subject));
                text.AppendLine(@"\questionurl{§§§}{£££}"
                                .Replace("§§§", shortUrl)
                                .Replace("£££", qrFilename));

                var candidateCounter = 1;

                foreach (var candidate in candidates)
                {
                    var name = candidate.CustomFields
                        .Where(f => f.Name.StartsWith("Abstimmungstitel", StringComparison.Ordinal))
                        .Where(f => f.Values.Any())
                        .Select(f => f.Values.FirstOrDefault())
                        .FirstOrDefault(f => !string.IsNullOrEmpty(f))
                        ?? candidate.Subject;
                    text.AppendLine(@"\subsection*{Kandidat*in £££}"
                                    .Replace("§§§", issueCounter.ToString())
                                    .Replace("£££", candidateCounter.ToString()));
                    text.AppendLine(@"\question{Gibts du §§§ deine Stimme?}"
                                    .Replace("§§§", name));

                    candidateCounter++;
                }
                text.AppendLine(@"\end{samepage}");
            }

            text.AppendLine();
            text.AppendLine(@"\vspace{0.5cm}");
            text.AppendLine();
        }

        private void AddVoting(IEnumerable<User> users, IEnumerable<Issue> allIssues, StringBuilder text, int issueCounter, Issue issue)
        {
            var questions = issue.CustomFields
                .Where(f => f.Name.StartsWith("Abstimmungstitel", StringComparison.Ordinal))
                .Where(f => f.Values.Any())
                .Select(f => f.Values.FirstOrDefault())
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
            var shortUrl = string.Format(
                "https://a.parat.swiss/{0}",
                issue.Id);
            var qrFilename = string.Format(
                "qr{0}.png",
                issue.Id);

            if (questions.Any())
            {
                text.AppendLine(@"\begin{samepage}");
                text.AppendLine(@"\section*{Abstimmungsvorlage §§§}"
                                .Replace("§§§", issueCounter.ToString()));
                text.AppendLine(@"\questionurl{§§§}{£££}"
                                .Replace("§§§", shortUrl)
                                .Replace("£££", qrFilename));

                var questionCounter = 1;

                foreach (var question in questions)
                {
                    text.AppendLine(@"\subsection*{Frage £££}"
                                    .Replace("§§§", issueCounter.ToString())
                                    .Replace("£££", questionCounter.ToString()));
                    text.AppendLine(@"\question{Stimmst du dem Antrag von £££ auf §§§ zu?}"
                                    .Replace("§§§", question)
                                    .Replace("£££", GetIssuePoster(users, issue)));

                    questionCounter++;
                }
                text.AppendLine(@"\end{samepage}");
            }

            text.AppendLine();
            text.AppendLine(@"\vspace{0.5cm}");
            text.AppendLine();
        }

        private static bool IsElection(Issue issue)
        {
            var category = issue.Category.Name.ToLowerInvariant();
            return category.Contains("wahl") ||
                   category.Contains("election") ||
                   category.Contains("éléction");
        }

        private string CreateVerificationLink()
        {
            return string.Format("{0}/ballotpaper/verify/{1}/{2}",
                Global.Config.WebSiteAddress,
                _ballotPaper.Id.Value,
                _ballotPaper.ComputeCode().ToHexString());
        }

        private byte[] CreateQrImage(string text)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);
            Bitmap qrCodeImage = qrCode.GetGraphic(20);

            using (var stream = new MemoryStream())
            {
                qrCodeImage.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
        }

        private byte[] CreateVerificationLinkQrImage()
        {
            return CreateQrImage(CreateVerificationLink());
        }

        public override IEnumerable<Tuple<string, byte[]>> Files
        {
            get
            {
                yield return new Tuple<string, byte[]>("qrcode.png", CreateVerificationLinkQrImage());

                foreach (var issue in _issues)
                {
                    var shortUrl = string.Format(
                        "https://a.parat.swiss/{0}",
                        issue.Id);
                    var qrFilename = string.Format(
                        "qr{0}.png",
                        issue.Id);
                    yield return new Tuple<string, byte[]>(qrFilename, CreateQrImage(shortUrl));
                }
            } 
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PointsTallyDocument : TemplateDocument, IContentProvider
    {
        private readonly Translator _translator;
        private readonly IDatabase _database;
        private readonly Membership _membership;
        private readonly Person _person;
        private readonly SystemWideSettings _settings;
        private readonly IEnumerable<Points> _pointsOverride;
        private PointsTally _lastTally;

        public PointsTally PointsTally { get; private set; }

        public PointsTallyDocument(Translator translator, IDatabase database, Membership membership, IEnumerable<Points> pointsOverride = null)
        {
            _translator = translator;
            _database = database;
            _membership = membership;
            _person = _membership.Person.Value;
            _settings = _database.Query<SystemWideSettings>().Single();
            _pointsOverride = pointsOverride;
        }

        public bool Create()
        {
            if (Prepare())
            {
                var document = Compile();

                if (document != null)
                {
                    PointsTally.DocumentData.Value = document;
                    return true;
                }
            }

            return false;
        }

        private static Bill GetLastBill(IDatabase database, Membership membership)
        {
            return database
                .Query<Bill>(DC.Equal("membershipid", membership.Id.Value))
                .OrderByDescending(b => b.UntilDate.Value).FirstOrDefault();
        }

        private static int GetBefore(int period)
        {
            if (period <= 7)
            {
                return 3; 
            }
            else if (period <= 16)
            {
                return 5; 
            }
            else if (period <= 66)
            {
                return 10; 
            }
            else if (period <= 99)
            {
                return 15;
            }
            else
            {
                return 20;
            }
        }

        public static DateTime ComputeFromDate(IDatabase database, Membership membership, PointsTally lastTally)
        {
            if (lastTally == null)
            {
                return membership.StartDate.Value.Date;
            }
            else
            {
                return lastTally.UntilDate.Value.AddDays(1).Date;
            }
        }

        public static DateTime ComputeUntilDate(IDatabase database, Membership membership, PointsTally lastTally)
        {
            if (membership.EndDate.Value.HasValue)
            {
                return membership.EndDate.Value.Value.Date;
            }
            else
            {
                var mainModel = membership.Type.Value.CreatePaymentModel(database);
                var period = mainModel.GetBillingPeriod();
                var lastBill = GetLastBill(database, membership);
                var before = GetBefore(period);

                if (lastBill == null)
                {
                    return membership.StartDate.Value.AddDays(period - before).Date;
                }
                else
                {
                    return lastBill.UntilDate.Value.AddDays(1 + period - before).Date;
                }
            }
        }

        public override bool Prepare()
        {
            PointsTally = new PointsTally(Guid.NewGuid());
            PointsTally.Person.Value = _person;
            PointsTally.CreatedDate.Value = DateTime.UtcNow.Date;

            _lastTally = _database
                .Query<PointsTally>(DC.Equal("personid", _person.Id.Value))
                .OrderByDescending(t => t.UntilDate.Value)
                .FirstOrDefault();

            PointsTally.FromDate.Value = ComputeFromDate(_database, _membership, _lastTally);
            PointsTally.UntilDate.Value = ComputeUntilDate(_database, _membership, _lastTally);

            return true;
        }

        protected override string TexTemplate
        {
            get { return _membership.Type.Value.GetPointsTallyDocument(_database, _translator.Language).Text.Value; } 
        }

        private string CreateTableContent()
        {
            var tableHead = _translator.Get("Document.PointsTally.Table.Head", "Head of the table in the points tally document", "Points");
            var tableColumnReason = _translator.Get("Document.PointsTally.Column.Reason", "Reason column in the points tally document", "Reason");
            var tableColumnDate = _translator.Get("Document.PointsTally.Column.Date", "Date column in the points tally document", "Date");
            var tableColumnPoints = _translator.Get("Document.PointsTally.Column.Points", "Points column in the points tally document", "Points");
            var tableColumnBalance = _translator.Get("Document.PointsTally.Column.Balance", "Balance column in the points tally document", "Balance");
            var tableRowConsidered = _translator.Get("Document.PointsTally.Row.Considered", "Considered row in the points tally document", "Considered");
            var tableRowBalanceForward = _translator.Get("Document.PointsTally.Row.BalanceForward", "Balance forward row in the points tally document", "Balance forward");
            var tableRowCarryOver = _translator.Get("Document.PointsTally.Row.CarryOver", "Carry over row in the points tally document", "Carry over");

            var list = _pointsOverride ?? _database
                .Query<Points>(DC.Equal("ownerid", _person.Id.Value))
                .Where(p => p.Moment.Value.ToLocalTime().Date >= PointsTally.FromDate.Value &&
                            p.Moment.Value.ToLocalTime().Date <= PointsTally.UntilDate.Value)
                .ToList();
            var sum = list.Sum(p => p.Amount);
            PointsTally.Considered.Value = Math.Min(sum, _membership.Type.Value.MaximumPoints.Value);
            PointsTally.ForwardBalance.Value = Math.Min(sum - PointsTally.Considered.Value, _membership.Type.Value.MaximumBalanceForward);

            var text = new StringBuilder();

            text.Append(@"\tablefirsthead{");
            text.Append(@"\multicolumn{4}{c}{");
            text.Append(tableHead);
            text.Append(@"} \\ ");
            text.Append(tableColumnReason);
            text.Append(@" & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnDate);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnPoints);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnBalance);
            text.Append(@"} \\ ");
            text.Append(@"\hline}");
            text.AppendLine();

            text.Append(@"\tablehead{");
            text.Append(@"\multicolumn{4}{c}{");
            text.Append(tableHead);
            text.Append(@"} \\ ");
            text.Append(tableColumnReason);
            text.Append(@" & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnDate);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnPoints);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnBalance);
            text.Append(@"} \\ ");
            text.Append(@"\hline}");
            text.AppendLine();

            text.Append(@"\tablelasttail{");
            text.Append(@"\hline\hline ");
            text.Append(tableRowConsidered);
            text.Append(@" & & & \multicolumn{1}{r}{");
            text.Append(PointsTally.Considered.Value.FormatThousands());
            text.Append(@"} \\ ");
            text.Append(tableRowCarryOver);
            text.Append(@" & & & \multicolumn{1}{r}{");
            text.Append(PointsTally.ForwardBalance.Value.FormatThousands());
            text.Append(@"} \\ }");
            text.AppendLine();

            text.Append(@"\begin{supertabular}{p{7.2cm} p{2cm} p{1.2cm} p{1.2cm}}");
            text.AppendLine();
            long runUp = 0;

            if (_lastTally != null)
            {
                runUp += _lastTally.ForwardBalance.Value;

                text.Append(tableRowBalanceForward);
                text.Append(@" & \multicolumn{1}{r}{");
                text.Append(PointsTally.FromDate.Value);
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(_lastTally.ForwardBalance.Value.FormatThousands());
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(runUp.FormatThousands());
                text.Append(@"} \\");
                text.AppendLine();
            }

            foreach (var p in list.OrderBy(p => p.Moment.Value))
            {
                runUp += p.Amount.Value;

                text.Append(p.Reason.Value);
                text.Append(@" & \multicolumn{1}{r}{");
                text.Append(p.Moment.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm"));
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(p.Amount.Value.FormatThousands());
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(runUp.FormatThousands());
                text.Append(@"} \\");
                text.AppendLine();
            }

            text.Append(@"\end{supertabular}");
            text.AppendLine();

            return text.ToString();
        }

        public string Prefix
        {
            get { return "PointsTally"; }
        }

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "PointsTally.TableContent":
                    return CreateTableContent();
                case "PointsTally.FromDate":
                    return PointsTally.FromDate.Value.ToString("dd.MM.yyyy");
                case "PointsTally.UntilDate":
                    return PointsTally.UntilDate.Value.ToString("dd.MM.yyyy");
                case "PointsTally.CreatedDate":
                    return PointsTally.CreatedDate.Value.ToString("dd.MM.yyyy");
                default:
                    throw new NotSupportedException(); 
            }
        }

        protected override Templator GetTemplator()
        {
            return new Templator(new PersonContentProvider(_translator, _person), this);
        }
    }
}

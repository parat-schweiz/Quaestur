using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;
using BaseLibrary;
using System.Text;

namespace Hospes
{
    public class SettlementContentProvider : IContentProvider
    {
        private readonly IDatabase _database;
        private readonly Translator _translator;
        private readonly Person _person;
        private readonly IEnumerable<Bill> _bills;
        private readonly SystemWideSettings _settings;

        public SettlementContentProvider(IDatabase database, Translator translator, Person person, IEnumerable<Bill> bills)
        {
            _database = database;
            _translator = translator;
            _person = person;
            _bills = bills;
            _settings = _database.Query<SystemWideSettings>().Single();
        }

        private string CreateSettlementTable()
        {
            var tableHead = _translator.Get("" +
                "Settlement.Document.Table.Head",
                "Head of the arrears table",
                "Settlement");
            var tableColumnNumber = _translator.Get("" +
                "Settlement.Document.Table.Column.Number",
                "Number column in the arrears table",
                "Number");
            var tableColumnFrom = _translator.Get("" +
                "Settlement.Document.Table.Column.FromDate",
                "From date column in the arrears table",
                "From");
            var tableColumnUntil = _translator.Get("" +
                "Settlement.Document.Table.Column.UntilDate",
                "Until date column in the arrears table",
                "Until");
            var tableColumnAmount = _translator.Get("" +
                "Settlement.Document.Table.Column.Amount",
                "Amount date column in the arrears table",
                "Amount");
            var tableColumnRunUp = _translator.Get("" +
                "Settlement.Document.Table.Column.Balance",
                "Balance date column in the arrears table",
                "Balance");
            var tableRowPrepayment = _translator.Get("" +
                "Settlement.Document.Table.Row.Prepayment",
                "Prepayment row in the arrears table",
                "Prepayment");
            var tableRowOutstanding = _translator.Get("" +
                "Settlement.Document.Table.Row.Outstanding",
                "Outstanding row in the arrears table",
                "Outstanding amount");
            var currentPrepayment = _person.CurrentPrepayment(_database);
            var outstandingAmount = _bills.Sum(b => b.Amount.Value) - currentPrepayment;

            var text = new StringBuilder();

            text.Append(@"\tablefirsthead{");
            text.Append(@"\multicolumn{7}{c}{");
            text.Append(tableHead);
            text.Append(@"} \\ ");
            text.Append(tableColumnNumber);
            text.Append(@" & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnFrom);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnUntil);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{2}{r}{");
            text.Append(tableColumnAmount);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{2}{r}{");
            text.Append(tableColumnRunUp);
            text.Append(@"} \\ ");
            text.Append(@"\hline}");
            text.AppendLine();

            text.Append(@"\tablehead{");
            text.Append(@"\multicolumn{7}{c}{");
            text.Append(tableHead);
            text.Append(@"} \\ ");
            text.Append(tableColumnNumber);
            text.Append(@" & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnFrom);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{1}{r}{");
            text.Append(tableColumnUntil);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{2}{r}{");
            text.Append(tableColumnAmount);
            text.Append(@"} & ");
            text.Append(@"\multicolumn{2}{r}{");
            text.Append(tableColumnRunUp);
            text.Append(@"} \\ ");
            text.Append(@"\hline}");
            text.AppendLine();

            text.Append(@"\tablelasttail{");
            if (currentPrepayment >= 0.01M)
            {
                text.Append(@"\multicolumn{3}{l}{");
                text.Append(tableRowPrepayment);
                text.Append(@"} & \multicolumn{1}{l}{");
                text.Append(_settings.Currency.Value);
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append((-currentPrepayment).FormatMoney());
                text.Append(@"} & \multicolumn{1}{l}{");
                text.Append(_settings.Currency.Value);
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(outstandingAmount.FormatMoney());
                text.Append(@"} \\");
            }
            text.Append(@"\hline\hline ");
            text.Append(@"\multicolumn{5}{l}{");
            text.Append(tableRowOutstanding);
            text.Append(@"} & \multicolumn{1}{l}{");
            text.Append(_settings.Currency.Value);
            text.Append(@"} & \multicolumn{1}{r}{");
            text.Append(outstandingAmount.FormatMoney());
            text.Append(@"} \\");
            text.Append(@"}");
            text.AppendLine();

            text.Append(@"\begin{supertabular}{p{3.2cm} p{1cm} p{1cm} p{1.2cm} p{0.9cm} p{1.2cm} p{0.9cm} p{1.2cm}}");
            text.AppendLine();
            decimal runUp = 0M;

            foreach (var bill in _bills)
            {
                runUp += bill.Amount.Value;

                text.Append(bill.Number.Value);
                text.Append(@" & \multicolumn{1}{r}{");
                text.Append(bill.FromDate.Value.FormatSwissDateDay());
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(bill.UntilDate.Value.FormatSwissDateDay());
                text.Append(@"} & \multicolumn{1}{l}{");
                text.Append(_settings.Currency.Value);
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(bill.Amount.Value.FormatMoney());
                text.Append(@"} & \multicolumn{1}{l}{");
                text.Append(_settings.Currency.Value);
                text.Append(@"} & \multicolumn{1}{r}{");
                text.Append(runUp.FormatMoney());
                text.Append(@"} \\");
                text.AppendLine();
            }

            text.Append(@"\end{supertabular}");
            text.AppendLine();

            return text.ToString();
        }

        public string Prefix
        {
            get { return "Settlement"; }
        }

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "Settlement.Table":
                    return CreateSettlementTable();
                case "Settlement.MaxReminderLevel":
                    return _bills.Max(b => b.ReminderLevel.Value).ToString();
                case "Settlement.MinReminderLevel":
                    return _bills.Min(b => b.ReminderLevel.Value).ToString();
                case "Settlement.BillCount":
                    return _bills.Count().ToString();
                case "Settlement.PaymentRequired":
                    var currentPrepayment = _person.CurrentPrepayment(_database);
                    var outstandingAmount = _bills.Sum(b => b.Amount.Value) - currentPrepayment;
                    return outstandingAmount > 0m ? "yes" : "no";
                default:
                    throw new InvalidOperationException(
                        "Variable " + variable + " not known in provider " + Prefix);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class DashboardItemViewModel
    {
        public string Tag;
        public string Indent;
        public string Width;
        public string Name;
        public string ValueOne;
        public string ValueTwo;
        public string ValueThree;

        public DashboardItemViewModel(string valueOne, string valueTwo, string valueThree)
        {
            Tag = "th";
            Name = string.Empty;
            Indent = "0%";
            Width = "40%";
            ValueOne = valueOne;
            ValueTwo = valueTwo;
            ValueThree = valueThree;
        }

        public DashboardItemViewModel(Translator translator, IDatabase db, Organization organization, int indent)
        {
            Tag = "td";
            Indent = indent.ToString() + "%";
            Width = (40 - indent).ToString() + "%";
            Name = organization.Name.Value[translator.Language];
            var members = db
                .Query<Membership>(DC.Equal("organizationid", organization.Id.Value))
                .Where(m => !m.Person.Value.Deleted)
                .ToList();
            ValueOne = members.Count().ToString();
            ValueTwo = members.Count(m => m.Type.Value.Rights.Value.HasFlag(MembershipRight.Voting)).ToString();

            foreach (var m in members)
            {
                if (!m.HasVotingRight.Value.HasValue)
                {
                    m.UpdateVotingRight(db);
                    db.Save(m); 
                }
            }

            ValueThree = members.Count(m => m.HasVotingRight.Value.Value).ToString();
        }
    }

    public class DashboardBalanceViewModel
    {
        public string Name;
        public string Value;
        public string Explain;

        public DashboardBalanceViewModel(string name, string value, string explain)
        {
            Name = name;
            Value = value;
            Explain = explain;
        }
    }

    public class DashboardViewModel : MasterViewModel
    {
        public List<DashboardBalanceViewModel> Balance;
        public List<DashboardItemViewModel> List;

        private void AddRecursive(Translator translator, IDatabase db, Organization organization, int indent)
        {
            List.Add(new DashboardItemViewModel(translator, db, organization, indent));

            foreach (var o in organization.Children)
            {
                AddRecursive(translator, db, o, indent + 5); 
            }
        }

        private Bill LastBill(Membership membership, IDatabase database)
        {
            switch (membership.Type.Value.Collection.Value)
            {
                case CollectionModel.Direct:
                    return database
                        .Query<Bill>(DC.Equal("membershipid", membership.Id.Value))
                        .OrderByDescending(b => b.UntilDate.Value)
                        .FirstOrDefault();
                case CollectionModel.ByParent:
                    var parent = membership.Person.Value.ActiveMemberships
                        .Where(m => m.Type.Value.Collection.Value != CollectionModel.None)
                        .FirstOrDefault(m => membership.Type.Value.Organization.Value.Parent.Value.MembershipTypes.Contains(m.Type.Value));
                    if (parent == null)
                    {
                        return null;
                    }
                    else
                    {
                        return LastBill(parent, database);
                    }
                case CollectionModel.BySub:
                    var sub = membership.Person.Value.ActiveMemberships
                        .Where(m => m.Type.Value.Collection.Value != CollectionModel.None)
                        .FirstOrDefault(m => membership.Type.Value.Organization.Value.Children.SelectMany(c => c.MembershipTypes).Contains(m.Type.Value));
                    if (sub == null)
                    {
                        return null;
                    }
                    else
                    {
                        return LastBill(sub, database);
                    }
                default:
                    return null;
            }
        }

        private Tuple<decimal, decimal> ComputeFeeAndDiscounted(Membership membership, IDatabase database, decimal portionOfDiscount)
        {
            var lastBill = LastBill(membership, database);
            var paymentModel = membership.Type.Value.CreatePaymentModel(database);
            var fromDate = lastBill != null ? lastBill.UntilDate.Value.AddDays(1) : membership.StartDate.Value;
            var untilDate = fromDate.AddDays(paymentModel.GetBillingPeriod() - 1);
            var amount = paymentModel.ComputeAmount(membership, fromDate, untilDate);
            var maxDiscount = membership.Type.Value.MaximumDiscount.Value / 100m;
            var discounted = amount * (1m - maxDiscount * portionOfDiscount);
            return new Tuple<decimal, decimal>(amount, discounted);
        }

        private decimal PointsDiscountPercent(Person person, IDatabase database, long pointsBalance)
        {
            var points = person.PointsBalance(database);
            var memberships = person.ActiveMemberships
                .Where(m => m.Type.Value.Payment.Value != PaymentModel.None)
                .ToList();
            var maxPoints = memberships
                .Sum(m => m.Type.Value.MaximumPoints.Value);
            var portionOfDiscount = (maxPoints > 0) ?
                Math.Min(1m, (decimal)pointsBalance / (decimal)maxPoints) : 0m;
            var fees = memberships
                .Select(m => ComputeFeeAndDiscounted(m, database, portionOfDiscount))
                .ToList();
            var undiscountedFees = fees.Sum(f => f.Item1);
            var discountedFees = fees.Sum(f => f.Item2);
            return (undiscountedFees > 0m) ? (100m - (100m / undiscountedFees * discountedFees)) : 0m;
        }

        public DashboardViewModel(Translator translator, IDatabase db, Session session)
            : base(db, translator,
                   translator.Get("Dashboard.Title", "Dashboard page title", "Dashboard"),
                   session)
        {
            var settings = db.Query<SystemWideSettings>().Single();
            var points = session.User.PointsBalance(db);
            var pointsDiscountPercent = PointsDiscountPercent(session.User, db, points);
            var money = session.User.MoneyBalance(db);
            var credits = session.User.CreditsBalance(db);
            var creditsWorth = (decimal)credits / (decimal)settings.CreditsPerCurrency.Value;

            Balance = new List<DashboardBalanceViewModel>();
            Balance.Add(new DashboardBalanceViewModel(
                translator.Get("Dashboard.Balance.Credits.Name", "Credits balance name on the dashboard", "Credits"),
                credits.ToString(),
                translator.Get("Dashboard.Balance.Credits.Explain", "Credits balance explain on the dashboard", "Worth {0} {1} in our shop", settings.Currency.Value, creditsWorth)));
            Balance.Add(new DashboardBalanceViewModel(
                translator.Get("Dashboard.Balance.Points.Name", "Points balance name on the dashboard", "Points"),
                points.ToString(),
                translator.Get("Dashboard.Balance.Points.Explain", "Points balance explain on the dashboard", "{0:0.00}% discount towards you next membership fee", pointsDiscountPercent)));
            Balance.Add(new DashboardBalanceViewModel(
                translator.Get("Dashboard.Balance.Money.Name", "Money balance name on the dashboard", "Fees balance"),
                string.Format("{0} {1}", settings.Currency.Value, money.FormatMoney()), ""));

            List = new List<DashboardItemViewModel>();
            List.Add(new DashboardItemViewModel(
                translator.Get("Dashboard.Members.Row.All", "All members row in the dashbaord", "All persons"),
                translator.Get("Dashboard.Members.Row.Full", "Full members row in the dashbaord", "Full members"),
                translator.Get("Dashboard.Members.Row.Voting", "Voting members row in the dashbaord", "Voting rights")));

            foreach (var o in db
                .Query<Organization>()
                .Where(o => o.Parent.Value == null)
                .OrderBy(o => o.Name.Value[translator.Language]))
            {
                AddRecursive(translator, db, o, 0);
            }
        }
    }

    public class DashboardModule : QuaesturModule
    {
        public DashboardModule()
        {
            RequireCompleteLogin();

            Get("/", parameters =>
            {
                return View["View/dashboard.sshtml",
                    new DashboardViewModel(Translator, Database, CurrentSession)];
            });
        }
    }
}

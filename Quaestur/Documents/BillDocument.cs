using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class BillDocument : TemplateDocument, IContentProvider
    {
        private readonly Translator _translator;
        private readonly IDatabase _database;
        private readonly Membership _membership;
        private readonly Person _person;
        private readonly Organization _organization;
        private readonly SystemWideSettings _settings;
        private IPaymentModel _mainModel;
        private List<Tuple<Membership, IPaymentModel>> _allIncluded;
        private PointsTally _lastTally;
        private long _maxPoints;
        private long _consideredPoints;
        private decimal _portionOfDiscount;

        public Bill Bill { get; private set; }
        public bool RequiresPersonalPaymentUpdate { get; private set; }
        public bool RequiresNewPointsTally { get; private set; }

        public BillDocument(Translator translator, IDatabase database, Membership membership)
        {
            _translator = translator;
            _database = database;
            _membership = membership;
            _person = _membership.Person.Value;
            _organization = _membership.Organization.Value;
            _settings = _database.Query<SystemWideSettings>().Single();
        }

        private string CreateNumber()
        {
            return Bill.CreatedDate.Value.ToString("yyyyMMdd") + "M" + _person.Number.Value.PadInt(7);
        }

        public bool Create()
        {
            if (Prepare())
            {
                var document = Compile();

                if (document != null)
                {
                    Bill.DocumentData.Value = document;
                    return true;
                }
            }

            return false;
        }

        public override bool Prepare()
        {
            _mainModel = _membership.Type.Value.CreatePaymentModel(_database);

            Bill = new Bill(Guid.NewGuid());
            Bill.Membership.Value = _membership;
            Bill.CreatedDate.Value = DateTime.Now.Date;
            Bill.Status.Value = BillStatus.New;
            Bill.Number.Value = CreateNumber();

            var lastBill = _database
                .Query<Bill>(DC.Equal("membershipid", _membership.Id.Value))
                .OrderByDescending(b => b.UntilDate.Value)
                .FirstOrDefault();

            if (lastBill == null)
            {
                Bill.FromDate.Value = _membership.StartDate.Value;
            }
            else
            {
                Bill.FromDate.Value = lastBill.UntilDate.Value.Date.AddDays(1d).Date;
            }

            if (_membership.EndDate.Value.HasValue)
            {
                if (_membership.EndDate.Value.Value.Date > Bill.FromDate.Value)
                {
                    Bill.UntilDate.Value = _membership.EndDate.Value.Value.Date;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var period = _mainModel.GetBillingPeriod();
                Bill.UntilDate.Value = Bill.FromDate.Value.AddDays(period).Date;
            }

            _allIncluded = new List<Tuple<Membership, IPaymentModel>>();
            _allIncluded.Add(new Tuple<Membership, IPaymentModel>(_membership, _mainModel));
            IncludeParent(_membership);
            IncludeChildren(_membership);

            _lastTally = _database
                .Query<PointsTally>(DC.Equal("personid", _person.Id.Value))
                .Where(t => t.UntilDate.Value < Bill.FromDate.Value)
                .OrderByDescending(t => t.UntilDate.Value)
                .FirstOrDefault();

            if (lastBill != null &&
                _lastTally != null &&
                lastBill.FromDate.Value > _lastTally.UntilDate.Value)
            {
                var shouldHaveTally = _person.Memberships
                    .Any(m => m.Type.Value.Collection.Value == CollectionModel.Direct &&
                         m.Type.Value.Payment.Value != PaymentModel.None &&
                         m.Type.Value.MaximumPoints.Value > 0);

                // The last tally was already considered in last bill
                if (shouldHaveTally)
                {
                    // there should be a newer tally which is missing
                    RequiresNewPointsTally = true;
                    return false;
                }
                else
                {
                    // no need for a tally
                    RequiresNewPointsTally = false;
                }
            }
            else
            {
                RequiresNewPointsTally = false;
            }

            _maxPoints = _person.Memberships
                .Where(m => m.Type.Value.Payment.Value != PaymentModel.None)
                .Sum(m => MaxPoints(m));
            _consideredPoints = _lastTally != null ? _lastTally.Considered.Value : 0;
            _portionOfDiscount = (_maxPoints > 0) ? 
                Math.Min(1m, (decimal)_consideredPoints / (decimal)_maxPoints) : 0m;

            RequiresPersonalPaymentUpdate = _allIncluded
                .Any(m => m.Item2.RequireParameterUpdate(m.Item1));
            return !RequiresPersonalPaymentUpdate;
        }

        private void IncludeChildren(Membership membership)
        {
            foreach (var childMembership in _person.Memberships
                .Where(DateOverlap)
                .Where(m => membership.Organization.Value.Children.Contains(m.Organization.Value)))
            {
                switch (childMembership.Type.Value.Collection.Value)
                {
                    case CollectionModel.Direct:
                    case CollectionModel.BySub:
                        // Do nothing further
                        break;
                    case CollectionModel.None:
                        IncludeChildren(childMembership);
                        break;
                    case CollectionModel.ByParent:
                        var model = childMembership.Type.Value.CreatePaymentModel(_database);
                        if (model != null)
                        {
                            _allIncluded.Add(new Tuple<Membership, IPaymentModel>(childMembership, model));
                        }
                        IncludeChildren(childMembership);
                        break;
                }
            }
        }

        private void IncludeParent(Membership membership)
        {
            foreach (var parentMembership in _person.Memberships
                .Where(DateOverlap)
                .Where(m => m.Organization.Value == membership.Organization.Value.Parent.Value))
            {
                switch (parentMembership.Type.Value.Collection.Value)
                {
                    case CollectionModel.Direct:
                    case CollectionModel.ByParent:
                        // Do nothing further
                        break;
                    case CollectionModel.None:
                        IncludeParent(parentMembership);
                        break;
                    case CollectionModel.BySub:
                        var model = parentMembership.Type.Value.CreatePaymentModel(_database);
                        if (model != null)
                        {
                            _allIncluded.Add(new Tuple<Membership, IPaymentModel>(parentMembership, model));
                        }
                        IncludeParent(parentMembership);
                        break;
                }
            }
        }

        private bool DateOverlap(Membership membership)
        {
            var start = membership.StartDate.Value.Date;
            var end = (membership.EndDate.Value ?? DateTime.MaxValue).Date;

            return !(end < Bill.FromDate.Value.Date || start > Bill.UntilDate.Value);
        }

        protected override string TexTemplate
        {
            get { return _membership.Type.Value.GetBillDocument(_database, _translator.Language).Text.Value; } 
        }

        private long MaxPoints(Membership membership)
        {
            var endDate = membership.EndDate.Value ?? DateTime.Now.AddYears(10);
            double billDays = Bill.UntilDate.Value.Date.Subtract(Bill.FromDate.Value.Date).TotalDays;
            double overlapDays = Dates.ComputeOverlap(Bill.FromDate.Value.Date, Bill.UntilDate.Value.Date, membership.StartDate.Value, endDate).TotalDays;
            return (long)Math.Floor(membership.Type.Value.MaximumPoints.Value / billDays * overlapDays);
        }

        private string CreateExplainations()
        {
            if (_lastTally != null)
            {
                var text = new StringBuilder();

                text.Append(@"\textbf{");
                text.Append(_translator.Get(
                    "Document.Bill.PointsDiscount",
                    "Discournt engagement points heading in bill creation",
                    "Discount engagement points"));
                text.Append(@"} & ~ \\");
                text.AppendLine();

                text.Append(_translator.Get(
                    "Document.Bill.MaximumPoints",
                    "Maximum points over all memberships in bill creation",
                    "Maximum points over all memberships"));
                text.Append(@" & ");
                text.Append(_maxPoints.FormatThousands());
                text.Append(@" \\");
                text.AppendLine();

                text.Append(_translator.Get(
                    "Document.Bill.ConsideredPoints",
                    "Points considered in bill creation",
                    "Points considered in this bill"));
                text.Append(@" & ");
                text.Append(_consideredPoints.FormatThousands());
                text.Append(@" \\");
                text.AppendLine();

                text.Append(_translator.Get(
                    "Document.Bill.PortionOfDiscount",
                    "Portion of total available discount in bill creation",
                    "Portion of total available discount"));
                text.Append(@" & ");
                text.Append(string.Format("{0:0.0}", _portionOfDiscount * 100m));
                text.Append(@"\% \\");
                text.AppendLine();

                return text.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        private string CreateFinalTableContent()
        {
            var text = new StringBuilder();
            var totalAmount = 0m;

            foreach (var included in _allIncluded)
            {
                var paymentModel = included.Item2;
                var periodAmount = paymentModel.ComputeAmount(included.Item1, Bill.FromDate.Value, Bill.UntilDate.Value);

                text.Append(@"\textbf{");
                text.Append(included.Item1.Organization.Value);
                text.Append(@"} & ~ & ~ \\");
                text.AppendLine();

                text.AppendLine(included.Item2.CreateExplainationLatex(_translator, _membership));

                var maxDiscount = included.Item1.Type.Value.MaximumDiscount.Value / 100m;
                var actualDiscount = maxDiscount * _portionOfDiscount;
                var discountAmount = Math.Round(actualDiscount * periodAmount, 2);

                text.Append(@"~~~~~");
                text.Append(_translator.Get(
                    "Document.Bill.PeriodFee",
                    "Period fee in the bill document",
                    "Fee from {0} until {1}",
                    Bill.FromDate.Value.FormatSwissDateDay(),
                    Bill.UntilDate.Value.FormatSwissDateDay()));
                text.Append(@" & ");
                text.Append(_settings.Currency);
                text.Append(@" & ");
                text.Append(Currency.Format(periodAmount));
                text.Append(@" \\");
                text.AppendLine();

                if (_portionOfDiscount > 0m)
                {
                    text.Append(@"~~~~~");
                    text.Append(_translator.Get(
                        "Document.Bill.FeeDiscount",
                        "Period fee in the bill document",
                        "{0:0.0}% of max {1:0.0}% discount",
                        actualDiscount * 100m,
                        maxDiscount * 100m)
                        .EscapeLatex());
                    text.Append(@" & ");
                    text.Append(_settings.Currency);
                    text.Append(@" & ");
                    text.Append(discountAmount);
                    text.Append(@" \\");
                    text.AppendLine();
                }

                text.Append(@"~ & ~ & ~ \\");
                text.AppendLine();

                totalAmount += (periodAmount - discountAmount);
            }

            text.Append(@"\textbf{");
            text.Append(_translator.Get(
                "Document.Bill.Total", 
                "Total amount of a bill", 
                "Total"));
            text.Append(@"} & ");
            text.Append(_settings.Currency);
            text.Append(@" & ");
            text.Append(Currency.Format(totalAmount));
            text.Append(@" \\");
            text.AppendLine();

            Bill.Amount.Value = totalAmount;

            if (Bill.Amount.Value == 0m)
            {
                Bill.Status.Value = BillStatus.Payed; 
            }

            return text.ToString();
        }

        public string Prefix
        {
            get { return "Bill"; }
        }

        public string GetContent(string variable)
        {
            switch (variable)
            {
                case "Bill.Explainations":
                    return CreateExplainations();
                case "Bill.FinalTableContent":
                    return CreateFinalTableContent();
                case "Bill.Organization":
                    return _organization.Name.Value[_translator.Language];
                case "Bill.FromDate":
                    return Bill.FromDate.Value.FormatSwissDateDay();
                case "Bill.UntilDate":
                    return Bill.UntilDate.Value.FormatSwissDateDay();
                case "Bill.CreatedDate":
                    return Bill.CreatedDate.Value.FormatSwissDateDay();
                case "Bill.Number":
                    return Bill.Number.Value;
                case "Bill.Amount":
                    return string.Format("{0:0.00}", Math.Round(Bill.Amount.Value, 2));
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

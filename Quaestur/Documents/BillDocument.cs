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

        public Bill Bill { get; private set; }

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

            return true;
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

        private string CreateFinalTableContent()
        {
            var text = new StringBuilder();
            var totalAmount = 0m;

            foreach (var included in _allIncluded)
            {
                var paymentModel = included.Item2;
                var periodAmount = paymentModel.ComputeAmount(_membership, Bill.FromDate.Value, Bill.UntilDate.Value);
                totalAmount += periodAmount;

                text.Append(@"\textbf{");
                text.Append(included.Item1.Organization.Value);
                text.Append(@"} & ~ & ~ \\");
                text.AppendLine();

                text.AppendLine(included.Item2.CreateExplainationLatex(_translator, _membership));

                text.Append(@"~~~~~");
                text.Append(_translator.Get(
                    "Document.Bill.PeriodFee",
                    "Yearly fee in the bill document",
                    "Fee from {0} until {1}",
                    Bill.FromDate.Value.ToString("dd.MM.yyyy"),
                    Bill.UntilDate.Value.ToString("dd.MM.yyyy")));
                text.Append(@" & ");
                text.Append(_settings.Currency);
                text.Append(@" & ");
                text.Append(Currency.Format(periodAmount));
                text.Append(@" \\");
                text.AppendLine();

                text.Append(@"~ & ~ & ~ \\");
                text.AppendLine();
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
                    return string.Empty;
                case "Bill.FinalTableContent":
                    return CreateFinalTableContent();
                case "Bill.Organization":
                    return _organization.Name.Value[_translator.Language];
                case "Bill.FromDate":
                    return Bill.FromDate.Value.ToString("dd.MM.yyyy");
                case "Bill.UntilDate":
                    return Bill.UntilDate.Value.ToString("dd.MM.yyyy");
                case "Bill.CreatedDate":
                    return Bill.CreatedDate.Value.ToString("dd.MM.yyyy");
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

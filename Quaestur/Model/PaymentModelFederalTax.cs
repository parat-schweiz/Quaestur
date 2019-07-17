using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PaymentModelFederalTax : IPaymentModel
    {
        private const string PercentageKey = "PaymentModel.FederalTax.Parameter.Percentage";
        private const string MinAmountKey = "PaymentModel.FederalTax.Parameter.MinAmount";
        private const string BillingPeriodKey = "PaymentModel.FederalTax.Parameter.BillingPeriod";
        private const string ReminderPeriodKey = "PaymentModel.FederalTax.Parameter.ReminderPeriod";
        private const string VotingRightGraceAfterBillKey = "PaymentModel.FederalTax.Parameter.VotingRightGraceAfterBill";
        public const string FullTaxKey = "PaymentModel.FederalTax.Parameter.FullTax";

        private MembershipType _membershipType;
        private IDatabase _database;

        public PaymentModelFederalTax(MembershipType membershipType, IDatabase database)
        {
            _membershipType = membershipType;
            _database = database;
        }

        public PaymentModelFederalTax()
        {
        }

        public IEnumerable<PaymentParameterType> ParameterTypes
        {
            get
            {
                yield return new PaymentParameterType(PercentageKey, 10m,
                    t => t.Get(PercentageKey, "Percentage parameter of the federal tax payment model", "Percentage"));
                yield return new PaymentParameterType(MinAmountKey, 10m,
                    t => t.Get(MinAmountKey, "Minimal amount parameter of the federal tax payment model", "Minimal amount"));
                yield return new PaymentParameterType(BillingPeriodKey, 180m,
                    t => t.Get(BillingPeriodKey, "Period parameter of the federal tax payment model", "Billing Period (days)"));
                yield return new PaymentParameterType(ReminderPeriodKey, 10m,
                    t => t.Get(ReminderPeriodKey, "Reminder period parameter of the federal tax payment model", "Reminder Period (days)"));
                yield return new PaymentParameterType(VotingRightGraceAfterBillKey, 30m,
                    t => t.Get(VotingRightGraceAfterBillKey, "Voting right grace period after bill sent parameter of the federal tax payment model", "Voting right grace period after bill sent (days)"));
            }
        }

        public IEnumerable<PaymentParameterType> PersonalParameterTyoes
        {
            get
            {
                yield return new PaymentParameterType(FullTaxKey, 10m,
                    t => t.Get(FullTaxKey, "Taxed income personal parameter of the federal tax payment model", "Taxed income"));
            }
        }

        public static void SetFullTax(IDatabase db, Person person, decimal value)
        {
            var incomeParameter = person.PaymentParameters
                .SingleOrDefault(p => p.Key == FullTaxKey);

            if (incomeParameter != null)
            {
                incomeParameter.Value.Value = value;
                incomeParameter.LastUpdate.Value = DateTime.UtcNow;
                db.Save(incomeParameter);
            }
            else
            {
                incomeParameter = new PersonalPaymentParameter(Guid.NewGuid());
                incomeParameter.Person.Value = person;
                incomeParameter.Key.Value = FullTaxKey;
                incomeParameter.Value.Value = value;
                incomeParameter.LastUpdate.Value = DateTime.UtcNow;
                db.Save(incomeParameter);
            }
        }

        public static decimal? GetFullTax(IEnumerable<PersonalPaymentParameter> parameters)
        {
            var incomeParameter = parameters
                .SingleOrDefault(p => p.Key == FullTaxKey);

            if (incomeParameter != null)
            {
                return incomeParameter.Value.Value;
            }
            else
            {
                return null;
            }
        }

        public static decimal? GetFullTax(Person person)
        {
            return GetFullTax(person.PaymentParameters);
        }

        public static decimal ComputeFullTax(decimal taxableIncome)
        {
            foreach (var tarif in TarifClasses.OrderByDescending(t => t.StartAmount))
            {
                if (taxableIncome > tarif.StartAmount)
                {
                    return tarif.Value + Math.Floor((taxableIncome - tarif.StartAmount) / 100m) * tarif.ValueFor100More;
                }
            }

            return 0m;
        }

        public decimal ComputeYearlyAmount(IEnumerable<PersonalPaymentParameter> parameters)
        {
            var percentage = _membershipType.PaymentParameters
                .Single(p => p.Key == PercentageKey).Value.Value;
            var minAmount = _membershipType.PaymentParameters
                .Single(p => p.Key == MinAmountKey).Value.Value;
            var fullTax = GetFullTax(parameters) ?? 0m;

            return Math.Max(minAmount, fullTax / 100m * percentage);
        }

        public decimal ComputeYearlyAmount(Membership membership)
        {
            return ComputeYearlyAmount(membership.Person.Value.PaymentParameters);
        }

        private static IEnumerable<TarifClass> TarifClasses
        {
            get
            {
                yield return new TarifClass(14500m, 0m, 0.77m);
                yield return new TarifClass(31600m, 131.65m, 0.88m);
                yield return new TarifClass(41400m, 217.90m, 2.64m);
                yield return new TarifClass(55200m, 582.20m, 2.97m);
                yield return new TarifClass(72500m, 1096.00m, 5.94m);
                yield return new TarifClass(78100m, 1428.60m, 6.60m);
                yield return new TarifClass(103600m, 3111.60m, 8.80m);
                yield return new TarifClass(134600m, 5839.60m, 11.00m);
                yield return new TarifClass(176000m, 10393.60m, 13.20m);
                yield return new TarifClass(755200m, 86848.00m, 11.50m);
            }
        }

        private class TarifClass
        {
            public decimal StartAmount { get; private set; }
            public decimal Value { get; private set; }
            public decimal ValueFor100More { get; private set; }

            public TarifClass(decimal startAcount, decimal value, decimal valueFrom100More)
            {
                StartAmount = startAcount;
                Value = value;
                ValueFor100More = valueFrom100More;
            } 
        }

        public string CreateExplainationLatex(Translator translator, IEnumerable<PersonalPaymentParameter> parameters)
        {
            var percentage = _membershipType.PaymentParameters
                .Single(p => p.Key == PercentageKey).Value.Value;
            var minAmount = _membershipType.PaymentParameters
                .Single(p => p.Key == MinAmountKey).Value.Value;
            var currency = _database.Query<SystemWideSettings>()
                .Single().Currency.Value;
            var fullTax = GetFullTax(parameters) ?? 0m;
            var thereofAmount = fullTax / 100m * percentage;
            var yearlyFee = Math.Max(minAmount, thereofAmount);

            var stated = translator.Get(
                "PaymentModel.FederalTax.Explaination.LaTeX.Stated",
                "Explaination text part for the federal tax payment model",
                "Stated federal tax");
            var thereof = translator.Get(
                "PaymentModel.FederalTax.Explaination.LaTeX.Thereof",
                "Explaination text part for the federal tax payment model",
                "{0}\\% thereof",
                Math.Round(percentage, 0));
            var atLeast = translator.Get(
                "PaymentModel.FederalTax.Explaination.LaTeX.AtLeast",
                "Explaination text part for the federal tax payment model",
                "but at least");
            var membershipFee = translator.Get(
                "PaymentModel.FederalTax.Explaination.LaTeX.MembershipFee",
                "Explaination text part for the federal tax payment model",
                "Yearly membership fee");

            var text = new StringBuilder();

            text.Append(@"~~~~~");
            text.Append(stated);
            text.Append(" & ");
            text.Append(currency); 
            text.Append(" & ");
            text.Append(Currency.Format(fullTax));
            text.Append(@" \\");
            text.AppendLine();

            text.Append(@"~~~~~");
            text.Append(thereof);
            text.Append(" & ");
            text.Append(currency);
            text.Append(" & ");
            text.Append(Currency.Format(thereofAmount));
            text.Append(@" \\");
            text.AppendLine();

            text.Append(@"~~~~~");
            text.Append(atLeast);
            text.Append(" & ");
            text.Append(currency);
            text.Append(" & ");
            text.Append(Currency.Format(minAmount));
            text.Append(@" \\");
            text.AppendLine();

            text.Append(@"~~~~~");
            text.Append(membershipFee);
            text.Append(" & ");
            text.Append(currency);
            text.Append(" & ");
            text.Append(Currency.Format(yearlyFee));
            text.Append(@" \\");
            text.AppendLine();

            return text.ToString();
        }

        public string CreateExplainationLatex(Translator translator, Membership membership)
        {
            return CreateExplainationLatex(translator, membership.Person.Value.PaymentParameters);
        }

        public string CreateExplainationText(Translator translator, IEnumerable<PersonalPaymentParameter> parameters)
        {
            var percentage = _membershipType.PaymentParameters
                .Single(p => p.Key == PercentageKey).Value.Value;
            var minAmount = _membershipType.PaymentParameters
                .Single(p => p.Key == MinAmountKey).Value.Value;
            var currency = _database.Query<SystemWideSettings>()
                .Single().Currency.Value;
            return translator.Get(
                "PaymentModel.FederalTax.Explaination.LaTeX",
                "Explaination text for the federal tax payment model",
                "{0}% of full tax, but at least {1} {2}",
                Math.Round(percentage, 0),
                currency,
                Currency.Format(minAmount));
        }

        public string CreateExplainationText(Translator translator, Membership membership)
        {
            return CreateExplainationText(translator, membership.Person.Value.PaymentParameters);
        }

        public int GetBillingPeriod()
        {
            return (int)_membershipType.PaymentParameters
                .Single(p => p.Key == BillingPeriodKey).Value;
        }

        public int GetReminderPeriod()
        {
            return (int)_membershipType.PaymentParameters
                .Single(p => p.Key == ReminderPeriodKey).Value;
        }

        public bool HasVotingRight(IDatabase database, Membership membership)
        {
            var days = (int)_membershipType.PaymentParameters
                .Single(p => p.Key == VotingRightGraceAfterBillKey).Value;
            var lastBill = database
                .Query<Bill>(DC.Equal("membershipid", membership.Id.Value))
                .OrderByDescending(m => m.UntilDate.Value)
                .FirstOrDefault();

            if (lastBill == null)
            {
                return false;
            }
            else if (lastBill.Status.Value == BillStatus.Payed)
            {
                return true;
            }
            else
            {
                return DateTime.UtcNow.Date.Subtract(lastBill.CreatedDate.Value.Date).TotalDays <= days;
            }
        }

        public int GetBillAdvancePeriod()
        {
            return (int)_membershipType.PaymentParameters
                .Single(p => p.Key == VotingRightGraceAfterBillKey).Value;
        }
    }
}

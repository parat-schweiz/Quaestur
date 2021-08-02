using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PaymentModelFixed : PaymentModelProRataTemporis
    {
        private const string AmountKey = "PaymentModel.Fixed.Parameter.Amount";
        private const string BillingPeriodKey = "PaymentModel.Fixed.Parameter.BillingPeriod";
        private const string ReminderPeriodKey = "PaymentModel.Fixed.Parameter.ReminderPeriod";
        private const string VotingRightGraceAfterBillKey = "PaymentModel.Fixed.Parameter.VotingRightGraceAfterBill";

        private MembershipType _membershipType;
        private IDatabase _database;

        public PaymentModelFixed(MembershipType membershipType, IDatabase database)
        {
            _membershipType = membershipType;
            _database = database;
        }

        public PaymentModelFixed()
        {
        }

        public override IEnumerable<PaymentParameterType> ParameterTypes
        {
            get 
            {
                yield return new PaymentParameterType(AmountKey, 10m,
                    t => t.Get(AmountKey, "Amount parameter of the fixed payment model", "Amount"));
                yield return new PaymentParameterType(BillingPeriodKey, 180m,
                    t => t.Get(BillingPeriodKey, "Period parameter of the fixed payment model", "Billing Period (days)"));
                yield return new PaymentParameterType(ReminderPeriodKey, 10m,
                    t => t.Get(ReminderPeriodKey, "Reminder period parameter of the fixed payment model", "Reminder Period (days)"));
                yield return new PaymentParameterType(VotingRightGraceAfterBillKey, 30m,
                    t => t.Get(VotingRightGraceAfterBillKey, "Voting right grace period after bill sent parameter of the fixed payment model", "Voting right grace period after bill sent (days)"));
            }
        }

        public override IEnumerable<PaymentParameterType> PersonalParameterTypes
        {
            get { return new PaymentParameterType[0]; } 
        }

        public override string CreateExplainationLatex(Translator translator, Membership membership)
        {
            var fixedAmount = _membershipType.PaymentParameters
                .Single(p => p.Key == AmountKey).Value;
            var currency = _database.Query<SystemWideSettings>()
                .Single().Currency.Value;
            var fixedInfo = translator.Get(
                "PaymentModel.Fixed.Explaination.LaTeX",
                "Explaination text for the fixed payment model",
                "Yearly fixed membership fee");

            var text = new StringBuilder();

            text.Append(@"~~~~~");
            text.Append(fixedInfo);
            text.Append(" & ");
            text.Append(currency);
            text.Append(" & ");
            text.Append(Currency.Format(fixedAmount));
            text.Append(@" \\");
            text.AppendLine();

            return text.ToString();
        }

        public override string CreateExplainationText(Translator translator, Membership membership)
        {
            return translator.Get(
                "PaymentModel.Fixed.Explaination.Text", 
                "Explaination text for the fixed payment model", 
                "Fixed membership fee amount");
        }

        public override int GetBillingPeriod()
        {
            return (int)_membershipType.PaymentParameters
                .Single(p => p.Key == BillingPeriodKey).Value;
        }

        public override int GetReminderPeriod()
        {
            return (int)_membershipType.PaymentParameters
                .Single(p => p.Key == ReminderPeriodKey).Value;
        }

        public override bool HasVotingRight(Membership membership)
        {
            var days = (int)_membershipType.PaymentParameters
                .Single(p => p.Key == VotingRightGraceAfterBillKey).Value;
            var bills = _database
                .Query<Bill>(DC.Equal("membershipid", membership.Id.Value));

            if (!bills.Any())
            {
                return false;
            }

            bool allBillsPayed = true;

            foreach (var bill in bills)
            {
                switch (bill.Status.Value)
                {
                    case BillStatus.Canceled:
                    case BillStatus.Payed:
                        break;
                    case BillStatus.New:
                        allBillsPayed &= DateTime.UtcNow.Date.Subtract(bill.CreatedDate.Value.Date).TotalDays <= days;
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            return allBillsPayed;
        }

        public override int GetBillAdvancePeriod()
        {
            return (int)_membershipType.PaymentParameters
                .Single(p => p.Key == VotingRightGraceAfterBillKey).Value;
        }

        protected override decimal ComputeYearlyAmount(Membership membership)
        {
            return _membershipType.PaymentParameters
                .Single(p => p.Key == AmountKey).Value;
        }

        public override bool RequireParameterUpdate(Membership membership)
        {
            return false;
        }

        public override bool InviteForParameterUpdate(Membership membership)
        {
            return false;
        }
    }
}

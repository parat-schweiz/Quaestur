using System;
using System.Linq;
using System.Collections.Generic;

namespace Quaestur
{
    public class PaymentModelFixed : IPaymentModel
    {
        private string AmountKey = "PaymentModel.Fixed.Parameter.Amount";
        private string BillingPeriodKey = "PaymentModel.Fixed.Parameter.BillingPeriod";
        private string ReminderPeriodKey = "PaymentModel.Fixed.Parameter.ReminderPeriod";
        private string VotingRightGraceAfterBillKey = "PaymentModel.Fixed.Parameter.VotingRightGraceAfterBill";

        private MembershipType _membershipType;

        public PaymentModelFixed(MembershipType membershipType)
        {
            _membershipType = membershipType;
        }

        public PaymentModelFixed()
        {
        }

        public IEnumerable<PaymentParameterType> ParameterTypes
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

        public decimal ComputeYearlyAmount(Membership membership)
        {
            return _membershipType.PaymentParameters
                .Single(p => p.Key == AmountKey).Value;
        }

        public string CreateExplainationLatex(Translator translator, Membership membership)
        {
            return string.Empty;
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
                .Where(b => b.Status.Value == BillStatus.Payed)
                .OrderByDescending(m => m.UntilDate.Value)
                .FirstOrDefault();

            if (lastBill == null)
            {
                return false;
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

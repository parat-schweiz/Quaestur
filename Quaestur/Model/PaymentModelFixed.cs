using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PaymentModelFixed : IPaymentModel
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

        public IEnumerable<PaymentParameterType> PersonalParameterTyoes
        {
            get { return new PaymentParameterType[0]; } 
        }

        public decimal ComputeYearlyAmount(Membership membership)
        {
            return _membershipType.PaymentParameters
                .Single(p => p.Key == AmountKey).Value;
        }

        public string CreateExplainationLatex(Translator translator, IEnumerable<PersonalPaymentParameter> parameters)
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

        public string CreateExplainationLatex(Translator translator, Membership membership)
        {
            return CreateExplainationLatex(translator, membership.Person.Value.PaymentParameters);
        }

        public string CreateExplainationText(Translator translator, IEnumerable<PersonalPaymentParameter> parameters)
        {
            return translator.Get(
                "PaymentModel.Fixed.Explaination.Text", 
                "Explaination text for the fixed payment model", 
                "Fixed membership fee amount");
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
            var bills = database
                .Query<Bill>(DC.Equal("membershipid", membership.Id.Value))
                .OrderByDescending(m => m.UntilDate.Value);
            var lastBill = bills.FirstOrDefault();

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
                var secondLastBill = bills.Skip(1).FirstOrDefault();

                if (secondLastBill == null)
                {
                    return false;
                }
                else if (secondLastBill.Status.Value != BillStatus.Payed)
                {
                    return false;
                }
                else
                {
                    return DateTime.UtcNow.Date.Subtract(lastBill.CreatedDate.Value.Date).TotalDays <= days;
                }
            }
        }

        public int GetBillAdvancePeriod()
        {
            return (int)_membershipType.PaymentParameters
                .Single(p => p.Key == VotingRightGraceAfterBillKey).Value;
        }

        public decimal ComputeYearlyAmount(IEnumerable<PersonalPaymentParameter> parameters)
        {
            throw new NotImplementedException();
        }
    }
}

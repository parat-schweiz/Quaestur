using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Hospes
{
    public class PaymentModelFlat : IPaymentModel
    {
        private const string AmountKey = "PaymentModel.Fixed.Parameter.Amount";
        private const string BillingPeriodKey = "PaymentModel.Fixed.Parameter.BillingPeriod";
        private const string ReminderPeriodKey = "PaymentModel.Fixed.Parameter.ReminderPeriod";

        private MembershipType _membershipType;
        private IDatabase _database;

        public PaymentModelFlat(MembershipType membershipType, IDatabase database)
        {
            _membershipType = membershipType;
            _database = database;
        }

        public PaymentModelFlat()
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
            }
        }

        public IEnumerable<PaymentParameterType> PersonalParameterTypes
        {
            get { return new PaymentParameterType[0]; } 
        }

        public string CreateExplainationLatex(Translator translator, Membership membership)
        {
            var flatAmount = _membershipType.PaymentParameters
                .Single(p => p.Key == AmountKey).Value;
            var currency = _database.Query<SystemWideSettings>()
                .Single().Currency.Value;
            var fixedInfo = translator.Get(
                "PaymentModel.Flat.Explaination.LaTeX",
                "Explaination text for the flat payment model",
                "Flat membership fee");

            var text = new StringBuilder();

            text.Append(@"~~~~~");
            text.Append(fixedInfo);
            text.Append(" & ");
            text.Append(currency);
            text.Append(" & ");
            text.Append(Currency.Format(flatAmount));
            text.Append(@" \\");
            text.AppendLine();

            return text.ToString();
        }

        public string CreateExplainationText(Translator translator, Membership membership)
        {
            return translator.Get(
                "PaymentModel.Flat.Explaination.Text",
                "Explaination text for the flat payment model",
                "Flat membership fee");
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

        public bool HasVotingRight(Membership membership)
        {
            var bills = _database
                .Query<Bill>(DC.Equal("membershipid", membership.Id.Value))
                .OrderBy(m => m.FromDate.Value);
            var firstBill = bills.FirstOrDefault();

            if (firstBill == null)
            {
                return false;
            }
            else if (firstBill.Status.Value == BillStatus.Payed)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public int GetBillAdvancePeriod()
        {
            return 30;
        }

        public decimal ComputeAmount(Membership membership, DateTime fromDate, DateTime untilDate)
        {
            if (fromDate.Date == membership.StartDate.Value.Date)
            {
                return _membershipType.PaymentParameters
                    .Single(p => p.Key == AmountKey).Value;
            }
            else
            {
                return 0m; 
            }
        }

        public bool RequireParameterUpdate(Membership membership)
        {
            return false;
        }

        public bool InviteForParameterUpdate(Membership membership)
        {
            return false;
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using BaseLibrary;
using SiteLibrary;

namespace Hospes
{
    public abstract class PaymentModelProRataTemporis : IPaymentModel
    {
        public abstract IEnumerable<PaymentParameterType> ParameterTypes { get; }

        public abstract IEnumerable<PaymentParameterType> PersonalParameterTypes { get; }

        public decimal ComputeAmount(Membership membership, DateTime fromDate, DateTime untilDate)
        {
            var periodDays = Math.Round((decimal)untilDate.Date.Subtract(fromDate.Date).TotalDays, 0) + 1m;
            var yearDayCount = 365m + (DateTime.IsLeapYear(DateTime.Now.Year) ? 1m : 0m);
            var yearlyAmount = ComputeYearlyAmount(membership);
            var periodAmount = yearlyAmount / yearDayCount * periodDays;
            periodAmount = Math.Round(periodAmount * 20m, 0) / 20m;

            switch (periodDays)
            {
                case 366m:
                case 365m:
                case 364m:
                case 363m:
                case 362m:
                case 361m:
                case 360m:
                    periodAmount = yearlyAmount;
                    break;
                case 182m:
                case 181m:
                case 180m:
                    periodAmount = yearlyAmount / 2m;
                    break;
                case 120m:
                    periodAmount = yearlyAmount / 3m;
                    break;
                case 90m:
                    periodAmount = yearlyAmount / 4m;
                    break;
                case 60m:
                    periodAmount = yearlyAmount / 6m;
                    break;
                case 30m:
                    periodAmount = yearlyAmount / 12m;
                    break;
            }

            return periodAmount;
        }

        public abstract string CreateExplainationLatex(Translator translator, Membership membership);

        public abstract string CreateExplainationText(Translator translator, Membership membership);

        public abstract int GetBillAdvancePeriod();

        public abstract int GetBillingPeriod();

        public abstract int GetReminderPeriod();

        public abstract bool HasVotingRight(Membership membership);

        protected abstract decimal ComputeYearlyAmount(Membership membership);

        public abstract bool RequireParameterUpdate(Membership membership);

        public abstract bool InviteForParameterUpdate(Membership membership);
    }
}

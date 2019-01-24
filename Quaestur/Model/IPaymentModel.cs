using System;
using System.Linq;
using System.Collections.Generic;

namespace Quaestur
{
    public class PaymentParameterType
    {
        public string Key { get; private set; }
        public decimal DefaultValue { get; private set; }
        public Func<Translator, string> GetTranslation { get; private set; }

        public PaymentParameterType(string key, decimal defaultValue, Func<Translator, string> getTranslation)
        {
            Key = key;
            DefaultValue = defaultValue;
            GetTranslation = getTranslation;
        }
    }

    public interface IPaymentModel
    { 
        IEnumerable<PaymentParameterType> ParameterTypes { get; }

        decimal ComputeYearlyAmount(Membership membership);

        string CreateExplainationLatex(Translator translator, Membership membership);

        bool HasVotingRight(IDatabase database, Membership membership);

        int GetBillingPeriod();

        int GetReminderPeriod();

        int GetBillAdvancePeriod();
    }
}

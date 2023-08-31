using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BaseLibrary;
using Nancy;
using Nancy.Helpers;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using Nancy.Security;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class PaymentInfo
    {
        public string Label { get; private set; }
        public string Value { get; private set; }

        public PaymentInfo(string label, string value)
        {
            Label = label;
            Value = value;
        }
    }

    public class PaymentViewModel : MasterViewModel
    {
        public List<PaymentInfo> Infos;
        public string PhraseQuestion;
        public string PhraseButtonPay;
        public string PhraseButtonCancel;
        public string CancelReturnUrl;
        public string AuthCode;
        public bool IsPayable;
        public string Id;

        public PaymentViewModel(IDatabase database, Translator translator, Session session, Person person, ApiClient client, PaymentTransaction transaction, SystemWideSettings settings, int creditsBalance, string authCode)
            : base(database, translator,
                   translator.Get("Payment.Title", "Title of the payment page", "Payment"),
                   session)
        {
            PhraseQuestion = translator.Get("Payment.Question", "Question user wether to pay with Quaestur.", "Do you whish to pay your purchase using Quaestur?");
            PhraseButtonPay = translator.Get("Payment.Button.Pay", "Pay button on the Payment page", "Pay");
            PhraseButtonCancel = translator.Get("Payment.Button.Cancel", "Cancel button on the Payment page", "Cancel");
            var split = new PaymentTransactionSplit(settings, transaction, creditsBalance);
            Infos = new List<PaymentInfo>();
            Infos.Add(new PaymentInfo(translator.Get("Payment.Info.Store", "Store info on the Payment page", "Store"), client.Name.Value[translator.Language]));
            Infos.Add(new PaymentInfo(translator.Get("Payment.Info.Reason", "Reason info on the Payment page", "Reason"), FormatReason(transaction)));
            Infos.Add(new PaymentInfo(translator.Get("Payment.Info.Amount.Currency", "Currency amount info on the Payment page", "Value"), FormatMoneyCurrency(settings, transaction.Amount)));
            Infos.Add(new PaymentInfo(translator.Get("Payment.Info.Amount.Credits", "Credits amount info on the Payment page", "Value"), FormatCredits(translator, split.ValueInCredits)));
            Infos.Add(new PaymentInfo(translator.Get("Payment.Info.Balance", "Balance info on the Payment page", "Your current balance"), FormatCredits(translator, creditsBalance)));
            Infos.Add(new PaymentInfo(translator.Get("Payment.Info.Payable", "Payment info on the Payment page", "Payment"), FormatCredits(translator, split.PayableCredits)));
            if (split.PayableCurrency > 0M)
            {
                Infos.Add(new PaymentInfo(string.Empty, FormatMoneyCurrency(settings, split.PayableCurrency)));
            }
            CancelReturnUrl = transaction.CancelReturnUrl;
            AuthCode = authCode;
            IsPayable = true;
            Id = transaction.Id.ToString();
        }

        private static string FormatMoneyCurrency(SystemWideSettings settings, decimal amount)
        {
            return string.Format("{0} {1}", amount.FormatMoney(), settings.Currency.Value);
        }

        private static string FormatCredits(Translator translator, int amount)
        {
            return string.Format("{0} {1}", amount, translator.Get("Credits", "Credits virutal currency.", "Credits"));
        }

        private static string FormatReason(PaymentTransaction transaction)
        {
            if (!string.IsNullOrEmpty(transaction.Url))
            {
                return string.Format("<a href=\"{0}\">{1}</a>", transaction.Url, transaction.Reason);
            }
            else
            {
                return transaction.Reason;
            }
        }
    }

    public class PaymentModule : QuaesturModule
    {
        public Negotiator Error(string returnUrl)
        {
            return View["View/info.sshtml", new InfoViewModel(Database, Translator,
                Translate("Payment.Error.Title", "Title of the message when payment requst is invalid", "Invalid request"),
                Translate("Payment.Error.Message", "Text of the message when payment requst is invalid", "Payment request is invalid."),
                Translate("Payment.Error.BackLink", "Link text of the message when payment requstis invalid", "Back"),
                returnUrl)];
        }

        public PaymentModule()
        {
            this.RequiresAuthentication();

            base.Get("/payments/show/{id}", parameters =>
            {
                PaymentTransactions.Instance.Expire();
                var idString = parameters.id;

                if (Guid.TryParse(idString, out Guid id))
                {
                    var transaction = PaymentTransactions.Instance.Get(id);

                    if ((transaction != null))
                    {
                        if ((transaction.State == PaymentTransactionState.Prepared))
                        {
                            var apiClient = Database.Query<ApiClient>(transaction.ShopId);
                            var settings = Database.Query<SystemWideSettings>().Single();
                            var creditsBalance = Database
                                .Query<Credits>(DC.Equal("ownerid", CurrentSession.User.Id.Value))
                                .Sum(c => c.Amount.Value);

                            if (apiClient != null)
                            {
                                var authCode = PaymentTransactions.Instance.ComputeAuthCode(transaction.Id, CurrentSession.User.Id);
                                Global.Log.Notice(
                                    "Payment transaction id {0} shown to user {1} for store {2}.",
                                    transaction.Id,
                                    CurrentSession.User.GetText(Translator),
                                    apiClient.Name.Value[Language.English]);
                                return View["View/payment.sshtml",
                                    new PaymentViewModel(Database, Translator, CurrentSession, CurrentSession.User, apiClient, transaction, settings, creditsBalance, authCode)];
                            }
                            else
                            {
                                Global.Log.Notice(
                                    "Payment transaction id {0} for user {1} not show due to not finding store.",
                                    transaction.Id,
                                    CurrentSession.User.GetText(Translator));
                                return Error(transaction.CancelReturnUrl);
                            }
                        }
                        else
                        {
                            Global.Log.Notice(
                                "Payment transaction id {0} for user {1} in wrong state {2}.",
                                transaction.Id,
                                CurrentSession.User.GetText(Translator),
                                transaction.State.ToString());
                            return Error("/");
                        }
                    }
                    else
                    {
                        Global.Log.Notice(
                            "Payment transaction id {0} for user {1} not found.",
                            id,
                            CurrentSession.User.GetText(Translator));
                        return Error("/");
                    }
                }
                else
                {
                    Global.Log.Info(
                        "Payment transaction id parse error by user {0}.",
                        CurrentSession.User.GetText(Translator));
                    return Error("/");
                }
            });
            base.Get("/payments/authorize/{id}/{authcode}", parameters =>
            {
                PaymentTransactions.Instance.Expire();
                var idString = parameters.id;
                string authCode = parameters.authcode;

                if (Guid.TryParse(idString, out Guid id) &&
                    !string.IsNullOrEmpty(authCode))
                {
                    var transaction = PaymentTransactions.Instance.Get(id);

                    if (transaction != null)
                    {
                        if (transaction.State == PaymentTransactionState.Prepared)
                        {
                            if (authCode == PaymentTransactions.Instance.ComputeAuthCode(id, CurrentSession.User.Id.Value))
                            {
                                var apiClient = Database.Query<ApiClient>(transaction.ShopId);
                                var settings = Database.Query<SystemWideSettings>().Single();

                                if (apiClient != null)
                                {
                                    transaction.Authorize(CurrentSession.User.Id.Value);
                                    Global.Log.Notice(
                                        "Payment transaction id {0} authorized by user {1} for store {2} with amount {3}.",
                                        transaction.Id,
                                        CurrentSession.User.GetText(Translator),
                                        apiClient.Name.Value[Language.English],
                                        transaction.Amount.FormatMoney());

                                    return Response.AsRedirect(transaction.PayReturnUrl);
                                }
                                else
                                {
                                    PaymentTransactions.Instance.Remove(transaction.Id);
                                    Global.Log.Notice(
                                        "Payment transaction id {0} canceled by for {1} due to store inconsistency",
                                        transaction.Id,
                                        CurrentSession.User.GetText(Translator));

                                    return Response.AsRedirect(transaction.CancelReturnUrl);
                                }
                            }
                            else
                            {
                                Global.Log.Notice(
                                    "Payment transaction id {0} for user {1} with wrong authcode.",
                                    transaction.Id,
                                    CurrentSession.User.GetText(Translator));
                                return Response.AsRedirect("/");
                            }
                        }
                        else
                        {
                            Global.Log.Notice(
                                "Payment transaction id {0} for user {1} with wrong state {2}.",
                                transaction.Id,
                                CurrentSession.User.GetText(Translator),
                                transaction.State.ToString());
                            return Response.AsRedirect("/");
                        }
                    }
                    else
                    {
                        Global.Log.Notice(
                            "Payment transaction id {0} for user {1} not found.",
                            transaction.Id,
                            CurrentSession.User.GetText(Translator));
                        return Response.AsRedirect("/");
                    }
                }
                else
                {
                    Global.Log.Info(
                        "Payment transaction id or authcode parse error for user {0}.",
                        CurrentSession.User.GetText(Translator));
                    return Response.AsRedirect("/");
                }
            });
        }
    }

    public class PaymentTransactions
    {
        private readonly Dictionary<Guid, PaymentTransaction> _list;
        private readonly object _lock = new object();
        private readonly byte[] _key = Rng.Get(32);

        public PaymentTransactions()
        {
            _list = new Dictionary<Guid, PaymentTransaction>();
        }

        public void Add(PaymentTransaction transaction)
        {
            lock (_lock)
            {
                _list.Add(transaction.Id, transaction);
            }
        }

        public PaymentTransaction Get(Guid id)
        {
            lock (_lock)
            {
                if (_list.ContainsKey(id))
                {
                    return _list[id];
                }
                else
                {
                    return null;
                }
            }
        }

        public string ComputeAuthCode(Guid id, Guid buyerId)
        {
            var transaction = Get(id);

            if (transaction != null)
            {
                var builder = new StringBuilder();
                builder.AppendLine(transaction.Id.ToString());
                builder.AppendLine(transaction.ShopId.ToString());
                builder.AppendLine(transaction.Amount.ToString());
                builder.AppendLine(transaction.Reason);
                builder.AppendLine(transaction.Url);
                builder.AppendLine(buyerId.ToString());

                using (var hmac = new HMACSHA256())
                {
                    hmac.Key = _key;
                    return hmac.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString())).ToHexString();
                }
            }
            else
            {
                return string.Empty;
            }
        }

        public void Remove(Guid id)
        {
            lock (_lock)
            {
                if (_list.ContainsKey(id))
                {
                    _list.Remove(id);
                }
            }
        }

        public void Expire()
        {
            lock (_lock)
            {
                var removeList = _list.Values
                    .Where(t => DateTime.UtcNow.Subtract(t.Moment).TotalMinutes > 10d)
                    .ToList();

                foreach (var transaction in removeList)
                {
                    _list.Remove(transaction.Id);
                }
            }
        }

        private static PaymentTransactions _instance = null;

        public static PaymentTransactions Instance
        { 
            get
            {
                if (_instance == null)
                {
                    _instance = new PaymentTransactions();
                }

                return _instance;
            }
        }
    }

    public class PaymentTransactionSplit
    { 
        public decimal PayableCurrency { get; private set; }
        public int PayableCredits { get; private set; }
        public int ValueInCredits { get; private set; }

        public PaymentTransactionSplit(SystemWideSettings settings, PaymentTransaction transaction, int creditsBalance)
        {
            ValueInCredits = Convert.ToInt32(transaction.Amount * settings.CreditsPerCurrency.Value);
            PayableCredits = Math.Min(creditsBalance, ValueInCredits);
            var overflowCredits = ValueInCredits - PayableCredits;
            PayableCurrency = overflowCredits / settings.CreditsPerCurrency.Value;
        }
    }

    public enum PaymentTransactionState
    {
        Prepared,
        Authorized,
    }

    public class PaymentTransaction
    {
        public Guid Id { get; private set; }
        public Guid ShopId { get; private set; }
        public Guid BuyerId { get; private set; }
        public decimal Amount { get; private set; }
        public string Reason { get; private set; }
        public string Url { get; private set; }
        public DateTime Moment { get; private set; }
        public string PayReturnUrl { get; private set; }
        public string CancelReturnUrl { get; private set; }
        public PaymentTransactionState State { get; private set; }

        public void Authorize(Guid buyerId)
        {
            BuyerId = buyerId;
            State = PaymentTransactionState.Authorized;
        }

        public PaymentTransaction(Guid shopId, decimal amount, string reason, string url, string payReturnUrl, string cancelReturnUrl)
        {
            ShopId = shopId;
            Amount = amount;
            Reason = reason;
            Url = url;
            Id = Guid.NewGuid();
            Moment = DateTime.UtcNow;
            PayReturnUrl = payReturnUrl;
            CancelReturnUrl = cancelReturnUrl;
            State = PaymentTransactionState.Prepared;
        }
    }
}

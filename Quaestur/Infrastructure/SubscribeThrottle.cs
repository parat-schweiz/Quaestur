using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using SiteLibrary;

namespace Quaestur
{
    public class SubscribeThrottleEntry
    {
        public readonly TimeSpan _workTime = TimeSpan.FromHours(1);
        public readonly TimeSpan _sendTime = TimeSpan.FromDays(7);

        private readonly List<DateTime> _sent;
        private readonly Dictionary<long, DateTime> _requested;
        private long _nextNumber = 0;

        public int SentCount { get { return _sent.Count; } }

        public SubscribeThrottleEntry()
        {
            _sent = new List<DateTime>();
            _requested = new Dictionary<long, DateTime>();
        }

        public void Request(out int bitlength, out long number)
        {
            number = _nextNumber;
            bitlength = BitLength; 
            _nextNumber++;
            _requested.Add(number, DateTime.UtcNow);
        }

        public bool Check(long number)
        {
            if (_requested.ContainsKey(number))
            {
                return DateTime.UtcNow <= _requested[number].Add(_workTime);
            }
            return false;
        }

        public void Sent(long number)
        {
            if (_requested.ContainsKey(number))
            {
                _requested.Remove(number);
                _sent.Add(DateTime.UtcNow);
            }
        }

        public int Count
        {
            get
            {
                return _sent.Count + _requested.Count;
            }
        }

        private int BitLength
        {
            get
            {
                return 15 + (Count / 2);
            }
        }

        public void Purge()
        {
            foreach (var request in _requested.ToList())
            {
                if (request.Value.Add(_workTime) > DateTime.UtcNow)
                {
                    _requested.Remove(request.Key);
                }
            }

            _sent.RemoveAll(s => s.Add(_sendTime) > DateTime.UtcNow);
        }
    }

    public class SubscribeThrottle
    {
        private readonly IDatabase _database;
        private readonly Dictionary<string, SubscribeThrottleEntry> _mails;
        private DateTime _lastPurge;
        private DateTime _lastReload;
        private List<MailDomain> _domains;

        public SubscribeThrottle(IDatabase database)
        {
            _database = database;
            _lastPurge = DateTime.UtcNow;
            _mails = new Dictionary<string, SubscribeThrottleEntry>();
            _domains = new List<MailDomain>();
            Reload();
        }

        private void CheckReload()
        {
            if (DateTime.UtcNow > _lastReload.AddMinutes(15))
            {
                Reload();
            }
        }

        private void Reload()
        {
            lock (_domains)
            {
                _domains.Clear();
                _domains.AddRange(_database.Query<MailDomain>());
            }
            _lastReload = DateTime.UtcNow;
        }

        private void Purge()
        {
            lock (_mails)
            {
                if (DateTime.UtcNow > _lastPurge.AddMinutes(15))
                {
                    foreach (var entry in _mails.ToList())
                    {
                        entry.Value.Purge();
                        if (entry.Value.Count < 1)
                        {
                            _mails.Remove(entry.Key);
                        }
                    }
                    _lastPurge = DateTime.UtcNow;
                }
            }
        }

        private bool IsMatch(MailDomain mailDomain, string domainValue)
        {
            if (mailDomain.Value.Value.StartsWith("*.", StringComparison.Ordinal))
            {
                var pattern = mailDomain.Value.Value.Substring(2);
                while (domainValue.Length >= pattern.Length)
                {
                    if (domainValue == pattern)
                    {
                        return true;
                    }
                    else if (Regex.IsMatch(domainValue, @"^.+\..+$"))
                    {
                        domainValue = domainValue.Substring(domainValue.IndexOf('.') + 1);
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            else
            {
                return mailDomain.Value.Value == domainValue;
            }
        }

        private int DomainLength(MailDomain mailDomain)
        {
            if (mailDomain.Value.Value.StartsWith("*.", StringComparison.Ordinal))
            {
                return mailDomain.Value.Value.Length - 2;
            }
            else
            {
                return mailDomain.Value.Value.Length;
            }
        }

        private MailDomain FindDomain(string domainValue)
        {
            CheckReload();
            lock (_domains)
            {
                return _domains
                    .Where(d => IsMatch(d, domainValue))
                    .OrderByDescending(DomainLength)
                    .FirstOrDefault();
            }
        }

        private MailDomain GetMailDomain(string mailAddress, bool create)
        {
            var domainValue = GetDomainPart(mailAddress);
            var domain = FindDomain(domainValue);
            if ((domain == null) && create)
            {
                domain = new MailDomain(Guid.NewGuid());
                domain.Value.Value = domainValue;
                domain.Type.Value = MailDomainType.Private;
                _domains.Add(domain);
            }
            return domain;
        }

        private string GetDomainPart(string mailAddress)
        {
            var match = Regex.Match(mailAddress, "^.+@(.+)$");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return mailAddress;
            }
        }

        private string RemovePlusPart(string mailAddress)
        {
            var match = Regex.Match(mailAddress, "^(.+?)(?:\\+.*)@(.+)$");
            if (match.Success)
            {
                return string.Format("{0}@{1}", match.Groups[1].Value, match.Groups[2].Value);
            }
            else
            {
                return mailAddress;
            }
        }

        private string GetEntryKey(string mailAddress, MailDomain mailDomain)
        { 
            switch (mailDomain.Type.Value)
            {
                case MailDomainType.Private:
                    return mailDomain.Value.Value;
                case MailDomainType.Hoster:
                    return RemovePlusPart(mailAddress);
                default:
                    throw new NotSupportedException();
            }
        }

        public void Request(string mailAddress, out int bitlength, out long number)
        {
            Purge();

            lock (_mails)
            {
                var mailDomain = GetMailDomain(mailAddress, true);
                var key = GetEntryKey(mailAddress, mailDomain);

                if (!_mails.ContainsKey(key))
                {
                    _mails.Add(key, new SubscribeThrottleEntry());
                }

                _mails[key].Request(out bitlength, out number);
                CreateOrUpdateDomain(mailDomain);
            }
        }

        private void CreateOrUpdateDomain(MailDomain mailDomain)
        {
            if (mailDomain.NewlyCreated)
            {
                _database.Save(mailDomain);
            }
            else if (mailDomain.Dirty)
            {
                UpdateDomain(mailDomain);
            }
        }

        private void UpdateDomain(MailDomain mailDomain)
        {
            lock (_domains)
            {
                using (var transaction = _database.BeginTransaction())
                {
                    var reload = _database.Query<MailDomain>(mailDomain.Id.Value);
                    foreach (var field in mailDomain.Fields.Where(f => f.Dirty))
                    {
                        field.TransferValue(reload.Fields.Single(f => f.ColumnName == field.ColumnName));
                    }
                    _database.Save(reload);
                    transaction.Commit();
                    _domains.Remove(mailDomain);
                    _domains.Add(reload);
                }
            }
        }

        public bool Check(string mailAddress, long number)
        {
            lock (_mails)
            {
                var mailDomain = GetMailDomain(mailAddress, false);
                if (mailDomain != null)
                {
                    var key = GetEntryKey(mailAddress, mailDomain);

                    if (_mails.ContainsKey(key))
                    {
                        var entry = _mails[key];
                        return entry.Check(number);
                    }
                }
            }
            return false;
        }

        public void Subscribed(string mailAddress)
        {
            var mailDomain = GetMailDomain(mailAddress, true);
            mailDomain.Subscribed.Value++;
            CreateOrUpdateDomain(mailDomain);
        }

        public void Unsubscribed(string mailAddress)
        {
            var mailDomain = GetMailDomain(mailAddress, true);
            mailDomain.Unsubscribed.Value++;
            CreateOrUpdateDomain(mailDomain);
        }

        public void Sent(string mailAddress, long number)
        {
            var mailDomain = GetMailDomain(mailAddress, true);
            var key = GetEntryKey(mailAddress, mailDomain);

            if (_mails.ContainsKey(key))
            {
                var entry = _mails[key];
                entry.Sent(number);
                mailDomain.Checked.Value++;
                CreateOrUpdateDomain(mailDomain);
                var text = string.Format("{0} subscribe mails were sent to {1} in 7d", entry.SentCount, mailAddress);
                switch (entry.SentCount)
                {
                    case 3:
                    case 5:
                        Global.Log.Notice(text);
                        Global.Mail.SendAdmin("Some subscribe mails", text);
                        break;
                    case 10:
                    case 15:
                    case 20:
                        Global.Log.Notice(text);
                        Global.Mail.SendAdmin("Many subscribe mails", text);
                        break;
                    case 30:
                    case 40:
                        Global.Log.Notice(text);
                        Global.Mail.SendAdmin("Too many subscribe mails", text);
                        break;
                    case 50:
                    case 75:
                    case 100:
                    case 150:
                    case 200:
                    case 300:
                    case 400:
                    case 500:
                    case 750:
                    case 1000:
                        Global.Log.Notice(text);
                        Global.Mail.SendAdmin("TOO MANY subscribe mails", text);
                        break;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

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

        public void AddSent()
        {
            _sent.Add(DateTime.UtcNow);
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
                return 14 + (Count / 2);
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
        private readonly Dictionary<string, SubscribeThrottleEntry> _mails;
        private DateTime _lastPurge;

        public SubscribeThrottle()
        {
            _lastPurge = DateTime.UtcNow;
            _mails = new Dictionary<string, SubscribeThrottleEntry>();
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

        public void Request(string mailAddress, out int bitlength, out long number)
        {
            Purge();

            lock (_mails)
            {
                if (!_mails.ContainsKey(mailAddress))
                {
                    _mails.Add(mailAddress, new SubscribeThrottleEntry());
                }

                _mails[mailAddress].Request(out bitlength, out number);
            }
        }

        public bool Check(string mailAddress, long number)
        {
            lock (_mails)
            {
                if (_mails.ContainsKey(mailAddress))
                {
                    var entry = _mails[mailAddress];
                    return entry.Check(number);
                }
            }
            return false;
        }

        public void Sent(string mailAddress, long number)
        {
            if (_mails.ContainsKey(mailAddress))
            {
                var entry = _mails[mailAddress];
                entry.Sent(number);
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

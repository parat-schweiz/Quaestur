using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Quaestur
{
    public class SecurityThrottle
    {
        private Dictionary<string, List<DateTime>> _fails;

        public SecurityThrottle()
        {
            _fails = new Dictionary<string, List<DateTime>>();
        }

        public void Fail(string username, bool secondFactor)
        {
            if (!string.IsNullOrEmpty(username))
            {
                FailInternal(string.Empty, secondFactor, true);
                FailInternal(username, secondFactor, false);
            }
        }

        private void FailInternal(string username, bool secondFactor, bool global)
        {
            if (secondFactor)
            {
                username += "___________________2FA";
            }

            int timeCount = 0;

            lock (_fails)
            {
                if (_fails.ContainsKey(username))
                {
                    var times = _fails[username];

                    foreach (var time in times
                        .Where(t => DateTime.UtcNow.Subtract(t).TotalHours > 1d)
                        .ToList())
                    {
                        times.Remove(time);
                    }

                    times.Add(DateTime.UtcNow);
                    timeCount = times.Count;
                }
                else
                {
                    _fails.Add(username, new List<DateTime>(new[] { DateTime.UtcNow }));
                    timeCount = 1;
                }
            }

            if (global)
            {
                switch (timeCount)
                {
                    case 100:
                    case 200:
                    case 500:
                    case 1000:
                    case 5000:
                    case 25000:
                        Global.Log.Warning("Quaestur recored {1} total wrong {0}.", secondFactor ? "2fas" : "logins", timeCount);
                        Global.Mail.SendAdmin("Wrong login alert", string.Format("Quaestur recored {1} total wrong {0}.", secondFactor ? "2fas" : "logins", timeCount));
                        break;
                }
            }
            else
            {
                switch (timeCount)
                {
                    case 10:
                    case 20:
                    case 50:
                    case 100:
                    case 500:
                    case 2500:
                        Global.Log.Warning("Quaestur recored {1} wrong {0} for account {2}.", secondFactor ? "2fas" : "logins", timeCount, username);
                        Global.Mail.SendAdmin("Wrong login alert", string.Format("Quaestur recored {1} wrong {0} for account {2}.", secondFactor ? "2fas" : "logins", timeCount, username));
                        break;
                }
            }
        }

        public void Check(string username, bool secondFactor)
        {
            int globalTimeCount = CheckInternal(string.Empty, secondFactor);
            int userTimeCount = CheckInternal(username, secondFactor);

            if (userTimeCount >= 20 ||
                globalTimeCount >= 200)
            {
                Thread.Sleep(2000);
            }
            else if (userTimeCount >= 10 ||
                     globalTimeCount >= 100)
            {
                Thread.Sleep(1000);
            }
            else if (userTimeCount >= 5 ||
                     globalTimeCount >= 50)
            {
                Thread.Sleep(500);
            }
            else if (userTimeCount >= 3 ||
                     globalTimeCount >= 30)
            {
                Thread.Sleep(200);
            }
            else if (userTimeCount >= 1 ||
                     globalTimeCount >= 10)
            {
                Thread.Sleep(100);
            }
        }

        private int CheckInternal(string username, bool secondFactor)
        {
            if (secondFactor)
            {
                username += "___________________2FA";
            }

            int timeCount = 0;

            lock (_fails)
            {
                if (_fails.ContainsKey(username))
                {
                    var times = _fails[username];

                    foreach (var time in times
                        .Where(t => DateTime.UtcNow.Subtract(t).TotalHours > 1d)
                        .ToList())
                    {
                        times.Remove(time);
                    }

                    timeCount = times.Count;
                }
            }

            return timeCount;
        }
    }
}

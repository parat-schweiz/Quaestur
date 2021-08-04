using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace Publicus
{
    public class SecurityThrottle
    {
        private Dictionary<string, List<DateTime>> _fails;
        private Semaphore _globalLock = new Semaphore(10, 10);

        public SecurityThrottle()
        {
            _fails = new Dictionary<string, List<DateTime>>();
        }

        public List<DateTime> GetFailList(string username)
        {
            lock (_fails)
            {
                if (!_fails.ContainsKey(username))
                {
                    _fails.Add(username, new List<DateTime>());
                }

                return _fails[username];
            }
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
                username += ":2fa";
            }
            else
            {
                username += ":pwd";
            }

            int timeCount = 0;
            var list = GetFailList(username);

            lock (list)
            {
                foreach (var time in list
                    .Where(t => DateTime.UtcNow.Subtract(t).TotalHours > 1d)
                    .ToList())
                {
                    list.Remove(time);
                }

                list.Add(DateTime.UtcNow);
                timeCount = list.Count;
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
                        Global.Log.Warning("Publicus recored {1} total wrong {0}.", secondFactor ? "2fas" : "logins", timeCount);
                        Global.Mail.SendAdmin("Wrong login alert", string.Format("Publicus recored {1} total wrong {0}.", secondFactor ? "2fas" : "logins", timeCount));
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
                        Global.Log.Warning("Publicus recored {1} wrong {0} for account {2}.", secondFactor ? "2fas" : "logins", timeCount, username);
                        Global.Mail.SendAdmin("Wrong login alert", string.Format("Publicus recored {1} wrong {0} for account {2}.", secondFactor ? "2fas" : "logins", timeCount, username));
                        break;
                }
            }
        }

        public void Check(string username, bool secondFactor)
        {
            if (secondFactor)
            {
                username += ":2fa";
            }
            else
            {
                username += ":pwd";
            }

            var globalList = GetFailList(string.Empty);
            var userList = GetFailList(username);
            var globalTimeCount = 0;

            lock (globalList)
            {
                foreach (var time in globalList
                    .Where(t => DateTime.UtcNow.Subtract(t).TotalHours > 1d)
                    .ToList())
                {
                    globalList.Remove(time);
                }

                globalTimeCount = globalList.Count;
            }

            _globalLock.WaitOne();

            try
            {
                lock (userList)
                {
                    foreach (var time in userList
                        .Where(t => DateTime.UtcNow.Subtract(t).TotalHours > 1d)
                        .ToList())
                    {
                        userList.Remove(time);
                    }

                    var userTimeCount = userList.Count;

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
            }
            finally
            {
                _globalLock.Release(); 
            }
        }
    }
}

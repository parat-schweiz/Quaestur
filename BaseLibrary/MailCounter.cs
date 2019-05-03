using System;

namespace BaseLibrary
{
    public class MailCounter
    {
        private readonly int _refillMinutes;
        private readonly int _refillCount;
        private int _counter;
        private DateTime _last;
        private readonly object _lock = new object();

        public MailCounter(int refillCount, int refillMinutes)
        {
            _refillCount = refillCount;
            _refillMinutes = refillMinutes;
            _counter = _refillCount;
            _last = DateTime.UtcNow;
        }

        public bool Available
        {
            get
            {
                lock (_lock)
                {
                    CheckRefill();
                    return _counter > 0;
                }
            }
        }

        public void Used()
        {
            lock (_lock)
            {
                CheckRefill();
                if (_counter > 0)
                {
                    _counter--;
                }
            }
        }

        private void CheckRefill()
        {
            if (DateTime.UtcNow >= _last.AddMinutes(_refillMinutes))
            {
                _counter = _refillCount;
                _last = DateTime.UtcNow;
            }
        }
    }
}

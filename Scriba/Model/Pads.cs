using System;
using System.Threading;
using System.Collections.Generic;

namespace Scriba
{
    public class Pads
    {
        private Dictionary<Guid, Pad> _pads;

        public void Add(Pad pad)
        {
            _pads.Add(pad.Id, pad); 
        }

        public PadLock Get(Guid id)
        {
            if (_pads.ContainsKey(id))
            {
                return new PadLock(_pads[id]);
            }
            else
            {
                return new PadLock(null);
            }
        }

        public Pads()
        {
            _pads = new Dictionary<Guid, Pad>();
        }
    }

    public class PadLock : IDisposable
    {
        public Pad Pad { get; private set; }

        public PadLock(Pad pad)
        {
            Pad = pad;

            if (Pad != null)
            {
                Monitor.Enter(Pad);
            }
        }

        public void Dispose()
        {
            if (Pad != null)
            {
                Monitor.Exit(Pad);
                Pad = null;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace SiteLibrary
{
    public class CacheEntry<TKey, TValue>
    { 
        public TKey Key { get; private set; }
        public TValue Value { get; private set; }
        public DateTime LastUsed { get; set; }
        public DateTime Created { get; private set; }

        public CacheEntry(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            Created = DateTime.UtcNow;
            LastUsed = DateTime.UtcNow;
        }
    }

    public class Cache<TKey, TValue>
    {
        public TimeSpan MaxCreationAge { get; set; }
        public TimeSpan MaxUsedAge { get; set; }

        private readonly Dictionary<TKey, CacheEntry<TKey, TValue>> _entries;

        public Cache(TimeSpan maxCreationAge, TimeSpan maxUsedAge)
        {
            _entries = new Dictionary<TKey, CacheEntry<TKey, TValue>>();
            MaxCreationAge = maxCreationAge;
            MaxUsedAge = maxUsedAge;
        }

        public bool Contains(TKey key)
        {
            lock (_entries)
            {
                return _entries.ContainsKey(key);
            }
        }

        public void Add(TKey key, TValue value)
        {
            lock (_entries)
            {
                _entries.Add(key, new CacheEntry<TKey, TValue>(key, value));
            }
        }

        public void Remove(TKey key)
        {
            lock (_entries)
            {
                _entries.Remove(key);
            }
        }

        public TValue Get(TKey key)
        {
            lock (_entries)
            {
                if (_entries.ContainsKey(key))
                {
                    var entry = _entries[key];
                    if (Expired(entry))
                    {
                        _entries.Remove(key);
                        return default(TValue);
                    }
                    else
                    {
                        entry.LastUsed = DateTime.UtcNow;
                        return entry.Value;
                    }
                }
                else
                {
                    return default(TValue);
                }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_entries)
            {
                if (_entries.TryGetValue(key, out CacheEntry<TKey, TValue> entry))
                {
                    if (Expired(entry))
                    {
                        _entries.Remove(key);
                        value = default(TValue);
                        return false;
                    }
                    else
                    {
                        entry.LastUsed = DateTime.UtcNow;
                        value = entry.Value;
                        return true;
                    }
                }
                else
                {
                    value = default(TValue);
                    return false;
                }
            }
        }

        private bool Expired(CacheEntry<TKey, TValue> entry)
        {
            return (DateTime.UtcNow > entry.Created.Add(MaxCreationAge)) &&
                   (DateTime.UtcNow > entry.LastUsed.Add(MaxUsedAge));
        }

        public void Purge()
        {
            lock (_entries)
            {
                foreach (var key in _entries.Values
                .Where(Expired).Select(e => e.Key).ToList())
                {
                    _entries.Remove(key);
                }
            }
        }
    }
}

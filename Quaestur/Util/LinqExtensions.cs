using System.Linq;
using System.Collections.Generic;
using Npgsql;

namespace System
{
    public static class LinqExtensions
    {
        public static int MaxOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, int defaultValue)
        {
            if (list.Any())
            {
                return list.Max(selector);
            }
            else
            {
                return defaultValue;
            }
        }

        public static long MaxOrDefault<T>(this IEnumerable<T> list, Func<T, long> selector, long defaultValue)
        {
            if (list.Count() > 0)
            {
                return defaultValue;
            }
            else
            {
                return list.Max(selector);
            }
        }

        public static float MaxOrDefault<T>(this IEnumerable<T> list, Func<T, float> selector, float defaultValue)
        {
            if (list.Count() > 0)
            {
                return defaultValue;
            }
            else
            {
                return list.Max(selector);
            }
        }

        public static double MaxOrDefault<T>(this IEnumerable<T> list, Func<T, double> selector, double defaultValue)
        {
            if (list.Count() > 0)
            {
                return defaultValue;
            }
            else
            {
                return list.Max(selector);
            }
        }

        public static decimal MaxOrDefault<T>(this IEnumerable<T> list, Func<T, decimal> selector, decimal defaultValue)
        {
            if (list.Count() > 0)
            {
                return defaultValue;
            }
            else
            {
                return list.Max(selector);
            }
        }
    }
}

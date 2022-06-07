using System.Linq;
using System.Collections.Generic;

namespace System
{
    public static class LinqExtensions
    {
        public static int SumOrDefault<T>(this IEnumerable<T> list, Func<T, int> selector, int defaultValue)
        {
            if (list.Any())
            {
                return list.Sum(selector);
            }
            else
            {
                return defaultValue;
            }
        }

        public static decimal SumOrDefault<T>(this IEnumerable<T> list, Func<T, decimal> selector, decimal defaultValue)
        {
            if (list.Any())
            {
                return list.Sum(selector);
            }
            else
            {
                return defaultValue;
            }
        }

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
            if (list.Any())
            {
                return list.Max(selector);
            }
            else
            {
                return defaultValue;
            }
        }

        public static float MaxOrDefault<T>(this IEnumerable<T> list, Func<T, float> selector, float defaultValue)
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

        public static double MaxOrDefault<T>(this IEnumerable<T> list, Func<T, double> selector, double defaultValue)
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

        public static decimal MaxOrDefault<T>(this IEnumerable<T> list, Func<T, decimal> selector, decimal defaultValue)
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

        public static DateTime MaxOrDefault<T>(this IEnumerable<T> list, Func<T, DateTime> selector, DateTime defaultValue)
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
    }
}

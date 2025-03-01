﻿using System;
using System.Globalization;

namespace BaseLibrary
{
    public static class Dates
    {
        public static bool TryParseDateTime(this string stringValue, out DateTime value)
        {
            return DateTime.TryParseExact(stringValue,
                new string[] {
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd HH:mm",
                    "dd.MM.yyyy HH:mm:ss",
                    "dd.MM.yyyy HH:mm",
                    "MM/dd/yyyy HH:mm:ss",
                    "MM/dd/yyyy HH:mm"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out value);
        }

        public static bool TryParseDate(this string stringValue, out DateTime value)
        {
            return DateTime.TryParseExact(stringValue,
                new string[] { "yyyy-MM-dd", "dd.MM.yyyy", "MM/dd/yyyy" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal,
                out value);
        }

        public static string FormatSwissDateDay(this DateTime value)
        {
            return value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
        }

        public static string FormatSwissDateMinutes(this DateTime value)
        {
            return value.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        public static string FormatSwissDateSeconds(this DateTime value)
        {
            return value.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static string FormatTimeMinutes(this DateTime value)
        {
            return value.ToString("HH:mm", CultureInfo.InvariantCulture);
        }

        public static string FormatTimeSeconds(this DateTime value)
        {
            return value.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static string FormatIso(this DateTime value)
        {
            return value.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static DateTime ParseIsoDate(this string value)
        {
            return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        }

        public static bool TryParseIsoDate(string value, out DateTime date)
        {
            return DateTime.TryParseExact(value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date);
        }

        public static TimeSpan ComputeOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            // make sure range 1 is shorter than range 2
            if (end1.Subtract(start1).TotalDays > end2.Subtract(start2).TotalDays)
            {
                return ComputeOverlap(start2, end2, start1, end1); 
            }

            // =>  start1 |         end1 |                   
            // =>           start2 |                   end2 |
            if (end1 >= start2 && end1 <= end2 && start1 <= start2)
            {
                return end1.Subtract(start2);
            }
            // =>             start1 |         end1 |        
            // =>           start2 |                   end2 |
            else if (start1 >= start2 && end1 <= end2)
            {
                return end1.Subtract(start1);
            }
            // =>                           start1 |         end1 |
            // =>           start2 |                   end2 |      
            else if (start1 >= start2 && start1 <= end2 && end1 >= end2)
            {
                return end2.Subtract(start1);
            }
            else
            {
                return new TimeSpan(0, 0, 0, 0); 
            }
        }
    }
}

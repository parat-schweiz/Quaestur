using System;
using System.Globalization;

namespace BaseLibrary
{
    public static class FormatterExtensions
    {
        public static string FormatThousands(this long value)
        {
            return string.Format(CultureInfo.GetCultureInfo("de-CH"), "{0:#,##0}", value);
        }

        public static string FormatThousands(this int value)
        {
            return string.Format(CultureInfo.GetCultureInfo("de-CH"), "{0:#,##0}", value);
        }

        public static string FormatMoney(this double value)
        {
            return string.Format(CultureInfo.GetCultureInfo("de-CH"), "{0:#,##0.00}", value);
        }

        public static string FormatMoney(this decimal value)
        {
            return string.Format(CultureInfo.GetCultureInfo("de-CH"), "{0:#,##0.00}", value);
        }

        public static string SizeFormat(this int value)
        {
            return ((long)value).SizeFormat();
        }

        public static string SizeFormat(this long value) 
        {
            if (value >= 1024 * 1024 * 1024)
            {
                return string.Format("{0:0.00} GiB", value / 1024d / 1024d / 1024d);
            }
            else if (value >= 1024 * 1024)
            {
                return string.Format("{0:0.00} MiB", value / 1024d / 1024d);
            }
            else if (value >= 1024)
            {
                return string.Format("{0:0.00} KiB", value / 1024d);
            }
            else
            {
                return string.Format("{0} Bytes", value);
            }
        }
    }
}

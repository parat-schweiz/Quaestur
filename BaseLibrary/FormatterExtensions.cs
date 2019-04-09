using System;

namespace BaseLibrary
{
    public static class FormatterExtensions
    {
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

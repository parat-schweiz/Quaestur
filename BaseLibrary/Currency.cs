using System;
using System.IO;
using System.Text;
using System.Linq;

namespace BaseLibrary
{
    public static class Currency
    {
        public static string Format(decimal value)
        {
            var preFormat = string.Format("{0:0.00}", Math.Round(value, 2));
            var parts = preFormat.Split(new string[] { "." }, StringSplitOptions.None);
            var before = parts[0];
            var after = string.Empty;
            int counter = 0;

            foreach (char c in before.ToCharArray().Reverse())
            {
                if (counter > 0 && counter % 3 == 0)
                {
                    after = "'" + after; 
                }

                after = c.ToString() + after;
                counter++;
            }

            return after + "." + parts[1];
        }
    }
}


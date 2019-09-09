using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DiscourseEngagement
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                throw new ArgumentException("Config file argument missing");
            }

            var master = new EngagementMaster(args[0]);
            master.Run();
        }
    }
}

using System;
using System.IO;
using Nancy.Hosting.Self;

namespace SecurityService
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            if ((args.Length < 1) ||
                (!File.Exists(args[0])))
            {
                throw new FileNotFoundException("Config file not found");
            }

            Global.Config.Load(args[0]);

            Global.Log.Info("Starting Security Service on " + Global.Config.BindAddress);

            // initialize an instance of NancyHost
            var host = new NancyHost(new Uri(Global.Config.BindAddress));
            host.Start();  // start hosting

            Global.Log.Info("Application started");

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}

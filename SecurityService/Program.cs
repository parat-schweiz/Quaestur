using System;
using System.Linq;
using Nancy.Hosting.Self;

namespace SecurityService
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            Global.Config.Load(args[0]);

            Global.Log.Notice("Starting Security Service on " + Global.Config.BindAddress);

            // initialize an instance of NancyHost
            var host = new NancyHost(new Uri(Global.Config.BindAddress));
            host.Start();  // start hosting

            Global.Log.Notice("Application started");

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}

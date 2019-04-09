using System;
using System.Linq;
using Nancy.Hosting.Self;

namespace SecurityService
{
    public static class MainClass
    {
        public static void Main(string[] args)
        {
            var uri = "http://localhost:8890";
            Global.Log.Notice("Starting Security Service on " + uri);

            // initialize an instance of NancyHost
            var host = new NancyHost(new Uri(uri));
            host.Start();  // start hosting

            Global.Log.Notice("Application started");

            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}

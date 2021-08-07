using System;
using System.IO;
using Nancy.Hosting.Self;

namespace Census
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

            using (var db = Global.CreateDatabase())
            {
                Model.Install(db);
            }

            var uri = "http://localhost:8893";
            Global.Log.Info("Starting Census on " + uri);

            // initialize an instance of NancyHost
            var host = new NancyHost(new Uri(uri));
            host.Start();  // start hosting

            Global.Log.Info("Application started");
            var runner = new TaskRunner();

            while (true)
            {
                runner.Run();
                System.Threading.Thread.Sleep(1000);
            }
        }
    }
}

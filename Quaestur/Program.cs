using System;
using System.IO;
using Nancy.Hosting.Self;

namespace Quaestur
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
            Global.Log.ConsoleSeverity = BaseLibrary.LogSeverity.Info;
            Global.Log.FileSeverity = BaseLibrary.LogSeverity.Verbose;
            Global.Log.BufferSeverity = BaseLibrary.LogSeverity.Notice;

            using (var db = Global.CreateDatabase())
            {
                Model.Install(db);
                var seeder = new Seeder(db);
                seeder.MinimalSeed();
            }

            var uri = "http://localhost:8888";
            Global.Log.Info("Starting Quaestur on " + uri);

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

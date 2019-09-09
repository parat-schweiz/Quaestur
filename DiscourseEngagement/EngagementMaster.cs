using System;
using System.IO;
using BaseLibrary;
using SiteLibrary;
using QuaesturApi;

namespace DiscourseEngagement
{
    public class EngagementMaster
    {
        private readonly EngagementConfig _config;
        private readonly Logger _logger;
        private readonly Quaestur _quaestur;

        public EngagementMaster(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Config file not found");
            }

            _config = new EngagementConfig();
            _config.Load(filename);

            _logger = new Logger(_config.LogFilePrefix);
            _logger.Notice("Discourse Engagement started");

            _quaestur = new Quaestur(_config.QuaesturApi);
        }

        public void Run()
        {
            System.Threading.Thread.Sleep(3000);

            foreach (var person in _quaestur.GetPersonList())
            {
                Console.WriteLine(person.Id.ToString() + " " + person.Username); 
            }
        }
    }
}

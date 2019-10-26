using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BaseLibrary;
using SiteLibrary;
using QuaesturApi;
using RedmineApi;

namespace RedmineEngagement
{
    public class EngagementMaster
    {
        private readonly EngagementConfig _config;
        private readonly Logger _logger;
        private readonly Quaestur _quaestur;
        private readonly Redmine _redmine;
        private readonly IDatabase _database;

        public EngagementMaster(string filename)
        {
            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Config file not found");
            }

            _config = new EngagementConfig();
            _config.Load(filename);

            _logger = new Logger(_config.LogFilePrefix);
            _logger.Notice("Redmine Engagement started");

            //_database = new PostgresDatabase(_config.Database);
            //Model.Install(_database, _logger);

            _quaestur = new Quaestur(_config.QuaesturApi);
            _redmine = new Redmine(_config.RedmineApi);
        }

        public void Run()
        {
            while (true)
            {
                Sync();
                System.Threading.Thread.Sleep(60 * 1000);
            }
        }

        private void Sync()
        {
            _redmine.GetUsers().ToList();
        }
    }
}

using System;
using System.IO;
using System.Linq;
using BaseLibrary;
using SiteLibrary;
using QuaesturApi;
using DiscourseApi;

namespace DiscourseEngagement
{
    public class EngagementMaster
    {
        private readonly EngagementConfig _config;
        private readonly Logger _logger;
        private readonly Quaestur _quaestur;
        private readonly Discourse _discourse;
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
            _logger.Notice("Discourse Engagement started");

            _database = new PostgresDatabase(_config.Database);
            Model.Install(_database, _logger);

            _quaestur = new Quaestur(_config.QuaesturApi);
            _discourse = new Discourse(_config.DiscourseApi);
        }

        public void Run()
        {
            System.Threading.Thread.Sleep(2000);

            while (true)
            {
                Sync();
                System.Threading.Thread.Sleep(60 * 1000);
            }
        }

        private void Sync()
        {
            SyncUsers();
        }

        private void SyncUsers()
        {
            var databasePersons = _database.Query<Person>();
            var quaesturPersons = _quaestur.GetPersonList().ToList();

            foreach (var user in _discourse.GetUsers().ToList())
            { 
                if (user.Auid.HasValue)
                {
                    if (quaesturPersons.Any(p => p.Id.Equals(user.Auid.Value)))
                    {
                        var person = databasePersons.SingleOrDefault(p => p.Id.Equals(user.Auid.Value));

                        if (person == null)
                        {
                            person = new Person(user.Auid.Value);
                            person.DiscourseUserId.Value = user.Id;
                            _database.Save(person);
                            _logger.Notice("Added user {0}, id {1}, {2}", user.Username, user.Auid.Value, user.Id);
                        }
                        else if (person.DiscourseUserId.Value != user.Id)
                        {
                            person.DiscourseUserId.Value = user.Id;
                            _database.Save(person);
                            _logger.Notice("Updated user {0}, id {1}, {2}", user.Username, user.Auid.Value, user.Id);
                        }
                    }
                    else
                    {
                        var person = databasePersons.SingleOrDefault(p => p.Id.Equals(user.Auid.Value));

                        if (person != null)
                        {
                            person.Delete(_database);
                            _logger.Notice("Dropping user {0}, id {1}, {2} because not in Quaestur", user.Username, user.Auid.Value, user.Id);
                        }
                        else
                        {
                            _logger.Notice("Not adding user {0}, id {1}, {2} because not in Quaestur", user.Username, user.Auid.Value, user.Id);
                        }
                    }
                }
                else
                {
                    _logger.Notice("Not adding user {0}, id {1} because no AUID", user.Username, user.Id);
                }
            }
        }

        private void SyncTopics()
        {
            foreach (var topic in _discourse.GetTopics())
            {
                Console.WriteLine(topic.Id + " " + topic.Title);
                if (topic.LikeCount > 0)
                {
                    var topic2 = _discourse.GetTopic(topic.Id);
                    foreach (var post in topic2.Posts)
                    {
                        if (post.LikeCount > 0)
                        {
                            var likes = _discourse.GetLikes(post.Id);
                            foreach (var like in likes)
                            {
                                Console.WriteLine(topic2.Id.ToString() + " " + post.Id.ToString() + " " + like);
                            }
                        }
                    }
                }
            }

            foreach (var person in _quaestur.GetPersonList())
            {
                Console.WriteLine(person.Id.ToString() + " " + person.Username); 
            }
        }
    }
}

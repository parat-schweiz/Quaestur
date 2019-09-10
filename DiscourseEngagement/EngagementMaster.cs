using System;
using System.IO;
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
            _discourse = new Discourse(_config.DiscourseApi);
        }

        public void Run()
        {
            System.Threading.Thread.Sleep(2000);

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

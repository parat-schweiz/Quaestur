using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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
            SyncContent();
            DoAwards();
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
                            person.UserId.Value = user.Id;
                            _database.Save(person);
                            _logger.Notice("Added user {0}, id {1}, {2}", user.Username, user.Auid.Value, user.Id);
                        }
                        else if (person.UserId.Value != user.Id)
                        {
                            person.UserId.Value = user.Id;
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

        private void SyncContent()
        {
            using (var transaction = _database.BeginTransaction())
            {
                var cache = new Cache(_database);
                cache.Reload();

                foreach (var apiTopic in _discourse.GetTopics().ToList())
                {
                    var dbTopic = cache.GetTopic(apiTopic.Id);

                    if (dbTopic == null)
                    {
                        _logger.Notice("New topic {0}", apiTopic.Id);
                        dbTopic = new Topic(Guid.NewGuid());
                        dbTopic.TopicId.Value = apiTopic.Id;
                        dbTopic.PostsCount.Value = apiTopic.PostsCount;
                        dbTopic.LikeCount.Value = apiTopic.LikeCount;
                        cache.Add(dbTopic);
                        _database.Save(dbTopic);
                        SyncTopic(cache, apiTopic, dbTopic);
                    }
                    else if (dbTopic.PostsCount.Value != apiTopic.PostsCount ||
                             dbTopic.LikeCount.Value != apiTopic.LikeCount)
                    {
                        _logger.Notice("Updated topic {0}", apiTopic.Id);
                        dbTopic.PostsCount.Value = apiTopic.PostsCount;
                        dbTopic.LikeCount.Value = apiTopic.LikeCount;
                        _database.Save(dbTopic);
                        SyncTopic(cache, apiTopic, dbTopic);
                    }
                }

                transaction.Commit();
            }
        }

        private void SyncTopic(Cache cache, DiscourseApi.Topic apiTopic, Topic dbTopic)
        {
            apiTopic = _discourse.GetTopic(apiTopic.Id);

            foreach (var apiPost in apiTopic.Posts)
            {
                var dbPost = cache.GetPost(apiTopic.Id, apiPost.Id);

                if (dbPost == null)
                {
                    _logger.Notice("New post {0}", apiPost.Id);
                    dbPost = new Post(Guid.NewGuid());
                    dbPost.Topic.Value = cache.GetTopic(apiTopic.Id);
                    dbPost.Person.Value = cache.GetPerson(apiPost.UserId);
                    dbPost.PostId.Value = apiPost.Id;
                    dbPost.Created.Value = apiPost.CreatedAt;
                    dbPost.AwardDecision.Value = AwardDecision.None;
                    cache.Add(dbPost);
                    _database.Save(dbPost);
                }
            }
        }

        private void DoAwards()
        {
            var budget = _quaestur.GetPointBudgetList().Single(b => b.Label[QuaesturApi.Language.English] == "Aktionen");

            using (var transaction = _database.BeginTransaction())
            {
                var cache = new Cache(_database);
                cache.Reload();

                var latest = new Dictionary<Guid, DateTime>();
                foreach (var person in cache.Persons)
                    latest.Add(person.Id.Value, new DateTime(1850, 1, 1));

                foreach (var post in cache.Posts.OrderBy(p => p.Created.Value))
                { 
                    if (post.AwardDecision.Value == AwardDecision.None)
                    { 
                        if (post.Person.Value == null)
                        {
                            post.AwardDecision.Value = AwardDecision.Negative;
                            _database.Save(post);
                            _logger.Notice(
                                "Not warding for {0}.{1} because no person assigned",
                                post.Topic.Value.TopicId.Value,
                                post.PostId.Value);
                        }
                        else
                        {
                            var backoff = post.Created.Value
                                .Subtract(latest[post.Person.Value.Id.Value]);
                            var backoffFactor = BackoffFactor(backoff);
                            var conversationFactor = ConversationFactor(cache, post);
                            var points = (int)Math.Floor(100d * backoffFactor * conversationFactor);
                            var reason = string.Format(
                                "Backoff {0:0.0}%, conversation {1:0.0}%",
                                backoffFactor * 100, conversationFactor * 100);
                            post.AwardedPoints.Value = points;
                            post.AwardedCalculation.Value = reason;

                            if (points > 0)
                            {
                                var result = _quaestur.AddPoints(post.Person.Value.Id, budget.Id, points, reason, DateTime.UtcNow, PointsReferenceType.None, Guid.Empty);
                                post.AwardDecision.Value = AwardDecision.Positive;
                                post.AwardedPointsId.Value = result.Id;
                                post.AwardedPoints.Value = points;
                                _logger.Notice(
                                   "Awarding {0} for {1}.{2}. Reason: {3}",
                                   points,
                                   post.Topic.Value.TopicId.Value,
                                   post.PostId.Value,
                                   reason);
                            }
                            else
                            {
                                post.AwardDecision.Value = AwardDecision.Negative;
                                _logger.Notice(
                                   "Not warding for {0}.{1} because result 0 points. Reason {2}",
                                   post.Topic.Value.TopicId.Value,
                                   post.PostId.Value,
                                   reason);
                            }

                            _database.Save(post);
                        }
                    }

                    if (post.Person.Value != null)
                        latest[post.Person.Value.Id.Value] = post.Created.Value;
                }

                transaction.Commit();
            }
        }

        private double ConversationFactor(Cache cache, Post post)
        {
            var thread = new Queue<Post>(cache.Posts
                .Where(p => p.Topic.Value == post.Topic.Value)
                .Where(p => p.PostId < post.PostId)
                .OrderByDescending(p => p.PostId));
            var participants = new List<Guid>();
            participants.Add(post.Person.Value.Id.Value);
            var addon = new Queue<double>(ConversationAddon);
            var factor = addon.Dequeue();

            while (thread.Count > 0 && addon.Count < 0)
            {
                var p = thread.Dequeue(); 

                if (p.Person.Value != null &&
                    !participants.Contains(p.Person.Value.Id.Value))
                {
                    participants.Add(p.Person.Value.Id.Value);
                    factor += addon.Dequeue();
                }
            }

            return factor;
        }

        private IEnumerable<double> ConversationAddon
        {
            get
            {
                yield return 0.4d;
                yield return 0.3d;
                yield return 0.2d;
                yield return 0.1d;
            }
        }

        private double BackoffFactor(TimeSpan backoff)
        {
            var factor = 0d;

            foreach (var level in Levels)
            {
                var length = new TimeSpan(Math.Min(backoff.Ticks, level.Item1.Ticks));
                var add = level.Item2 / level.Item1.TotalSeconds * length.TotalSeconds;
                factor += add;
                backoff = backoff.Subtract(length);
            }

            return factor;
        }

        private IEnumerable<Tuple<TimeSpan, double>> Levels
        {
            get
            {
                yield return new Tuple<TimeSpan, double>(new TimeSpan(0, 0, 1, 0), 0d);
                yield return new Tuple<TimeSpan, double>(new TimeSpan(0, 0, 1, 0), 0.03d);
                yield return new Tuple<TimeSpan, double>(new TimeSpan(0, 0, 58, 0), 0.27d);
                yield return new Tuple<TimeSpan, double>(new TimeSpan(0, 23, 0, 0), 0.6d);
                yield return new Tuple<TimeSpan, double>(new TimeSpan(6, 0, 0, 0), 0.1d);
            }
        }
    }
}

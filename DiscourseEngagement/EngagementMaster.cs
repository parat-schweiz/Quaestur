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
                    var questurPerson = quaesturPersons.SingleOrDefault(p => p.Id.Equals(user.Auid.Value));

                    if (questurPerson != null)
                    {
                        var person = databasePersons.SingleOrDefault(p => p.Id.Equals(user.Auid.Value));

                        if (person == null)
                        {
                            person = new Person(user.Auid.Value);
                            person.UserId.Value = user.Id;
                            person.UserName.Value = user.Username;
                            person.Language.Value = SiteLibrary.Language.English;
                            _database.Save(person);
                            _logger.Notice("Added user {0}, id {1}, {2}", user.Username, user.Auid.Value, user.Id);
                        }
                        else if (person.UserId.Value != user.Id)
                        {
                            person.UserId.Value = user.Id;
                            person.UserName.Value = user.Username;
                            person.Language.Value = SiteLibrary.Language.English;
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
                    dbPost.LikeCount.Value = apiPost.LikeCount;
                    cache.Add(dbPost);
                    _database.Save(dbPost);
                    SyncLikes(cache, apiPost, dbPost);
                }
                else if (dbPost.LikeCount < apiPost.LikeCount)
                {
                    dbPost.LikeCount.Value = apiPost.LikeCount;
                    _database.Save(dbPost);
                    SyncLikes(cache, apiPost, dbPost);
                }
            }
        }

        private void SyncLikes(Cache cache, DiscourseApi.Post apiPost, Post dbPost)
        {
            var likes = _discourse.GetLikes(apiPost.Id);

            foreach (var userId in likes)
            {
                var dbLike = cache.GetLike(apiPost.Id, userId);

                if (dbLike == null)
                {
                    dbLike = new Like(Guid.NewGuid());
                    dbLike.Person.Value = cache.GetPerson(userId);
                    dbLike.Post.Value = dbPost;
                    dbLike.Created.Value = DateTime.Now;
                    dbLike.AwardDecision.Value = AwardDecision.None;
                    cache.Add(dbLike);
                    _database.Save(dbLike);
                }
            }
        }

        private void DoAwards()
        {
            var budget = _quaestur.GetPointBudgetList().Single(b => b.Label[QuaesturApi.Language.English] == "Aktionen");

            using (var transaction = _database.BeginTransaction())
            {
                var translation = new Translation(_database);
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
                            var reason = translation.Get(
                                post.Person.Value.Language.Value,
                                "Award.Post.Reason",
                                "When a person posts on discourse",
                                "Posting in discourse, backoff factor {0:0.0}%, conversation factor {1:0.0}%",
                                backoffFactor * 100, 
                                conversationFactor * 100);
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

                foreach (var like in cache.Likes)
                {
                    if (like.AwardDecision.Value == AwardDecision.None)
                    {
                        if (like.Person.Value == null)
                        {
                            like.AwardDecision.Value = AwardDecision.Negative;
                            _database.Save(like);
                            _logger.Notice(
                                "Not warding for {0}.{1}.{2} because no person assigned",
                                like.Post.Value.Topic.Value.TopicId.Value,
                                like.Post.Value.PostId,
                                like.Person.Value.UserId);
                        }
                        else
                        {
                            like.AwardDecision.Value = AwardDecision.Positive;
                            like.AwardedFromPoints.Value = 10;
                            like.AwardedFromCalculation.Value = translation.Get(
                                like.Person.Value.Language.Value,
                                "Award.Like.From.Reason",
                                "When the person gives a like in discourse",
                                "Giving a like in discourse");
                            like.AwardedToPoints.Value = 20;
                            like.AwardedToCalculation.Value = translation.Get(
                                like.Post.Value.Person.Value.Language.Value,
                                "Award.Like.To.Reason",
                                "When the person recieves a like in discourse",
                                "Receiving a like in discourse"); 
                            var resultFrom = _quaestur.AddPoints(
                                like.Person.Value.Id, 
                                budget.Id,
                                like.AwardedFromPoints.Value.Value, 
                                like.AwardedFromCalculation.Value, 
                                DateTime.UtcNow, 
                                PointsReferenceType.None, 
                                Guid.Empty);
                            var resultTo = _quaestur.AddPoints(
                                like.Post.Value.Person.Value.Id, 
                                budget.Id,
                                like.AwardedToPoints.Value.Value,
                                like.AwardedToCalculation.Value,
                                DateTime.UtcNow,
                                PointsReferenceType.None,
                                Guid.Empty);
                            like.AwardedFromPointsId.Value = resultFrom.Id;
                            like.AwardedToPointsId.Value = resultTo.Id;
                            _database.Save(like);
                            _logger.Notice(
                                "Awarding {0}/{1} for like {2}.{3}.{4}",
                                like.AwardedFromPoints.Value,
                                like.AwardedToPoints.Value,
                                like.Post.Value.Topic.Value.TopicId.Value,
                                like.Post.Value.PostId,
                                like.Person.Value.UserId);
                        }
                    }
                }

                transaction.Commit();
            }
        }

        private double ConversationFactor(Cache cache, Post post)
        {
            var thread = new Queue<Post>(cache.Posts
                .Where(p => p.Topic.Value == post.Topic.Value)
                .Where(p => p.PostId.Value < post.PostId.Value)
                .OrderByDescending(p => p.PostId.Value));
            var participants = new List<Guid>();
            var addon = new Queue<double>(ConversationAddon);
            var factor = addon.Dequeue();

            while (thread.Count > 0)
            {
                var p = thread.Dequeue();

                if (p.Person.Value != null)
                {
                    if (p.Person.Value.Id.Equals(post.Person.Value.Id))
                    {
                        participants = new List<Guid>();
                        addon = new Queue<double>(ConversationAddon);
                        factor = addon.Dequeue();
                    }
                    else if (!participants.Contains(p.Person.Value.Id.Value) &&
                             addon.Count > 0)
                    {
                        participants.Add(p.Person.Value.Id.Value);
                        factor += addon.Dequeue();
                    }
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

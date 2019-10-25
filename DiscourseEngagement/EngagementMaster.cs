﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        private SiteLibrary.Language Convert(QuaesturApi.Language language)
        {
            switch (language)
            {
                case QuaesturApi.Language.German:
                    return SiteLibrary.Language.German;
                case QuaesturApi.Language.French:
                    return SiteLibrary.Language.French;
                case QuaesturApi.Language.Italian:
                    return SiteLibrary.Language.Italian;
                default:
                    return SiteLibrary.Language.English;
            } 
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
                            person.QuaesturUserName.Value = questurPerson.Username;
                            person.DiscourseUserName.Value = user.Username;
                            person.Language.Value = Convert(questurPerson.Language);
                            _database.Save(person);
                            _logger.Notice("Added user {0}, id {1}, {2}", user.Username, user.Auid.Value, user.Id);
                        }
                        else if (person.UserId.Value != user.Id)
                        {
                            person.UserId.Value = user.Id;
                            person.QuaesturUserName.Value = questurPerson.Username;
                            person.DiscourseUserName.Value = user.Username;
                            person.Language.Value = Convert(questurPerson.Language);
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

                foreach (var apiCategory in _discourse.GetCategories().ToList())
                {
                    foreach (var apiTopic in _discourse.GetTopics(apiCategory).ToList())
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
                    CheckInstructionPost(cache, apiPost, dbPost);
                }
                else if (dbPost.LikeCount < apiPost.LikeCount)
                {
                    dbPost.LikeCount.Value = apiPost.LikeCount;
                    _database.Save(dbPost);
                    SyncLikes(cache, apiPost, dbPost);
                }
            }
        }

        private Match MultiMatch(string text)
        {
            var patterns = new string[]
            {
                "^([0-9]+) Punkte für @([a-zA-Z0-9_\\-]+) von Budget “([a-zA-Z0-9 ]+)”$",
                "^([0-9]+) points for @([a-zA-Z0-9_\\-]+) from budget “([a-zA-Z0-9 ]+)”$",
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(text, pattern);
                if (match.Success) return match;
            }

            return null;
        }

        private void CheckInstructionPost(Cache cache, DiscourseApi.Post apiPost, Post dbPost)
        {
            var text = Regex.Replace(apiPost.Text, "<.+?>", string.Empty);
            var lines = text
                .Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            var reason = lines.First().Trim();
            var match = MultiMatch(lines.Last().Trim());

            if (match != null && match.Success)
            {
                var points = int.Parse(match.Groups[1].Value);
                var username = match.Groups[2].Value;
                var budgetLabel = match.Groups[3].Value;
                var url = _config.DiscourseApi.ApiUrl + string.Format("/t/{0}/{1}", apiPost.TopicId, apiPost.Id);
                var owner = cache.Persons.SingleOrDefault(p => p.DiscourseUserName.Value == username);
                var budget = _quaestur.GetPointBudgetList().SingleOrDefault(b => b.Label.IsAny(budgetLabel));
                var translation = new Translation(_database);
                var translator = new Translator(translation, dbPost.Person.Value.Language.Value);
                var quote = string.Format(
                    "[quote=\"{0}, post:{1}, topic:{2}, full:true\"]\n{3}\n[/quote]",
                    apiPost.Username, apiPost.Id, apiPost.TopicId, text);

                if (owner == null)
                {
                    var response = translator.Get(
                        "Award.Assign.Error.UserUnkown",
                        "When the owner is unknown when assigning points through post",
                        "Cannot determine mentioned user.");
                    var postText = quote + Environment.NewLine + Environment.NewLine + response;
                    _discourse.Post(apiPost.TopicId, postText);
                    _logger.Notice(
                       "Cannot find user at assignment in {0}.{1}",
                       dbPost.Topic.Value.TopicId.Value,
                       dbPost.PostId.Value);
                }
                else if (budget == null)
                {
                    var response = translator.Get(
                        "Award.Assign.Error.BudgetUnkown",
                        "When the points budget is unknown when assigning points through post",
                        "Cannot determine mentioned points budget.");
                    var postText = quote + Environment.NewLine + Environment.NewLine + response;
                    _discourse.Post(apiPost.TopicId, postText);
                    _logger.Notice(
                       "Cannot find points budget at assignment in {0}.{1}",
                       dbPost.Topic.Value.TopicId.Value,
                       dbPost.PostId.Value);
                }
                else
                {
                    try
                    {
                        var result = _quaestur.AddPoints(
                            owner.Id.Value,
                            budget.Id,
                            points,
                            reason,
                            url,
                            apiPost.CreatedAt,
                            PointsReferenceType.None,
                            Guid.Empty,
                            dbPost.Person.Value.Id.Value);
                        var response = translator.Get(
                            "Award.Assign.Assigned",
                            "When points are assigned through post",
                            "Assigned points.");
                        var postText = quote + Environment.NewLine + Environment.NewLine + response;
                        _discourse.Post(apiPost.TopicId, postText);
                        _logger.Notice(
                           "Awarding {0} to {1} because assignment in {2}.{3}",
                           points,
                           owner.UserId,
                           dbPost.Topic.Value.TopicId.Value,
                           dbPost.PostId.Value,
                           reason);
                    }
                    catch (ApiAccessDeniedException)
                    {
                        var response = translator.Get(
                            "Award.Assign.Error.Denied",
                            "When the access is denied when assigning points through post",
                            "Not permitted to assign points.");
                        var postText = quote + Environment.NewLine + Environment.NewLine + response;
                        _discourse.Post(apiPost.TopicId, postText);
                        _logger.Notice(
                           "Access denied at assignment in {0}.{1}",
                           dbPost.Topic.Value.TopicId.Value,
                           dbPost.PostId.Value);
                    }
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
            var budget = _quaestur.GetPointBudgetList()
                .Single(b => b.Label.IsAny(_config.DiscourseBudgetLabel));

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
                            var points = (int)Math.Floor((double)_config.PostPoints * backoffFactor * conversationFactor);
                            var reason = translation.Get(
                                post.Person.Value.Language.Value,
                                "Award.Post.Reason",
                                "When a person posts on discourse",
                                "Posting in discourse, backoff factor {0:0.0}%, conversation factor {1:0.0}%",
                                backoffFactor * 100, 
                                conversationFactor * 100);
                            var url = _config.DiscourseApi.ApiUrl + string.Format("/t/{0}/{1}", post.Topic.Value.TopicId, post.PostId);
                            post.AwardedPoints.Value = points;
                            post.AwardedCalculation.Value = reason;

                            if (points > 0)
                            {
                                var result = _quaestur.AddPoints(
                                    post.Person.Value.Id, 
                                    budget.Id, 
                                    points, 
                                    reason, 
                                    url, 
                                    DateTime.UtcNow, 
                                    PointsReferenceType.None, 
                                    Guid.Empty, 
                                    null);
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
                            like.AwardedFromPoints.Value = _config.LikeGivePoints;
                            like.AwardedFromCalculation.Value = translation.Get(
                                like.Person.Value.Language.Value,
                                "Award.Like.From.Reason",
                                "When the person gives a like in discourse",
                                "Giving a like in discourse");
                            like.AwardedToPoints.Value = _config.LikeRecievePoints;
                            like.AwardedToCalculation.Value = translation.Get(
                                like.Post.Value.Person.Value.Language.Value,
                                "Award.Like.To.Reason",
                                "When the person recieves a like in discourse",
                                "Receiving a like in discourse");
                            var url = _config.DiscourseApi.ApiUrl + string.Format("/t/{0}/{1}", like.Post.Value.Topic.Value.TopicId, like.Post.Value.PostId);
                            var resultFrom = _quaestur.AddPoints(
                                like.Person.Value.Id, 
                                budget.Id,
                                like.AwardedFromPoints.Value.Value, 
                                like.AwardedFromCalculation.Value, 
                                url,
                                DateTime.UtcNow, 
                                PointsReferenceType.None, 
                                Guid.Empty,
                                null);
                            var resultTo = _quaestur.AddPoints(
                                like.Post.Value.Person.Value.Id, 
                                budget.Id,
                                like.AwardedToPoints.Value.Value,
                                like.AwardedToCalculation.Value,
                                url,
                                DateTime.UtcNow,
                                PointsReferenceType.None,
                                Guid.Empty,
                                null);
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

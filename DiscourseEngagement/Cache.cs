using System;
using System.Linq;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace DiscourseEngagement
{
    public class Cache
    {
        private readonly IDatabase _database;
        private readonly Dictionary<int, Topic> _topics;
        private readonly Dictionary<int, Person> _persons;
        private readonly Dictionary<string, Post> _posts;

        public Cache(IDatabase database)
        {
            _database = database;
            _topics = new Dictionary<int, Topic>();
            _persons = new Dictionary<int, Person>();
            _posts = new Dictionary<string, Post>();
        }

        public void Add(Person person)
        {
            if (!_persons.ContainsKey(person.UserId.Value))
                _persons.Add(person.UserId.Value, person);
        }

        public void Add(Topic topic)
        {
            if (!_topics.ContainsKey(topic.TopicId.Value))
                _topics.Add(topic.TopicId.Value, topic);
        }

        public void Add(Post post)
        {
            Add(post.Topic.Value);
            if (post.Person.Value != null)
                Add(post.Person.Value);
            var key = GetPostKey(post);
            if (!_posts.ContainsKey(key))
                _posts.Add(key, post); 
        }

        private string GetPostKey(Post post)
        {
            return GetPostKey(post.Topic.Value.TopicId.Value, post.PostId.Value);
        }

        private string GetPostKey(int topicId, int postId)
        {
            return topicId.ToString() + "." + postId.ToString(); 
        }

        public void Reload()
        {
            _topics.Clear();
            _persons.Clear();
            _posts.Clear();

            foreach(var person in _database.Query<Person>())
            {
                Add(person); 
            }

            foreach (var topic in _database.Query<Topic>())
            {
                Add(topic); 
            }

            foreach (var post in _database.Query<Post>())
            {
                Add(post);
            }
        }

        public bool ContainsPerson(int userId)
        {
            return _persons.ContainsKey(userId);
        }

        public Person GetPerson(int userId)
        {
            return ContainsPerson(userId) ? _persons[userId] : null;
        }

        public bool ContainsTopic(int topicId)
        {
            return _topics.ContainsKey(topicId); 
        }

        public Topic GetTopic(int topicId)
        {
            return ContainsTopic(topicId) ? _topics[topicId] : null;
        }

        public bool ContainsPosts(int topicId, int postId)
        {
            return _posts.ContainsKey(GetPostKey(topicId, postId)); 
        }

        public Post GetPost(int topicId, int postId)
        {
            return ContainsPosts(topicId, postId) ? _posts[GetPostKey(topicId, postId)] : null;
        }

        public IEnumerable<Person> Persons
        {
            get { return _persons.Values; }
        }

        public IEnumerable<Topic> Topics
        {
            get { return _topics.Values; }
        }

        public IEnumerable<Post> Posts
        {
            get { return _posts.Values; }
        }

        public IEnumerable<Post> GetPostsByPerson(int userId)
        {
            return GetPostsByPerson(GetPerson(userId));
        }

        public IEnumerable<Post> GetPostsByPerson(Person person)
        {
            return _posts.Where(p => p.Value.Person.Value == person).Select(p => p.Value); 
        }
    }
}

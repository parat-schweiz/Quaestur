using System;
using System.Collections.Generic;
using SiteLibrary;
using BaseLibrary;

namespace RedmineEngagement
{
    public class Cache
    {
        private readonly IDatabase _database;
        private readonly Dictionary<string, Person> _personByName;
        private readonly Dictionary<int, Person> _personById;
        private readonly Dictionary<int, Issue> _issueById;

        public Cache(IDatabase database)
        {
            _database = database;
            _personByName = new Dictionary<string, Person>();
            _personById = new Dictionary<int, Person>();
            _issueById = new Dictionary<int, Issue>();
        }

        public void Reload()
        {
            _personById.Clear();
            _personByName.Clear();
            _issueById.Clear();

            foreach (var person in _database.Query<Person>())
            {
                Add(person); 
            }

            foreach (var issue in _database.Query<Issue>())
            {
                Add(issue); 
            }
        }

        public void Add(Issue issue)
        {
            if (!_issueById.ContainsKey(issue.IssueId))
            {
                _issueById.Add(issue.IssueId, issue); 
            } 
        }

        public void Add(Person person)
        {
            if (!_personByName.ContainsKey(person.UserName))
            {
                _personByName.Add(person.UserName, person); 
            }

            if (!_personById.ContainsKey(person.UserId))
            {
                _personById.Add(person.UserId, person);
            }
        }

        public Person GetPerson(int userId)
        {
            if (_personById.ContainsKey(userId))
            {
                return _personById[userId];
            }
            else
            {
                return null; 
            } 
        }

        public Person GetPerson(string userName)
        {
            if (_personByName.ContainsKey(userName))
            {
                return _personByName[userName];
            }
            else
            {
                return null;
            }
        }

        public Issue GetIssue(int issueId)
        {
            if (_issueById.ContainsKey(issueId))
            {
                return _issueById[issueId];
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<Person> Person
        {
            get { return _personById.Values; }
        }
    }
}

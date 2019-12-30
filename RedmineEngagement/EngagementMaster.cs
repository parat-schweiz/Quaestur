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
        private readonly Cache _cache;

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

            _database = new PostgresDatabase(_config.Database);
            Model.Install(_database, _logger);
            _cache = new Cache(_database);

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
            SyncUsers();
            SyncIssues();
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
            using (var transaction = _database.BeginTransaction())
            {
                _cache.Reload();

                var users = _redmine.GetUsers().ToList();
                var persons = _quaestur.GetPersonList().ToList();

                foreach (var user in users)
                {
                    var person = persons.SingleOrDefault(p => p.Username == user.Username);

                    if (person != null)
                    {
                        var dbPerson = _cache.GetPerson(user.Username);

                        if (dbPerson != null)
                        {
                            dbPerson.Language.Value = Convert(person.Language);
                            _database.Save(dbPerson);
                        }
                        else
                        {
                            dbPerson = new Person(person.Id);
                            dbPerson.UserId.Value = user.Id;
                            dbPerson.UserName.Value = user.Username;
                            dbPerson.Language.Value = Convert(person.Language);
                            _database.Save(dbPerson);
                        }
                    }
                }

                transaction.Commit();
            }
        }

        private void SyncIssues()
        {
            using (var transaction = _database.BeginTransaction())
            {
                _cache.Reload();

                var apiIssues = _redmine.GetIssues().ToList();

                foreach (var apiIssue in apiIssues)
                {
                    var dbIssue = _cache.GetIssue(apiIssue.Id);

                    if (dbIssue != null)
                    {
                        if (dbIssue.UpdatedOn.Value != apiIssue.UpdatedOn)
                        {
                            dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
                            _database.Save(dbIssue);
                            _logger.Info(
                                "Updated issue {0}",
                                apiIssue.Id);
                            SyncIssue(dbIssue, apiIssue);
                        }
                    }
                    else
                    {
                        dbIssue = new Issue(Guid.NewGuid());
                        dbIssue.IssueId.Value = apiIssue.Id;
                        dbIssue.CreatedOn.Value = apiIssue.CreatedOn;
                        dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
                        _database.Save(dbIssue);
                        _logger.Info(
                            "New issue {0}",
                            apiIssue.Id);
                        SyncIssue(dbIssue, apiIssue);
                    } 
                }

                transaction.Commit();
            }
        }

        private void SyncIssue(Issue dbIssue, RedmineApi.Issue apiIssue)
        {
            foreach (var assignmentConfig in _config.Assignments)
            {
                CheckAssignment(dbIssue, apiIssue, assignmentConfig);
            }
        }

        private void CheckAssignment(Issue dbIssue, RedmineApi.Issue apiIssue, AssignmentConfig assignmentConfig)
        {
            if (dbIssue.Assignments.Any(a => a.ConfigId == assignmentConfig.Id))
            {
                return;
            }

            var assign = true;
            assign &= string.IsNullOrEmpty(assignmentConfig.Project) ||
                      assignmentConfig.Project == apiIssue.Project.Name;
            assign &= string.IsNullOrEmpty(assignmentConfig.Tracker) ||
                      assignmentConfig.Tracker == apiIssue.Tracker.Name;
            assign &= string.IsNullOrEmpty(assignmentConfig.Status) ||
                      assignmentConfig.Status == apiIssue.Status.Name;
            assign &= string.IsNullOrEmpty(assignmentConfig.Category) ||
                      assignmentConfig.Category == apiIssue.Category.Name;

            if (!assign)
            {
                return;
            }

            Person dbPerson = null;

            switch (assignmentConfig.UserField)
            {
                case "Author":
                    if (apiIssue.Author == null)
                    {
                        return;
                    }
                    else if (apiIssue.CreatedOn < assignmentConfig.MinimumDate)
                    {
                        return;
                    }
                    else if (apiIssue.CreatedOn > assignmentConfig.MaximumDate)
                    {
                        return;
                    }

                    dbPerson = _cache.GetPerson(apiIssue.Author.Id);

                    if (dbPerson == null)
                    {
                        _logger.Notice(
                            "Cannot find author user id {0} from issue {1}",
                            apiIssue.Author.Id,
                            apiIssue.Id);
                        _redmine.AddNote(apiIssue.Id, "Erstellender Benutzer nicht gefunden.");
                        apiIssue = _redmine.GetIssue(apiIssue.Id);
                        dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
                        _database.Save(dbIssue);
                        return;
                    }

                    break;
                case "Assignee":
                    if (apiIssue.AssignedTo == null)
                    {
                        return;
                    }
                    else if (apiIssue.UpdatedOn < assignmentConfig.MinimumDate)
                    {
                        return;
                    }
                    else if (apiIssue.UpdatedOn > assignmentConfig.MaximumDate)
                    {
                        return;
                    }

                    dbPerson = _cache.GetPerson(apiIssue.AssignedTo.Id);

                    if (dbPerson == null)
                    {
                        _logger.Notice(
                            "Cannot find assignee user id {0} from issue {1}",
                            apiIssue.AssignedTo.Id,
                            apiIssue.Id);
                        _redmine.AddNote(apiIssue.Id, "Zusgewiesener Benutzer nicht gefunden.");
                        apiIssue = _redmine.GetIssue(apiIssue.Id);
                        dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
                        _database.Save(dbIssue);
                        return;
                    }
                    break;
                default:
                    var field = apiIssue.CustomFields
                        .SingleOrDefault(f => f.Name == assignmentConfig.UserField);

                    if (field != null)
                    {
                        dbPerson =
                            !string.IsNullOrEmpty(field.Value) ?
                            _cache.GetPerson(field.Value) : null;

                        if (dbPerson == null)
                        {
                            _logger.Notice(
                                "Cannot find user '{0}' from issue {1}",
                                field.Value,
                                apiIssue.Id);
                            _redmine.AddNote(apiIssue.Id, "Benutzer nicht gefunden.");
                            apiIssue = _redmine.GetIssue(apiIssue.Id);
                            dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
                            _database.Save(dbIssue);
                            return;
                        }
                    }
                    else
                    {
                        _logger.Warning(
                            "Cannot find username field '{0}' from assignment config '{1}' in issue {2}",
                            assignmentConfig.UserField,
                            assignmentConfig.Id,
                            apiIssue.Id);
                        return;
                    }
                    break;
            }

            int points = assignmentConfig.Points;

            if (!string.IsNullOrEmpty(assignmentConfig.PointsField))
            {
                var field = apiIssue.CustomFields
                    .SingleOrDefault(f => f.Name == assignmentConfig.PointsField);

                if (field != null)
                {
                    if (!int.TryParse(field.Value, out points))
                    {
                        _logger.Notice(
                            "Invalid value in the points field '{0}' in issue {1}",
                            field.Value,
                            apiIssue.Id);
                        _redmine.AddNote(apiIssue.Id, "Kann Punktewert nicht parsen.");
                        apiIssue = _redmine.GetIssue(apiIssue.Id);
                        dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
                        _database.Save(dbIssue);
                        return;
                    }
                }
                else
                {
                    _logger.Warning(
                        "Cannot find username field '{0}' from assignment config '{1}' in issue {2}",
                        assignmentConfig.UserField,
                        assignmentConfig.Id,
                        apiIssue.Id);
                    return;
                }
            }

            if (points == 0)
            {
                return; 
            }

            PointBudget budget = null;

            if (!string.IsNullOrEmpty(assignmentConfig.PointsBudgetField))
            {
                var field = apiIssue.CustomFields
                    .SingleOrDefault(f => f.Name == assignmentConfig.PointsBudgetField);

                if (field != null)
                {
                    budget = _quaestur.GetPointBudgetList()
                        .SingleOrDefault(b => b.FullLabel.IsAny(field.Value));

                    if (budget == null)
                    {
                        _logger.Notice(
                            "Cannot find points budget '{0}' from issue {1}",
                            field.Value,
                            apiIssue.Id);
                        _redmine.AddNote(apiIssue.Id, "Punktebudget nicht gefunden.");
                        apiIssue = _redmine.GetIssue(apiIssue.Id);
                        dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
                        _database.Save(dbIssue);
                        return;
                    }
                }
                else
                {
                    _logger.Warning(
                        "Cannot find points budget field '{0}' from assignment config '{1}'",
                        assignmentConfig.PointsBudgetField,
                        assignmentConfig.Id);
                    return;
                }
            }

            if (!string.IsNullOrEmpty(assignmentConfig.PointsBudget))
            {
                budget = _quaestur.GetPointBudgetList()
                    .SingleOrDefault(b => b.FullLabel.IsAny(assignmentConfig.PointsBudget));

                if (budget == null)
                {
                    _logger.Warning(
                        "Cannot find points budget '{0}' from assignment config '{1}'",
                        assignmentConfig.PointsBudget,
                        assignmentConfig.Id);
                    return;
                }
            }

            if (budget == null)
            {
                _logger.Warning(
                    "No points budget configured in assignment config '{0}'",
                    assignmentConfig.Id);
                return;
            }

            NamedId newStatus = null;

            if (!string.IsNullOrEmpty(assignmentConfig.NewStatus))
            {
                newStatus = _redmine.GetIssueStatuses()
                    .SingleOrDefault(s => s.Name == assignmentConfig.NewStatus);

                if (newStatus == null)
                {
                    _logger.Warning(
                        "Cannot find status '{0}' from assignment config '{1}'",
                        assignmentConfig.NewStatus,
                        assignmentConfig.Id);
                }
            }

            var reason = assignmentConfig.Reason
                .Replace("$Subject", apiIssue.Subject);

            var dbAssignment = new Assignment(Guid.NewGuid());
            dbAssignment.Person.Value = dbPerson;
            dbAssignment.ConfigId.Value = assignmentConfig.Id;
            dbAssignment.Issue.Value = dbIssue;
            dbAssignment.AwardedCalculation.Value = reason;
            dbAssignment.AwardedPoints.Value = points;

            var apiPoints = _quaestur.AddPoints(
                dbPerson.Id,
                budget.Id,
                points,
                reason,
                _config.RedmineApi.ApiUrl + "/issues/" + apiIssue.Id.ToString(),
                apiIssue.UpdatedOn,
                PointsReferenceType.RedmineIssue,
                dbAssignment.Id.Value,
                null);

            dbAssignment.AwardedPointsId.Value = apiPoints.Id;
            _database.Save(dbAssignment);

            _logger.Notice(
                "Assigned {0} to {1} from budget {2}.",
                points,
                dbPerson.UserName,
                budget.Label[QuaesturApi.Language.English]);
            var notes = string.Format(
                "{0} Punkte an @{1} aus Budget {2}",
                points,
                dbPerson.UserName,
                budget.Label[QuaesturApi.Language.German]);

            if (newStatus == null)
            {
                _redmine.AddNote(apiIssue.Id, notes);
            }
            else
            {
                _redmine.UpdateStatus(apiIssue.Id, newStatus.Id, notes);
            }

            apiIssue = _redmine.GetIssue(apiIssue.Id);
            dbIssue.UpdatedOn.Value = apiIssue.UpdatedOn;
            _database.Save(dbIssue);
        }
    }
}

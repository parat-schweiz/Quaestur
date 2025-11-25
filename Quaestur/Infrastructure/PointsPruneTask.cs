using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using MailKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PointsPruneTask : ITask
    {
        private DateTime _lastAction;

        public PointsPruneTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddHours(7))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Info("Points prune task");

                RunAll(database);

                Global.Log.Info("Points prune task complete");
            }
        }

        private void RunAll(IDatabase database)
        {
            var settings = database.Query<SystemWideSettings>().Single();

            foreach (var person in database
                .Query<Person>()
                .ToList())
            {
                try
                {
                    RunPerson(database, settings, person);
                }
                catch (Exception exception)
                {
                    Global.Log.Error("Point prune for {0} failed to process: {1}", person.ShortHand, exception.ToString());
                    Global.Mail.SendAdmin(
                        "Point prune process failed",
                        string.Format("Point prune failed to process: {0}", exception.ToString()));
                }
            }
        }

        private void RunPerson(IDatabase database, SystemWideSettings settings, Person person)
        {
            using (var transaction = database.BeginTransaction())
            {
                var tallies = database
                        .Query<PointsTally>(DC.Equal("personid", person.Id.Value))
                        .ToList();
                var pointsList = database
                    .Query<Points>(DC.Equal("ownerid", person.Id.Value))
                    .ToList();

                tallies.RemoveAll(t => t.UntilDate.Value.Year < DateTime.Now.Year - settings.PointsDataPreservationYears.Value);

                var oldestTallyFull = tallies.OrderBy(t => t.FromDate.Value).FirstOrDefault();

                if (oldestTallyFull != null)
                {
                    var deletePointsList = pointsList
                        .Where(p => p.Moment.Value.Date < oldestTallyFull.FromDate.Value.Date)
                        .ToList();

                    if (deletePointsList.Any())
                    {
                        foreach (var points in deletePointsList)
                        {
                            points.Delete(database);
                        }

                        Global.Log.Notice("Pruned {0} points entries from {1}.", deletePointsList.Count, person);
                    }

                    var deleteTalliesList = tallies
                        .Where(t => t.FromDate.Value.Year < DateTime.UtcNow.Year - settings.PointsTallyDataPreservationYears.Value)
                        .ToList();

                    if (deleteTalliesList.Any())
                    {
                        foreach (var pointsTally in deleteTalliesList)
                        {
                            pointsTally.Delete(database);
                        }

                        Global.Log.Notice("Pruned {0} points talies from {1}.", deleteTalliesList.Count, person);
                    }
                }

                transaction.Commit();
            }
        }
    }
}

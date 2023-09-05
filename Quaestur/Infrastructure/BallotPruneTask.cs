using System;
using System.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class BallotPruneTask : ITask
    {
        private DateTime _lastAction;

        public BallotPruneTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddHours(7))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Info("Ballot prune task");

                RunAll(database);

                Global.Log.Info("Ballot prune task complete");
            }
        }

        private void RunAll(IDatabase database)
        {
            var settings = database.Query<SystemWideSettings>().Single();

            using (var transaction = database.BeginTransaction())
            {
                var oldBallots = database
                    .Query<Ballot>()
                    .Where(m => m.EndDate.Value < DateTime.UtcNow.AddDays(-(settings.BallotPreservationDays.Value)))
                    .ToList();

                if (oldBallots.Any())
                {
                    foreach (var ballots in oldBallots)
                    {
                        ballots.Delete(database);
                    }

                    Global.Log.Notice("Pruned {0} ballots", oldBallots.Count);
                    transaction.Commit();
                }
            }
        }
    }
}

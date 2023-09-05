using System;
using System.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class MailingPruneTask : ITask
    {
        private DateTime _lastAction;

        public MailingPruneTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddHours(7))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Info("Mailing prune task");

                RunAll(database);

                Global.Log.Info("Mailing prune task complete");
            }
        }

        private bool IsOld(SystemWideSettings settings, Mailing mailing)
        {
            if (mailing.SentDate.Value.HasValue)
            {
                return mailing.SentDate.Value < DateTime.UtcNow.AddDays(-(settings.MailingPreservationDays.Value));
            }
            else if (mailing.SendingDate.Value.HasValue)
            {
                return mailing.SentDate.Value < DateTime.UtcNow.AddDays(-(settings.MailingPreservationDays.Value));
            }
            else
            {
                return mailing.CreatedDate.Value < DateTime.UtcNow.AddDays(-(settings.MailingPreservationDays.Value));
            }
        }

        private void RunAll(IDatabase database)
        {
            var settings = database.Query<SystemWideSettings>().Single();

            using (var transaction = database.BeginTransaction())
            {
                var oldMailings = database
                    .Query<Mailing>()
                    .Where(m => IsOld(settings, m))
                    .ToList();

                if (oldMailings.Any())
                {
                    foreach (var mailing in oldMailings)
                    {
                        mailing.Delete(database);
                    }

                    Global.Log.Notice("Pruned {0} mailings", oldMailings.Count);
                    transaction.Commit();
                }
            }
        }
    }
}

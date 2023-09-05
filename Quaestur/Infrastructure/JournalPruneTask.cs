using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using MailKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class JournalPruneTask : ITask
    {
        private DateTime _lastAction;

        public JournalPruneTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddHours(7))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Info("Journal prune task");

                RunAll(database);

                Global.Log.Info("Journal prune task complete");
            }
        }

        private void RunAll(IDatabase database)
        {
            var settings = database.Query<SystemWideSettings>().Single();

            using (var transaction = database.BeginTransaction())
            {
                var oldEntries = database
                    .Query<JournalEntry>()
                    .Where(p => p.Moment.Value < DateTime.UtcNow.AddDays(-settings.JournalPreservationDays.Value))
                    .ToList();

                if (oldEntries.Any())
                {
                    foreach (var journalEntry in oldEntries)
                    {
                        journalEntry.Delete(database);
                    }

                    Global.Log.Notice("Pruned {0} journal entries", oldEntries.Count);
                    transaction.Commit();
                }
            }
        }
    }
}

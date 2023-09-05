using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using MailKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class CreditsPruneTask : ITask
    {
        private DateTime _lastAction;

        public CreditsPruneTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddHours(7))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Info("Credits prune task");

                RunAll(database);

                Global.Log.Info("Credits prune task complete");
            }
        }

        private void RunAll(IDatabase database)
        {
            var settings = database.Query<SystemWideSettings>().Single();

            foreach (var person in database
                .Query<Person>()
                .ToList())
            {
                RunPerson(database, settings, person);
            }
        }

        private void RunPerson(IDatabase database, SystemWideSettings settings, Person person)
        {
            using (var transaction = database.BeginTransaction())
            {
                var creditsList = database
                        .Query<Credits>(DC.Equal("ownerid", person.Id.Value))
                        .ToList();

                var oldCredits = creditsList
                    .Where(c => c.Moment.Value.Year < DateTime.UtcNow.Year - settings.CreditsDataPreservationYears.Value)
                    .ToList();

                if (oldCredits.Any())
                {
                    var translation = new Translation(database);
                    var translator = new Translator(translation, person.Language.Value);

                    var balanceForward = new Credits(Guid.NewGuid());
                    balanceForward.Owner.Value = person;
                    balanceForward.Amount.Value = oldCredits.Sum(c => c.Amount.Value);
                    balanceForward.Moment.Value = new DateTime(DateTime.UtcNow.Year - settings.CreditsDataPreservationYears.Value, 1, 1);
                    balanceForward.Reason.Value = translator.Get(
                        "Credits.Prune.BalanceForward.Reason", "Reason stated for credits prune balance forward.", "Übertrag");
                    balanceForward.ReferenceType.Value = InteractionReferenceType.BalanceForward;
                    database.Save(balanceForward);

                    foreach (var credits in oldCredits)
                    {
                        credits.Delete(database);
                    }

                    Global.Log.Notice("Pruning consolidated {0} credits entries from {1} into balance forward.", oldCredits.Count, person);

                    transaction.Commit();
                }
            }
        }
    }
}

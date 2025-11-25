using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using MailKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class CreditsDecayTask : ITask
    {
        private DateTime _lastAction;

        public CreditsDecayTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddMinutes(42))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Info("Running credits decay task");

                RunAll(database);

                Global.Log.Info("Credits decay task complete");
            }
        }

        private void RunAll(IDatabase database)
        {
            var settings = database.Query<SystemWideSettings>().Single();

            foreach (var person in database
                .Query<Person>()
                .Where(p => !p.Deleted.Value)
                .ToList())
            {
                try
                {
                    RunPerson(person, database, settings);
                }
                catch (Exception exception)
                {
                    Global.Log.Error("Credits decay for {0} failed to process: {1}", person.ShortHand, exception.ToString());
                    Global.Mail.SendAdmin(
                        "Credits decay process failed", 
                        string.Format("Credits decay failed to process: {0}", exception.ToString()));
                }
            }
        }

        private void RunPerson(Person person, IDatabase database, SystemWideSettings settings)
        {
            var age = DateTime.UtcNow.AddDays(-(settings.CreditsDecayAgeDays.Value));
            var translation = new Translation(database);
            var translator = new Translator(translation, person.Language.Value);

            using (var transaction = database.BeginTransaction())
            { 
                var credits = database
                    .Query<Credits>(DC.Equal("ownerid", person.Id.Value))
                    .OrderBy(c => c.Moment.Value)
                    .ToList();

                if (credits.Any() && (credits.First().Moment.Value < age))
                {
                        var balanceAtAge = credits
                            .Where(c => c.Moment.Value < age)
                            .SumOrDefault(c => c.Amount.Value, 0);
                        var spentAfterAge = credits
                            .Where(c => (c.Moment.Value >= age) && (c.Amount.Value < 0))
                            .SumOrDefault(c => (-c.Amount.Value), 0);

                    if (spentAfterAge < balanceAtAge)
                    {
                        var totalBalance = credits
                            .SumOrDefault(c => c.Amount.Value, 0);
                        var newDecay = new Credits(Guid.NewGuid());
                        newDecay.Owner.Value = person;
                        newDecay.Amount.Value = -(balanceAtAge - spentAfterAge);
                        newDecay.Reason.Value = translator.Get(
                            "Credits.Decay.Reason", "Reason stated for credits decay.", "Decay");
                        newDecay.ReferenceType.Value = InteractionReferenceType.Decay;
                        newDecay.Moment.Value = DateTime.UtcNow;
                        database.Save(newDecay);
                        transaction.Commit();
                        Global.Log.Info(
                            "Credits for person {0} decayed to the amount of {1} because of a lack of spending.",
                            person.GetText(translator),
                            newDecay.Amount.Value);
                    }
                    else
                    {
                        Global.Log.Info(
                            "Credits for person {0} do not decay because eough were spent.",
                            person.GetText(translator));
                    }
                }
                else
                {
                    Global.Log.Info(
                        "Person {0} does not have any aged credits.",
                        person.GetText(translator));
                }
            }
        }
    }
}

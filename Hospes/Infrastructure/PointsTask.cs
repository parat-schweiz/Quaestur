using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using MailKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class PointsTask : ITask
    {
        private DateTime _lastAction;

        public PointsTask()
        {
            _lastAction = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastAction.AddMinutes(15))
            {
                _lastAction = DateTime.UtcNow;
                Global.Log.Notice("Running points task");

                RunAll(database);

                Global.Log.Notice("Points task complete");
            }
        }

        private void RunAll(IDatabase database)
        {
            foreach (var organization in database.Query<Organization>())
            {
                var periods = database.Query<BudgetPeriod>(DC.Equal("organizationid", organization.Id.Value)).ToList();

                if (!periods.Any())
                {
                    var newPeriod = new BudgetPeriod(Guid.NewGuid());
                    newPeriod.Organization.Value = organization;
                    newPeriod.StartDate.Value = new DateTime(DateTime.UtcNow.Year, 1, 1);
                    newPeriod.EndDate.Value = new DateTime(DateTime.UtcNow.Year + 1, 1, 1).AddDays(-1);
                    database.Save(newPeriod);
                    periods.Add(newPeriod);
                }

                if (!periods.Any(p => p.EndDate.Value >= DateTime.UtcNow.AddDays(180)))
                {
                    var newPeriod = new BudgetPeriod(Guid.NewGuid());
                    newPeriod.Organization.Value = organization;
                    newPeriod.StartDate.Value = periods.OrderByDescending(p => p.EndDate.Value).First().EndDate.Value.Date.AddDays(1);
                    newPeriod.EndDate.Value = new DateTime(newPeriod.StartDate.Value.Year + 1, 1, 1).AddDays(-1);
                    database.Save(newPeriod);
                    periods.Add(newPeriod);
                }

                foreach (var period in periods
                    .Where(p => p.EndDate.Value.Date >= DateTime.UtcNow.Date)
                    .OrderByDescending(p => p.Organization.Value.Subordinates.Count()))
                {
                    period.UpdateTotalPoints(database);
                    database.Save(period);

                    foreach (var budget in database.Query<PointBudget>(DC.Equal("periodid", period.Id.Value)))
                    {
                        budget.UpdateCurrentPoints(database);
                        database.Save(budget);
                    }
                }
            }
        }
    }
}

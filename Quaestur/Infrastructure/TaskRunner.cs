using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public interface ITask
    {
        void Run(IDatabase database);
    }

    public class TaskRunner
    {
        private List<ITask> _task;
        private IDatabase _database;

        public TaskRunner()
        {
            _task = new List<ITask>();
            _task.Add(new MailingTask());
            _task.Add(new BillingTask());
            _task.Add(new BillingReminderTask());
            _task.Add(new VotingRightsUpdateTask());
            _task.Add(new BallotTask());
            _task.Add(new PointsTask());
            _task.Add(new PointsTallyTask());
            _task.Add(new PaymentParameterUpdateReminderTask());
            _database = Global.CreateDatabase();
        }

        public void Run()
        {
            foreach (var task in _task)
            {
                task.Run(_database);
            }
        }
    }
}

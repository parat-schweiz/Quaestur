using System;
using System.Collections.Generic;
using System.Threading;
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
            _task.Add(new PointsTallyTask());
            _task.Add(new BillingTask());
            _task.Add(new BillingReminderTask());
            _task.Add(new VotingRightsUpdateTask());
            _task.Add(new BallotTask());
            _task.Add(new PointsTask());
            _task.Add(new PaymentParameterUpdateTask());
            _task.Add(new LogBufferTask());
            _task.Add(new CreditsDecayTask());
            _task.Add(new CreditsPruneTask());
            _task.Add(new PointsPruneTask());
            _task.Add(new JournalPruneTask());
            _task.Add(new MailingPruneTask());
            _task.Add(new BallotPruneTask());
            _task.Add(new MembershipTaskTask());
            _database = Global.CreateDatabase();
        }

        public void Run()
        {
            for (int i = 0; i < Global.Config.StartTaskWaitSeconds; i++)
            {
                Thread.Sleep(1000);
            }

            foreach (var task in _task)
            {
                task.Run(_database);
            }
        }
    }
}

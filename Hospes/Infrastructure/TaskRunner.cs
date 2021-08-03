using System;
using System.Collections.Generic;
using System.Threading;
using SiteLibrary;

namespace Hospes
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

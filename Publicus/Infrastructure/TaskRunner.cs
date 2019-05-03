using System;
using System.Collections.Generic;

namespace Publicus
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
            foreach (var task in _task)
            {
                task.Run(_database);
            }
        }
    }
}

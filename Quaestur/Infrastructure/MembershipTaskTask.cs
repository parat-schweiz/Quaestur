using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;
using System.Text;

namespace Quaestur
{
    public class MembershipTaskTask : ITask
    {
        private DateTime _lastSending;
        private List<MembershipTask> _ready;
        private List<MembershipTask> _errors;

        public MembershipTaskTask()
        {
            _lastSending = DateTime.MinValue;
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                _ready = new List<MembershipTask>();
                _errors = new List<MembershipTask>();

                Global.Log.Info("Running membership task task");

                foreach (var task in database.Query<MembershipTask>().ToList())
                {
                    if (Global.MailCounter.Available)
                    {
                        RunTask(database, task);
                    }
                }

                if (_errors.Any())
                {
                    var text = new StringBuilder();

                    foreach (var error in _errors)
                    {
                        text.AppendLine(error.Error.Value);
                        text.AppendLine();
                    }

                    Global.Mail.SendAdmin("Membership task(s) failed", text.ToString());
                    Global.MailCounter.Used();
                }

                foreach (var group in _ready.GroupBy(t => t.Membership.Value.Type.Value))
                {
                    var text = new StringBuilder();
                    text.AppendLine(string.Format("There are {0} new membership tasks pending", group.Count()));
                    text.AppendLine();
                    text.AppendLine(string.Format("{0}/membershiptasks", Global.Config.WebSiteAddress));
                    Global.Mail.Send(
                        group.Key.NotificationGroup.Value.MailAddress.Value.AnyValue,
                        "New membership tasks pending",
                        text.ToString());
                    Global.MailCounter.Used();
                }

                Global.Log.Info("Membership task task complete");
            }
        }

        private void RunTask(IDatabase database, MembershipTask task)
        {
            switch (task.Status.Value)
            {
                case MembershipTaskStatus.New:
                    {
                        var taskObject = task.Create(database);
                        var validation = taskObject.Validate();
                        if (string.IsNullOrEmpty(validation))
                        {
                            _ready.Add(task);
                        }
                        else
                        {
                            task.Modifed.Value = DateTime.UtcNow;
                            task.Status.Value = MembershipTaskStatus.Invalidated;
                            task.Message.Value = validation;
                            task.Error.Value = string.Empty;
                            database.Save(task);
                        }
                    }
                    break;
                case MembershipTaskStatus.Hold:
                    {
                        var taskObject = task.Create(database);
                        var validation = taskObject.Validate();
                        if (!string.IsNullOrEmpty(validation))
                        {
                            task.Modifed.Value = DateTime.UtcNow;
                            task.Status.Value = MembershipTaskStatus.Invalidated;
                            task.Message.Value = validation;
                            task.Error.Value = string.Empty;
                            database.Save(task);
                        }
                    }
                    break;
                case MembershipTaskStatus.Ready:
                    {
                        var taskObject = task.Create(database);
                        var validation = taskObject.Validate();
                        if (string.IsNullOrEmpty(validation))
                        {
                            try
                            {
                                taskObject.Execute();
                                task.Modifed.Value = DateTime.UtcNow;
                                task.Status.Value = MembershipTaskStatus.Successful;
                                task.Message.Value = string.Empty;
                                task.Error.Value = string.Empty;
                                database.Save(task);
                            }
                            catch (Exception exception)
                            {
                                task.Modifed.Value = DateTime.UtcNow;
                                task.Status.Value = MembershipTaskStatus.Failed;
                                task.Message.Value = exception.Message;
                                task.Error.Value = exception.ToString();
                                database.Save(task);
                                _errors.Add(task);
                            }
                        }
                        else
                        {
                            task.Modifed.Value = DateTime.UtcNow;
                            task.Status.Value = MembershipTaskStatus.Invalidated;
                            task.Message.Value = validation;
                            task.Error.Value = string.Empty;
                            database.Save(task);
                        }
                    }
                    break;
                case MembershipTaskStatus.Failed:
                case MembershipTaskStatus.Successful:
                case MembershipTaskStatus.Invalidated:
                    if (DateTime.UtcNow.Subtract(task.Modifed.Value).TotalDays > 3d)
                    {
                        task.Delete(database);
                    }
                    break;
            }
        }
    }
}

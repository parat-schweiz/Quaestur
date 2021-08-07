using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class LogBufferTask : ITask
    {
        private DateTime _lastCheck;

        public LogBufferTask()
        {
            _lastCheck = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastCheck.AddMinutes(5))
            {
                _lastCheck = DateTime.UtcNow;
                Global.Log.Info("Running log buffer task");
                RunTask();
                Global.Log.Info("Log buffer task complete");
            }
        }

        private void RunTask()
        {
            Global.Log.Verbose("Logg buffer content severity: {0}", Global.Log.BufferContentSeverity);
            Global.Log.Verbose("Logg buffer content age: {0}", Global.Log.BufferContentAge);
            Global.Log.Verbose("Logg buffer count: {0}", Global.Log.BufferCount);

            switch (Global.Log.BufferContentSeverity)
            {
                case LogSeverity.Error:
                    {
                        string subject = string.Format("{0} has error", Global.Config.SiteName);
                        string text = string.Join(Environment.NewLine, Global.Log.PopBuffer());
                        Global.Mail.SendAdmin(subject, text);
                    }
                    break;
                case LogSeverity.Warning:
                    if ((DateTime.UtcNow >= Global.Log.BufferContentAge.AddHours(2)) ||
                        (Global.Log.BufferCount >= 10000))
                    {
                        string subject = string.Format("{0} has warning", Global.Config.SiteName);
                        string text = string.Join(Environment.NewLine, Global.Log.PopBuffer());
                        Global.Mail.SendAdmin(subject, text);
                    }
                    break;
                case LogSeverity.Notice:
                    if ((DateTime.UtcNow >= Global.Log.BufferContentAge.AddHours(24)) ||
                        (Global.Log.BufferCount >= 10000))
                    {
                        string subject = string.Format("{0} has notice", Global.Config.SiteName);
                        string text = string.Join(Environment.NewLine, Global.Log.PopBuffer());
                        Global.Mail.SendAdmin(subject, text);
                    }
                    break;
                default:
                    if (Global.Log.BufferCount >= 10000)
                    {
                        string subject = string.Format("{0} buffer clear", Global.Config.SiteName);
                        string text = string.Join(Environment.NewLine, Global.Log.PopBuffer());
                        Global.Mail.SendAdmin(subject, text);
                    }
                    break;
            }
        }
    }
}

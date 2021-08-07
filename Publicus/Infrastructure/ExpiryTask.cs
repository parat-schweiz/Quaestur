using System;
using System.Linq;
using System.Collections.Generic;
using MimeKit;
using BaseLibrary;
using SiteLibrary;

namespace Publicus
{
    public class ExpiryTask : ITask
    {
        private DateTime _lastSending;

        public ExpiryTask()
        {
            _lastSending = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(65))
            {
                _lastSending = DateTime.UtcNow;
                Global.Log.Info("Running expiry task");

                foreach (var contact in database.Query<Contact>().ToList())
                {
                    if (contact.ExpiryDate.Value.HasValue &&
                        DateTime.UtcNow > contact.ExpiryDate.Value.Value)
                    {
                        contact.Delete(database);
                    }
                }

                Global.Log.Info("Mailing expiry complete");
            }
        }
    }
}

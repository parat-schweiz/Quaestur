using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BaseLibrary;
using SiteLibrary;

namespace Census
{
    public class SurveySession
    {
        public Guid Id { get; private set; }
        public DateTime LastUsed { get; private set; }
        public Guid CurrentQuestionId { get; set; }
        public Language Language { get; set; }

        public SurveySession()
        {
            Id = Guid.NewGuid();
            LastUsed = DateTime.UtcNow;
        }

        public void Used()
        {
            LastUsed = DateTime.UtcNow;
        }

        public bool Outdated
        {
            get
            {
                return DateTime.UtcNow.Subtract(LastUsed).TotalHours > 48;
            }
        }
    }
}

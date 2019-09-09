using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using BaseLibrary;
using SiteLibrary;

namespace Census
{
    public class SurveySessionManager
    {
        private Dictionary<Guid, SurveySession> _sessions;

        public SurveySessionManager()
        {
            _sessions = new Dictionary<Guid, SurveySession>();
        }

        public void Add(SurveySession session)
        {
            CleanUp();

            lock (_sessions)
            {
                _sessions.Add(session.Id, session);
            }
        }

        public SurveySession Get(Guid sessionId)
        {
            CleanUp();

            lock (_sessions)
            {
                if (_sessions.ContainsKey(sessionId))
                {
                    var session = _sessions[sessionId];
                    session.Used();
                    return session;
                }
                else
                {
                    return null;
                }
            }
        }

        public void CleanUp()
        {
            lock (_sessions)
            {
                var outdated = _sessions.Values.Where(s => s.Outdated).ToList();

                foreach (var session in outdated)
                {
                    _sessions.Remove(session.Id); 
                }
            }
        }
    }
}

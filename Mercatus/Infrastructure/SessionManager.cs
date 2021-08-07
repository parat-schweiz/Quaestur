using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;
using System.Security.Claims;
using System.Security.Principal;

namespace Mercatus
{
    public enum LoginResult
    {
        Success = 0,
        WrongLogin = 1,
        Locked = 2,
    }

    public class Session : ClaimsPrincipal
    {
        public const string IdentityIdClaim = "IdentityId";
        public const string AuthenticationType = "Login";

        public Guid Id { get; private set; }
        public User User { get; private set; } 
        public DateTime LastAccess { get; private set; }

        public Session(User user)
        {
            Id = Guid.NewGuid();
            User = user;
            LastAccess = DateTime.UtcNow;
        }

        public void Update()
        {
            LastAccess = DateTime.UtcNow;
        }

        public bool Expired
        {
            get
            {
                return DateTime.UtcNow > LastAccess + new TimeSpan(0, 1, 0, 0);
            } 
        }

        public string UserName
        {
            get { return User.UserName; }
        }

        public override bool HasClaim(Predicate<Claim> match)
        {
            return Claims.Any(c => match(c));
        }

        public override IIdentity Identity
        {
            get
            {
                return Identities.First();
            }
        }

        public override IEnumerable<ClaimsIdentity> Identities
        {
            get
            {
                yield return new ClaimsIdentity(Claims, AuthenticationType);
            }
        }

        public override IEnumerable<Claim> Claims
        {
            get
            {
                yield return new Claim(IdentityIdClaim, User.Id.ToString());
            }
        }
    }

    public class SessionManager : IUserMapper
    {
        private readonly Dictionary<Guid, Session> _sessions;

        public SessionManager()
        {
            _sessions = new Dictionary<Guid, Session>();
        }

        public Session Add(User user)
        {
            lock (_sessions)
            {
                var session = new Session(user);
                _sessions.Add(session.Id, session);
                return session;
            }
        }

        public void Remove(Session session)
        {
            lock (_sessions)
            {
                if (_sessions.ContainsKey(session.Id))
                {
                    _sessions.Remove(session.Id);
                }
            }
        }

        public void CleanUp()
        {
            lock (_sessions)
            {
                foreach (var session in _sessions.Values.Where(s => s.Expired).ToList())
                {
                    _sessions.Remove(session.Id);
                }
            }
        }

        public ClaimsPrincipal GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            lock (_sessions)
            {
                if (_sessions.ContainsKey(identifier))
                {
                    var session = _sessions[identifier];

                    if (session.Expired)
                    {
                        _sessions.Remove(session.Id);
                        return null;
                    }
                    else
                    {
                        session.Update();
                        return session;
                    }
                }
                else
                {
                    return null;
                }
            }
        }
    }
}

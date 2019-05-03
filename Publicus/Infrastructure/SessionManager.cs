using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;

namespace Publicus
{
    public enum LoginResult
    {
        Success = 0,
        WrongLogin = 1,
        Locked = 2,
    }

    public class Session : IUserIdentity
    {
        private class RolePermission
        {
            public Role Role { get; private set; } 
            public Permission Permission { get; private set; }

            public RolePermission(Role role, Permission permission)
            {
                Role = role;
                Permission = permission;
            }
        }

        private List<RolePermission> _access;
        public Guid Id { get; private set; }
        public User User { get; private set; } 
        public DateTime LastAccess { get; private set; }
        public List<MasterRole> MasterRoles { get; private set; }

        public IEnumerable<RoleAssignment> RoleAssignments
        {
            get
            {
                return MasterRoles.SelectMany(mr => mr.RoleAssignments);
            }
        }

        public bool HasContactNewAccess()
        {
            return RoleAssignments
                .Select(ra => ra.Role.Value.Group.Value.Feed.Value)
                .Where(o => HasAccess(o, PartAccess.Demography, AccessRight.Write))
                .Where(o => HasAccess(o, PartAccess.Subscription, AccessRight.Write))
                .Where(o => HasAccess(o, PartAccess.Contact, AccessRight.Write))
                .Any();
        }

        public bool HasSystemWideAccess(PartAccess partAccess, AccessRight right)
        {
            foreach (var rolePermission in _access
                .Where(rp => rp.Permission.Part.Value == partAccess)
                .Where(rp => rp.Permission.Right.Value >= right))
            {
                switch (rolePermission.Permission.Subject.Value)
                {
                    case SubjectAccess.SystemWide:
                        return true;
                }
            }

            return false;
        }

        public bool HasAnyFeedAccess(PartAccess partAccess, AccessRight right)
        {
            foreach (var rolePermission in _access
                .Where(rp => rp.Permission.Part.Value == partAccess)
                .Where(rp => rp.Permission.Right.Value >= right))
            {
                switch (rolePermission.Permission.Subject.Value)
                {
                    case SubjectAccess.SystemWide:
                    case SubjectAccess.Feed:
                    case SubjectAccess.SubFeed:
                        return true;
                }
            }

            return false;
        }

        private bool HasThisAccess(RolePermission rp)
        {
            switch (rp.Permission.Subject.Value)
            {
                case SubjectAccess.None:
                    return true;
                case SubjectAccess.SystemWide:
                    return HasSystemWideAccess(rp.Permission.Part.Value, rp.Permission.Right.Value);
                case SubjectAccess.Group:
                    return HasAccess(rp.Role.Group.Value, rp.Permission.Part.Value, rp.Permission.Right.Value);
                case SubjectAccess.Feed:
                    return HasAccess(rp.Role.Group.Value.Feed.Value, rp.Permission.Part.Value, rp.Permission.Right.Value);
               case SubjectAccess.SubFeed:
                    return HasAccess(rp.Role.Group.Value.Feed.Value, rp.Permission.Part.Value, rp.Permission.Right.Value) &&
                        rp.Role.Group.Value.Feed.Value.Children.All(c =>
                            HasAccess(c, rp.Permission.Part.Value, rp.Permission.Right.Value));
                default:
                    throw new NotSupportedException();
            }
        }

        public bool HasAccess(Contact contact, PartAccess partAccess, AccessRight right)
        {
            if ((partAccess != PartAccess.Deleted) &&
                contact.Deleted && 
                !HasAccess(contact, PartAccess.Deleted, right))
            {
                return false;
            }

            if (User == contact)
            {
                switch (partAccess)
                {
                    case PartAccess.Anonymous:
                    case PartAccess.Contact:
                        return true;
                    case PartAccess.Demography:
                    case PartAccess.Documents:
                    case PartAccess.Subscription:
                    case PartAccess.RoleAssignments:
                    case PartAccess.TagAssignments:
                    case PartAccess.Journal:
                        if (right == AccessRight.Read)
                        {
                            return true; 
                        }
                        break;
                }
            }

            foreach (var subscription in contact.ActiveSubscriptions)
            {
                if (HasAccess(subscription.Feed.Value, partAccess, right))
                {
                    return true; 
                } 
            }

            return false;
        }

        public bool HasAccess(Feed feed, PartAccess partAccess, AccessRight right)
        {
            foreach (var rolePermission in _access
                .Where(rp => rp.Permission.Part.Value == partAccess)
                .Where(rp => rp.Permission.Right.Value >= right))
            {
                switch (rolePermission.Permission.Subject.Value)
                {
                    case SubjectAccess.SystemWide:
                        return true;
                    case SubjectAccess.Feed:
                        if (rolePermission.Role.Group.Value.Feed.Value == feed)
                        {
                            return true; 
                        }
                        break;
                    case SubjectAccess.SubFeed:
                        if (rolePermission.Role.Group.Value.Feed.Value == feed)
                        {
                            return true;
                        }
                        else if (rolePermission.Role.Group.Value.Feed.Value
                            .Subordinates.Contains(feed))
                        {
                            return true; 
                        }
                        break;
                    case SubjectAccess.Group:
                        break;
                }
            }

            return false;
        }

        public bool HasAccess(Group group, PartAccess partAccess, AccessRight right)
        {
            foreach (var rolePermission in _access
                .Where(rp => rp.Permission.Part.Value == partAccess)
                .Where(rp => rp.Permission.Right.Value >= right))
            {
                switch (rolePermission.Permission.Subject.Value)
                {
                    case SubjectAccess.SystemWide:
                        return true;
                    case SubjectAccess.Feed:
                        if (rolePermission.Role.Group.Value.Feed.Value
                            .Groups.Contains(group))
                        {
                            return true;
                        }
                        break;
                    case SubjectAccess.SubFeed:
                        if (rolePermission.Role.Group.Value.Feed.Value
                            .Groups.Contains(group))
                        {
                            return true;
                        }
                        else if (rolePermission.Role.Group.Value.Feed.Value
                            .Subordinates.SelectMany(o => o.Groups).Contains(group))
                        {
                            return true;
                        }
                        break;
                    case SubjectAccess.Group:
                        if (rolePermission.Role.Group.Value == group)
                        {
                            return true; 
                        }
                        break;
                }
            }

            return false;
        }

        public Session(User user, IEnumerable<MasterRole> masterRoles)
        {
            Id = Guid.NewGuid();
            User = user;
            LastAccess = DateTime.UtcNow;
            MasterRoles = masterRoles.ToList();
            _access = new List<RolePermission>(RoleAssignments
                .Select(ra => ra.Role.Value)
                .SelectMany(r => r.Permissions.Select(p => new RolePermission(r, p))));
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

        public IEnumerable<string> Claims
        {
            get
            {
                yield return User.Id.ToString();
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

        public Session Add(User user, IEnumerable<MasterRole> masterRoles)
        {
            lock (_sessions)
            {
                var session = new Session(user, masterRoles);
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

        public IUserIdentity GetUserFromIdentifier(Guid identifier, NancyContext context)
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

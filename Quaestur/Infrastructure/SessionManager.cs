using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;

namespace Quaestur
{
    public enum LoginResult
    {
        Success = 0,
        WrongLogin = 1,
        Locked = 2,
    }

    public class Session : IUserIdentity
    {
        public const string CompleteAuthClaim = "CompleteAuth";
        public const string TwoFactorAuthClaim = "TwoFactorAuth";

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
        public Person User { get; private set; } 
        public DateTime LastAccess { get; private set; }
        public bool CompleteAuth { get; set; }
        public bool TwoFactorAuth { get; set; }
        public string ReturnUrl { get; set; }

        public void ReloadUser(IDatabase database)
        {
            User = database.Query<Person>(User.Id);
        }

        public bool HasPersonNewAccess()
        {
            if (!TwoFactorAuth)
            {
                return false;
            }

            return User.RoleAssignments
                .Select(ra => ra.Role.Value.Group.Value.Organization.Value)
                .Where(o => HasAccess(o, PartAccess.Demography, AccessRight.Write))
                .Where(o => HasAccess(o, PartAccess.Membership, AccessRight.Write))
                .Where(o => HasAccess(o, PartAccess.Contact, AccessRight.Write))
                .Any();
        }

        public bool HasSystemWideAccess(PartAccess partAccess, AccessRight right)
        {
            if (!TwoFactorAuth)
            {
                return false;
            }

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

        public bool HasAnyOrganizationAccess(PartAccess partAccess, AccessRight right)
        {
            if (!TwoFactorAuth)
            {
                return false;
            }

            foreach (var rolePermission in _access
                .Where(rp => rp.Permission.Part.Value == partAccess)
                .Where(rp => rp.Permission.Right.Value >= right))
            {
                switch (rolePermission.Permission.Subject.Value)
                {
                    case SubjectAccess.SystemWide:
                    case SubjectAccess.Organization:
                    case SubjectAccess.SubOrganization:
                        return true;
                }
            }

            return false;
        }

        public bool HasAllAccessOf(Person person)
        {
            if (!TwoFactorAuth)
            {
                return false;
            }

            var personAccess = new List<RolePermission>(person.RoleAssignments
                .Select(ra => ra.Role.Value)
                .SelectMany(r => r.Permissions.Select(p => new RolePermission(r, p))));

            return personAccess.All(HasThisAccess);
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
                case SubjectAccess.Organization:
                    return HasAccess(rp.Role.Group.Value.Organization.Value, rp.Permission.Part.Value, rp.Permission.Right.Value);
               case SubjectAccess.SubOrganization:
                    return HasAccess(rp.Role.Group.Value.Organization.Value, rp.Permission.Part.Value, rp.Permission.Right.Value) &&
                        rp.Role.Group.Value.Organization.Value.Children.All(c =>
                            HasAccess(c, rp.Permission.Part.Value, rp.Permission.Right.Value));
                default:
                    throw new NotSupportedException();
            }
        }

        public bool HasAccess(Person person, PartAccess partAccess, AccessRight right)
        {
            if ((partAccess != PartAccess.Deleted) &&
                person.Deleted && 
                !HasAccess(person, PartAccess.Deleted, right))
            {
                return false;
            }

            if (User == person)
            {
                switch (partAccess)
                {
                    case PartAccess.Anonymous:
                    case PartAccess.Contact:
                    case PartAccess.Security:
                        return true;
                    case PartAccess.Billing:
                    case PartAccess.Demography:
                    case PartAccess.Documents:
                    case PartAccess.Membership:
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

            if (!TwoFactorAuth)
            {
                return false; 
            }

            foreach (var membership in person.ActiveMemberships)
            {
                if (HasAccess(membership.Organization.Value, partAccess, right))
                {
                    return true; 
                } 
            }

            foreach (var roleAssignment in person.RoleAssignments)
            {
                if (HasAccess(roleAssignment.Role.Value.Group.Value, partAccess, right))
                {
                    return true; 
                }
            }

            return false;
        }

        public bool HasAccess(Organization organization, PartAccess partAccess, AccessRight right)
        {
            if (!TwoFactorAuth)
            {
                return false;
            }

            foreach (var rolePermission in _access
                .Where(rp => rp.Permission.Part.Value == partAccess)
                .Where(rp => rp.Permission.Right.Value >= right))
            {
                switch (rolePermission.Permission.Subject.Value)
                {
                    case SubjectAccess.SystemWide:
                        return true;
                    case SubjectAccess.Organization:
                        if (rolePermission.Role.Group.Value.Organization.Value == organization)
                        {
                            return true; 
                        }
                        break;
                    case SubjectAccess.SubOrganization:
                        if (rolePermission.Role.Group.Value.Organization.Value == organization)
                        {
                            return true;
                        }
                        else if (rolePermission.Role.Group.Value.Organization.Value
                            .Subordinates.Contains(organization))
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
            if (!TwoFactorAuth)
            {
                return false;
            }

            foreach (var rolePermission in _access
                .Where(rp => rp.Permission.Part.Value == partAccess)
                .Where(rp => rp.Permission.Right.Value >= right))
            {
                switch (rolePermission.Permission.Subject.Value)
                {
                    case SubjectAccess.SystemWide:
                        return true;
                    case SubjectAccess.Organization:
                        if (rolePermission.Role.Group.Value.Organization.Value
                            .Groups.Contains(group))
                        {
                            return true;
                        }
                        break;
                    case SubjectAccess.SubOrganization:
                        if (rolePermission.Role.Group.Value.Organization.Value
                            .Groups.Contains(group))
                        {
                            return true;
                        }
                        else if (rolePermission.Role.Group.Value.Organization.Value
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

        public Session(Person user)
        {
            Id = Guid.NewGuid();
            User = user;
            LastAccess = DateTime.UtcNow;
            CompleteAuth = false;
            TwoFactorAuth = false;
            _access = new List<RolePermission>(user.RoleAssignments
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

                if (CompleteAuth)
                {
                    yield return CompleteAuthClaim;
                }

                if (TwoFactorAuth)
                {
                    yield return TwoFactorAuthClaim;
                }
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

        public Session Add(Person user)
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

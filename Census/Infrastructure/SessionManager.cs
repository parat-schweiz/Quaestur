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

namespace Census
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

        public bool HasQuestionaireNewAccess()
        {
            return RoleAssignments
                .Select(ra => ra.Role.Value.Group.Value.Organization.Value)
                .Where(o => HasAccess(o, PartAccess.Questionaire, AccessRight.Write))
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

        public bool HasAnyOrganizationAccess(PartAccess partAccess, AccessRight right)
        {
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

        public bool HasAccess(Organization organization, PartAccess partAccess, AccessRight right)
        {
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

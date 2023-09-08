using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Security.Cryptography;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;
using SiteLibrary;
using System.Security.Principal;
using System.Security.Claims;

namespace Quaestur
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
        public const string AuthenticationClaim = "Authentication";
        public const string AuthenticationClaimTwoFactor = "TwoFactorAuth";
        public const string AuthenticationClaimComplete = "Complete";

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
        private DeviceSession _deviceSession;
        public Guid Id { get { return _deviceSession.Id.Value; } }
        public Person User { get { return _deviceSession.User.Value; } }
        public bool TwoFactorAuth { get { return _deviceSession.TwoFactorAuth.Value; } }
        public DateTime LastAccess { get { return _deviceSession.LastAccess.Value; } }
        public bool CompleteAuth { get; private set; }
        public string ReturnUrl { get; set; }

        public void ReloadDeviceSession(IDatabase database)
        {
            _deviceSession = database.Query<DeviceSession>(_deviceSession.Id);
            _access = new List<RolePermission>(User.RoleAssignments
                .Select(ra => ra.Role.Value)
                .SelectMany(r => r.Permissions.Select(p => new RolePermission(r, p))));
        }

        public void SetOneFactorLogin(IDatabase database)
        {
            Update(database);
            _deviceSession.TwoFactorAuth.Value = false;
            database.Save(_deviceSession);
            CompleteAuth = true;
        }

        public void SetTwoFactorLogin(IDatabase database)
        {
            Update(database);
            _deviceSession.TwoFactorAuth.Value = true;
            database.Save(_deviceSession);
            CompleteAuth = true;
        }

        public void DeleteDeviceSession(IDatabase database)
        {
            _deviceSession.Delete(database);
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
                        if (right <= AccessRight.Write)
                        {
                            return true;
                        }
                        break;
                    case PartAccess.Billing:
                    case PartAccess.Demography:
                    case PartAccess.Documents:
                    case PartAccess.Membership:
                    case PartAccess.RoleAssignments:
                    case PartAccess.TagAssignments:
                    case PartAccess.Journal:
                    case PartAccess.Points:
                        if (right <= AccessRight.Read)
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

            if (HasSystemWideAccess(partAccess, right))
            {
                return true; 
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

        public Session(IDatabase database, Person user, string sessionName)
        {
            _deviceSession = new DeviceSession(Guid.NewGuid());
            _deviceSession.User.Value = user;
            _deviceSession.Name.Value = sessionName;
            database.Save(_deviceSession);
            _access = new List<RolePermission>(user.RoleAssignments
                .Select(ra => ra.Role.Value)
                .SelectMany(r => r.Permissions.Select(p => new RolePermission(r, p))));
        }

        public Session(DeviceSession deviceSession)
        {
            _deviceSession = deviceSession;
            CompleteAuth = true;
            _access = new List<RolePermission>(User.RoleAssignments
                .Select(ra => ra.Role.Value)
                .SelectMany(r => r.Permissions.Select(p => new RolePermission(r, p))));
        }

        public void Update(IDatabase database)
        {
            if (_deviceSession.LastAccess.Value.AddSeconds(60) < DateTime.UtcNow)
            {
                _deviceSession.LastAccess.Value = DateTime.UtcNow;
                database.Save(_deviceSession);
            }
        }

        public bool Expired
        {
            get { return _deviceSession.Expired; }
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

                if (TwoFactorAuth)
                {
                    yield return new Claim(AuthenticationClaim, AuthenticationClaimTwoFactor);
                }

                if (CompleteAuth)
                {
                    yield return new Claim(AuthenticationClaim, AuthenticationClaimComplete);
                }
            }
        }
    }

    public class SessionManager : IUserMapper
    {
        private readonly Dictionary<Guid, Session> _sessions;
        private readonly IDatabase _database;
        private DateTime _lastDatabaseCleanup = DateTime.UtcNow;

        public SessionManager(IDatabase database)
        {
            _database = database;
            _sessions = new Dictionary<Guid, Session>();
        }

        public Session Add(Person user, string sessionName)
        {
            lock (_sessions)
            {
                var session = new Session(_database, user, sessionName);
                _sessions.Add(session.Id, session);
                return session;
            }
        }

        public void Remove(DeviceSession session)
        {
            lock (_sessions)
            {
                _database.Delete(session);

                if (_sessions.ContainsKey(session.Id))
                {
                    _sessions.Remove(session.Id);
                }
            }
        }

        public void Remove(Session session)
        {
            lock (_sessions)
            {
                session.DeleteDeviceSession(_database);

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
                foreach (var session in _sessions.Values
                    .Where(s => s.Expired).ToList())
                {
                    Remove(session);
                }

                if (DateTime.UtcNow > _lastDatabaseCleanup.AddHours(1))
                { 
                    foreach (var session in _database.Query<DeviceSession>()
                        .Where(s => s.Expired).ToList())
                    {
                        session.Delete(_database);
                    }
                    _lastDatabaseCleanup = DateTime.UtcNow;
                }
            }
        }

        public ClaimsPrincipal GetUserFromIdentifier(Guid identifier, NancyContext context)
        {
            lock (_sessions)
            {
                Session session = null;

                if (_sessions.ContainsKey(identifier))
                {
                    session = _sessions[identifier];
                }

                if (session != null)
                {
                    if (session.Expired)
                    {
                        Remove(session);
                        return null;
                    }
                    else
                    {
                        session.Update(_database);
                        return session;
                    }
                }
                else
                {
                    var deviceSession = _database.Query<DeviceSession>(identifier);

                    if (deviceSession != null)
                    {
                        session = new Session(deviceSession);

                        if (session.Expired)
                        {
                            session.DeleteDeviceSession(_database);
                            return null;
                        }
                        else
                        {
                            session.Update(_database);
                            _sessions.Add(session.Id, session);
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
}

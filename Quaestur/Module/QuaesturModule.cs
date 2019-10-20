using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nancy;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nancy.Security;
using SiteLibrary;

namespace Quaestur
{
    public class QuaesturModule : NancyModule, IDisposable
    {
        protected IDatabase Database { get; private set; }
        protected Translation Translation { get; private set; }

        public QuaesturModule()
        { 
            Database = Global.CreateDatabase();
            Translation = new Translation(Database);
        }

        protected void RequireCompleteLogin()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(c => c.Type == Quaestur.Session.AuthenticationClaim && c.Value == Quaestur.Session.AuthenticationClaimComplete);
        }

        protected string ReadBody()
        {
            using (var reader = new System.IO.StreamReader(Context.Request.Body))
            {
                return reader.ReadToEnd();
            }
        }

        protected byte[] GetDataUrlString(string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                var parts = stringValue.Split(new string[] { "data:", ";base64," }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    try
                    {
                        return Convert.FromBase64String(parts[1]);
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void Notice(string text, params object[] parameters)
        {
            Global.Log.Notice(text, parameters);
        }

        public void Info(string text, params object[] parameters)
        {
            Global.Log.Info(text, parameters);
        }

        public void Warning(string text, params object[] parameters)
        {
            Global.Log.Warning(text, parameters);
        }

        public Session CurrentSession
        {
            get 
            {
                return Context.CurrentUser as Session;
            } 
        }

        public bool HasPersonNewAccess()
        {
            return CurrentSession.HasPersonNewAccess();
        }

        private bool CheckAccess(AccessRight requestedRight, AccessRight actualRight)
        {
            switch (requestedRight)
            {
                case AccessRight.Write:
                    return actualRight == AccessRight.Write;
                case AccessRight.Read:
                    return actualRight == AccessRight.Write || actualRight == AccessRight.Read;
                default:
                    return false; 
            } 
        }

        public bool HasAccess(Person subject, Person person, PartAccess part, AccessRight right)
        {
            var roles = subject.RoleAssignments.Select(r => r.Role.Value).ToList();
            var permissions = new List<Permission>(roles
                .SelectMany(r => Database.Query<Permission>(DC.Equal("roleid", r.Id.Value)))
                .Distinct());

            foreach (var permission in permissions)
            {
                if (permission.Part.Value == part)
                {
                    switch (permission.Subject.Value)
                    {
                        case SubjectAccess.SystemWide:
                            if (CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.SubOrganization:
                            if (person.Memberships.Any(m => m.Organization.Value == permission.Role.Value.Group.Value.Organization.Value) &&
                                person.Memberships.Any(m => permission.Role.Value.Group.Value.Organization.Value.Subordinates.Contains(m.Organization.Value)) &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.Organization:
                            if (person.Memberships.Any(m => m.Organization.Value == permission.Role.Value.Group.Value.Organization.Value) &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.Group:
                            if (person.RoleAssignments.Any(r => r.Role.Value.Group.Value == permission.Role.Value.Group.Value) &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            return false;
        }


        public bool HasAccess(Person subject, Group group, PartAccess part, AccessRight right)
        {
            var roles = subject.RoleAssignments.Select(r => r.Role.Value).ToList();
            var permissions = new List<Permission>(roles
                .SelectMany(r => Database.Query<Permission>(DC.Equal("roleid", r.Id.Value)))
                .Distinct());

            foreach (var permission in permissions)
            {
                if (permission.Part.Value == part)
                {
                    switch (permission.Subject.Value)
                    {
                        case SubjectAccess.SystemWide:
                            if (CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.SubOrganization:
                            if (permission.Role.Value.Group.Value.Organization.Value.Groups.Contains(group) &&
                                permission.Role.Value.Group.Value.Organization.Value.Subordinates.Any(o => o.Groups.Contains(group)) &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.Organization:
                            if (permission.Role.Value.Group.Value.Organization.Value.Groups.Contains(group) &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.Group:
                            if (permission.Role.Value.Group.Value == group &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            return false;
        }

        public bool HasAccess(Person subject, Organization organization, PartAccess part, AccessRight right)
        {
            var roles = subject.RoleAssignments.Select(r => r.Role.Value).ToList();
            var permissions = new List<Permission>(roles
                .SelectMany(r => Database.Query<Permission>(DC.Equal("roleid", r.Id.Value)))
                .Distinct());

            foreach (var permission in permissions)
            {
                if (permission.Part.Value == part)
                {
                    switch (permission.Subject.Value)
                    {
                        case SubjectAccess.SystemWide:
                            if (CheckAccess(right, permission.Right.Value))
                            {
                                return true; 
                            }
                            break;
                        case SubjectAccess.SubOrganization:
                            if (permission.Role.Value.Group.Value.Organization.Value == organization &&
                                permission.Role.Value.Group.Value.Organization.Value.Subordinates.Contains(organization) &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.Organization:
                            if (permission.Role.Value.Group.Value.Organization.Value == organization &&
                                CheckAccess(right, permission.Right.Value))
                            {
                                return true;
                            }
                            break;
                        case SubjectAccess.Group:
                            break;
                        default:
                            break;
                    }
                }
            }

            return false;
        }

        public bool HasAllAccessOf(Person person)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAllAccessOf(person);
            }
        }

        public bool HasAnyOrganizationAccess(PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasSystemWideAccess(partAccess, right);
            }
        }

        public bool HasSystemWideAccess(PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasSystemWideAccess(partAccess, right);
            }
        }

        public bool HasAccess(Person person, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(person, partAccess, right); 
            }
        }

        public bool HasAccess(Organization organization, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(organization, partAccess, right);
            }
        }

        public bool HasAccess(Group group, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(group, partAccess, right);
            }
        }

        public Negotiator AccessDenied()
        {
            return View["View/info.sshtml", new AccessDeniedViewModel(Translator)];
        }

        private static Language? ConvertLocale(string locale)
        {
            if (locale.StartsWith("de", StringComparison.InvariantCulture))
            {
                return Language.German;
            }
            else if (locale.StartsWith("fr", StringComparison.InvariantCulture))
            {
                return Language.French;
            }
            else if (locale.StartsWith("it", StringComparison.InvariantCulture))
            {
                return Language.Italian;
            }
            else if (locale.StartsWith("en", StringComparison.InvariantCulture))
            {
                return Language.English;
            }
            else
            {
                return null; 
            }
        }

        public Translator Translator
        {
            get
            {
                return new Translator(Translation, CurrentLanguage);
            }
        }

        public Translator GetTranslator(Language language)
        {
            return new Translator(Translation, language);
        }

        public string Translate(string key, string hint, string technical, params object[] parameters)
        {
            return Translation.Get(CurrentLanguage, key, hint, technical, parameters);
        }

        protected void Journal(string subject, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translator = GetTranslator(person.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = subject;
            entry.Person.Value = person;
            Database.Save(entry);

            var technicalTranslator = GetTranslator(Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        protected void Journal(Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            Journal(CurrentSession.User, person, key, hint, technical, parameters);
        }

        protected void Journal(Person subject, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            Journal(subject.ShortHand, person, key, hint, technical, parameters);
        }

        public void Dispose()
        {
            if (Translation != null)
            {
                Translation = null;
            }

            if (Database != null)
            {
                Database.Dispose();
                Database = null; 
            }
        }

        public Language CurrentLanguage
        {
            get
            {
                if (CurrentSession != null)
                {
                    return CurrentSession.User.Language.Value;
                }
                else
                {
                    return BrowserLanguage;
                }
            }
        }

        public Language BrowserLanguage
        {
            get
            {
                var language = Request.Headers.AcceptLanguage
                    .Select(l => new Tuple<Language?, decimal>(ConvertLocale(l.Item1), l.Item2))
                    .Where(l => l.Item1 != null)
                    .OrderByDescending(l => l.Item2)
                    .Select(l => l.Item1)
                    .FirstOrDefault();

                if (language.HasValue)
                {
                    return language.Value;
                }
                else
                {
                    return Language.English;
                }
            }
        }

        protected PostStatus CreateStatus()
        {
            return new PostStatus(Database, Translator, CurrentSession);
        }
    }
}

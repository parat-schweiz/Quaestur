using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class PersonDetailSecurityItemViewModel
    {
        public string RowId;
        public string Phrase;
        public string Value;
        public string Path;
        public string Editable;

        public PersonDetailSecurityItemViewModel(
            string rowId,
            string phrase,
            string value,
            string path,
            bool editable)
        {
            RowId = rowId;
            Phrase = phrase.EscapeHtml();
            Value = value.EscapeHtml();
            Path = path;
            Editable = editable ? "editable" : "accessdenied";
        }
    }

    public class PersonDetailSecurityDeviceSessionViewModel
    {
        public string Id;
        public string Name;
        public string Created;
        public string LastAccess;
        public string Editable;

        public PersonDetailSecurityDeviceSessionViewModel(DeviceSession deviceSession, Session session)
        {
            Id = deviceSession.Id.Value.ToString();
            Name = deviceSession.Name.Value.ToString();
            Created = deviceSession.Created.Value.ToLocalTime().FormatSwissDateSeconds();
            LastAccess = deviceSession.LastAccess.Value.ToLocalTime().FormatSwissDateSeconds();
            var writeAccess = session.HasAccess(deviceSession.User.Value, PartAccess.Security, AccessRight.Write);
            Editable = writeAccess ? "editable" : string.Empty;
        }
    }

    public class PersonDetailSecurityViewModel
    {
        public string Id;
        public string PhraseSessionsHeaderName;
        public string PhraseSessionsHeaderCreated;
        public string PhraseSessionsHeaderLastAccess;
        public List<PersonDetailSecurityItemViewModel> List;
        public List<PersonDetailSecurityDeviceSessionViewModel> Sessions;

        public PersonDetailSecurityViewModel(Translator translator, IDatabase database, Session session, Person person)
        {
            PhraseSessionsHeaderName = translator.Get("Security.Sessions.Header.Name", "Name header in the session table in the personal security tab", "Name");
            PhraseSessionsHeaderCreated = translator.Get("Security.Sessions.Header.Created", "Created header in the session table in the personal security tab", "Created");
            PhraseSessionsHeaderLastAccess = translator.Get("Security.Sessions.Header.LastAccess", "Last access header in the session table in the personal security tab", "Last access");

            Id = person.Id.Value.ToString();
            List = new List<PersonDetailSecurityItemViewModel>();

            List.Add(new PersonDetailSecurityItemViewModel(
                "securityStatus",
                translator.Get("Security.Row.Status", "Status row title in the security tab of the person detail page", "Status"),
                person.UserStatus.Value.Translate(translator),
                "/user/status/edit/",
                session.HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                session.HasAllAccessOf(person)));

            List.Add(new PersonDetailSecurityItemViewModel(
                "securityPassword",
                translator.Get("Security.Row.Password", "Password row title in the security tab of the person detail page", "Password"),
                person.PasswordHash == null ?
                    translator.Get("Security.Password.None", "None value for the security tab of the person detail page", "Not set") :
                    translator.Get("Security.Password.Some", "Some value for the security tab of the person detail page", "************"),
                person == session.User ? "/password/change/" : "/password/set/",
                true));

            if (session.User == person)
            {
                List.Add(new PersonDetailSecurityItemViewModel(
                    "securityTwoFactor",
                    translator.Get("Security.Row.TwoFactor", "Two factor authentication row title in the security tab of the person detail page", "Two factor authentication"),
                    person.TwoFactorSecret.Value == null ?
                        translator.Get("Security.Row.TwoFactor.Disabled", "Two factor authentication disabled row value in the security tab of the person detail page", "Disabled") :
                        translator.Get("Security.Row.TwoFactor.Enabled", "Two factor authentication enabled row value in the security tab of the person detail page", "Enabled"),
                    "/twofactor/set/",
                    true));
            }
            else
            {
                List.Add(new PersonDetailSecurityItemViewModel(
                    "securityTwoFactor",
                    translator.Get("Security.Row.TwoFactor", "Two factor authentication row title in the security tab of the person detail page", "Two factor authentication"),
                    person.TwoFactorSecret.Value == null ?
                        translator.Get("Security.Row.TwoFactor.Disabled", "Two factor authentication disabled row value in the security tab of the person detail page", "Disabled") :
                        translator.Get("Security.Row.TwoFactor.Enabled", "Two factor authentication enabled row value in the security tab of the person detail page", "Enabled"),
                    "/twofactor/disable/",
                    session.HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                    session.HasAllAccessOf(person)));
            }

            Sessions = database
                .Query<DeviceSession>(DC.Equal("userid", person.Id.Value))
                .OrderByDescending(s => s.LastAccess.Value)
                .Select(s => new PersonDetailSecurityDeviceSessionViewModel(s, session))
                .ToList();
        }
    }

    public class PersonDetailSecurityModule : QuaesturModule
    {
        public PersonDetailSecurityModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/security/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Security, AccessRight.Read))
                    {
                        return View["View/persondetail_security.sshtml",
                            new PersonDetailSecurityViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
            Get("/session/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var deviceSession = Database.Query<DeviceSession>(idString);

                if ((deviceSession != null) &&
                    CurrentSession.HasAccess(deviceSession.User.Value, PartAccess.Security, AccessRight.Write))
                {
                    Global.Sessions.Remove(deviceSession);
                }
                return string.Empty;
            });
        }
    }
}

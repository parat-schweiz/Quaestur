using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using SiteLibrary;

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

    public class PersonDetailSecurityViewModel
    {
        public string Id;
        public List<PersonDetailSecurityItemViewModel> List;

        public PersonDetailSecurityViewModel(Translator translator, IDatabase database, Session session, Person person)
        {
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

                return null;
            });
        }
    }
}

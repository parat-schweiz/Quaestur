using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class UserStatusEditViewModel : DialogViewModel
    {
        public string Id;
        public string UserStatus;
        public List<NamedIntViewModel> UserStatuses;
        public string PhraseFieldUserStatus;

        public UserStatusEditViewModel()
        { 
        }

        public UserStatusEditViewModel(Translator translator, Person person)
            : base(translator, 
                   translator.Get("User.Status.Edit.Title", "Title of the user status edit dialog", "Edit user status"),
                   "editDialog")
        {
            Id = person.Id.Value.ToString();
            UserStatus = string.Empty;
            UserStatuses = new List<NamedIntViewModel>();
            UserStatuses.Add(new NamedIntViewModel(translator, Quaestur.UserStatus.Active, person.UserStatus.Value == Quaestur.UserStatus.Active));
            UserStatuses.Add(new NamedIntViewModel(translator, Quaestur.UserStatus.Locked, person.UserStatus.Value == Quaestur.UserStatus.Locked));
            PhraseFieldUserStatus = translator.Get("User.Status.Edit.Field.UserStatus", "Field 'User status' in the edit user status dialog", "User status").EscapeHtml();
        }
    }

    public class UserModule : QuaesturModule
    {
        public UserModule()
        {
            RequireCompleteLogin();

            Get("/user/status/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                        HasAllAccessOf(person))
                    {
                        return View["View/userstatusedit.sshtml",
                            new UserStatusEditViewModel(Translator, person)];
                    }
                }

                return null;
            });
            Post("/user/status/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<UserStatusEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                        status.HasAllAccessOf(person))
                    {
                        status.AssignEnumIntString("UserStatus", person.UserStatus, model.UserStatus);

                        if (status.IsSuccess)
                        {
                            Database.Save(person);
                            Journal(person,
                                "User.Status.Journal.Edit",
                                "Journal entry updated user status",
                                "Updated status");
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

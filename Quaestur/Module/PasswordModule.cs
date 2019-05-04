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
    public class PasswordEditViewModel : DialogViewModel
    {
        public string Id;
        public string CurrentPassword;
        public string NewPassword1;
        public string NewPassword2;
        public string PhraseFieldCurrentPassword;
        public string PhraseFieldNewPassword1;
        public string PhraseFieldNewPassword2;
        public string PhraseButtonBack;
        public string PhraseButtonChange;
        public bool Change;

        public PasswordEditViewModel()
        { 
        }

        public PasswordEditViewModel(Translator translator, Person person, bool change)
            : base(translator, 
                   change ?
                   translator.Get("Password.Edit.Title.Change", "Title of the change password dialog", "Change password") :
                   translator.Get("Password.Edit.Title.Set", "Title of the set password dialog", "Set password"),
                   "editDialog")
        {
            Id = person.Id.Value.ToString();
            Change = change;
            PhraseFieldCurrentPassword = translator.Get("Password.Edit.Field.CurrentPassword", "Field 'Current password' in the edit password dialog", "Current password").EscapeHtml();
            PhraseFieldNewPassword1 = translator.Get("Password.Edit.Field.NewPassword1", "Field 'New password' in the edit password dialog", "New password").EscapeHtml();
            PhraseFieldNewPassword2 = translator.Get("Password.Edit.Field.NewPassword2", "Field 'Repeat password' in the edit password dialog", "Repeat password").EscapeHtml();
            PhraseButtonBack = translator.Get("Password.Edit.Buttton.Back", "Back button in the edit password dialog", "Back").EscapeHtml();
            PhraseButtonChange = translator.Get("Password.Edit.Button.Change", "Change password button in the edit password dialog", "Change").EscapeHtml();
            CurrentPassword = string.Empty;
            NewPassword1 = string.Empty;
            NewPassword2 = string.Empty;
        }
    }

    public class PasswordModule : QuaesturModule
    {
        public PasswordModule()
        {
            RequireCompleteLogin();

            Get["/password/set/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                        HasAllAccessOf(person))
                    {
                        return View["View/passwordset.sshtml",
                            new PasswordEditViewModel(Translator, person, false)];
                    }
                }

                return null;
            };
            Post["/password/set/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PasswordEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Security, AccessRight.Write) &&
                        status.HasAllAccessOf(person))
                    {
                        if (model.NewPassword1 != model.NewPassword2)
                        {
                            status.SetValidationError("NewPassword2", "Password.Edit.Validation.NotEqual", "Message when new passwords are not equal at change/set", "New passwords are not equal");
                        }
                        else if (model.NewPassword1.Length < 12)
                        {
                            status.SetValidationError("NewPassword1", "Password.Edit.Validation.TooShort", "Message when new password is to short at change/set", "New password must be at least 12 characters long");
                        }

                        if (status.IsSuccess)
                        {
                            person.PasswordHash.Value = Global.Security.SecurePassword(model.NewPassword1);
                            Database.Save(person);
                            Journal(person,
                                "Password.Journal.Edit",
                                "Journal entry set password",
                                "Set password");
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/password"] = parameters =>
            {
                return View["View/password.sshtml",
                    new PasswordEditViewModel(Translator, CurrentSession.User, true)];
            };
            Get["/password/change/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person == CurrentSession.User)
                {
                    return View["View/passwordchange.sshtml",
                        new PasswordEditViewModel(Translator, person, true)];
                }

                return null;
            };
            Post["/password/change/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PasswordEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (person == CurrentSession.User)
                {
                    if (!UserController.VerifyHash(person.PasswordHash, model.CurrentPassword))
                    {
                        status.SetValidationError("CurrentPassword", "Password.Edit.Validation.CurrentWrong", "Message when current password is wrong at password change", "Current password is wrong");
                    }
                    else if (model.NewPassword1 != model.NewPassword2)
                    {
                        status.SetValidationError("NewPassword2", "Password.Edit.Validation.NotEqual", "Message when new passwords are not equal at password change/set", "New passwords do not match");
                    }
                    else if (model.NewPassword1.Length < 12)
                    {
                        status.SetValidationError("NewPassword1", "Password.Edit.Validation.TooShort", "Message when new password is to short at password change/set", "New password must be at least 12 characters long");
                    }

                    if (status.IsSuccess)
                    {
                        person.PasswordHash.Value = Global.Security.SecurePassword(model.NewPassword1);
                        person.PasswordType.Value = PasswordType.SecurityService;
                        Database.Save(person);
                        Journal(person,
                            "Password.Journal.Edit",
                            "Journal entry changed password",
                            "Changed password");
                    }
                }
                else
                {
                    status.SetErrorAccessDenied();
                }

                return status.CreateJsonData();
            };
        }
    }
}

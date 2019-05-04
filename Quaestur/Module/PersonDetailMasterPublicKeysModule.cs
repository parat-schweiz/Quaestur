using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDetailPublicKeyItemViewModel
    {
        public string Id;
        public string Type;
        public string KeyId;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailPublicKeyItemViewModel(Translator translator, PublicKey key)
        {
            Id = key.Id.ToString();
            Type = key.Type.Value.Translate(translator).EscapeHtml();
            KeyId = key.ShortKeyId.EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.PublicKeys.Delete.Confirm.Question", "Delete public key confirmation question", "Do you really wish to delete public key {0}?", key.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonDetailPublicKeysViewModel
    {
        public string Title;
        public string Id;
        public string Editable;
        public List<PersonDetailPublicKeyItemViewModel> List;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailPublicKeysViewModel(Translator translator, Session session, Person person)
        {
            Title = translator.Get("Person.Detail.PublicKeys.Title", "Title of the public keys part of the person detail page", "Public keys").EscapeHtml();
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailPublicKeyItemViewModel>(person.PublicKeys
                .Select(pk => new PersonDetailPublicKeyItemViewModel(translator, pk))
                .OrderBy(pk => pk.Type + "/" + pk.KeyId));
            Editable =
                (session.HasAccess(person, PartAccess.Contact, AccessRight.Write) &&
                session.HasAllAccessOf(person)) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.PublicKeys.Delete.Confirm.Title", "Delete public key confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class PersonDetailMasterPublicKeysModule : QuaesturModule
    {
        public PersonDetailMasterPublicKeysModule()
        {
            RequireCompleteLogin();

            Get["/person/detail/master/publickeys/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/persondetail_master_publickeys.sshtml", 
                            new PersonDetailPublicKeysViewModel(Translator, CurrentSession, person)];
                    }
                }

                return null;
            };
        }
    }
}

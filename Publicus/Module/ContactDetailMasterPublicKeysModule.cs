using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailPublicKeyItemViewModel
    {
        public string Id;
        public string Type;
        public string KeyId;
        public string PhraseDeleteConfirmationQuestion;

        public ContactDetailPublicKeyItemViewModel(Translator translator, PublicKey key)
        {
            Id = key.Id.ToString();
            Type = key.Type.Value.Translate(translator).EscapeHtml();
            KeyId = key.ShortKeyId.EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Contact.Detail.Master.PublicKeys.Delete.Confirm.Question", "Delete public key confirmation question", "Do you really wish to delete public key {0}?", key.GetText(translator)).EscapeHtml();
        }
    }

    public class ContactDetailPublicKeysViewModel
    {
        public string Title;
        public string Id;
        public string Editable;
        public List<ContactDetailPublicKeyItemViewModel> List;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public ContactDetailPublicKeysViewModel(Translator translator, Session session, Contact contact)
        {
            Title = translator.Get("Contact.Detail.PublicKeys.Title", "Title of the public keys part of the contact detail page", "Public keys").EscapeHtml();
            Id = contact.Id.Value.ToString();
            List = new List<ContactDetailPublicKeyItemViewModel>(contact.PublicKeys
                .Select(pk => new ContactDetailPublicKeyItemViewModel(translator, pk))
                .OrderBy(pk => pk.Type + "/" + pk.KeyId));
            Editable =
                session.HasAccess(contact, PartAccess.Contact, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationTitle = translator.Get("Contact.Detail.Master.PublicKeys.Delete.Confirm.Title", "Delete public key confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class ContactDetailMasterPublicKeysModule : PublicusModule
    {
        public ContactDetailMasterPublicKeysModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/master/publickeys/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/contactdetail_master_publickeys.sshtml", 
                            new ContactDetailPublicKeysViewModel(Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

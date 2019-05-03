using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailDeleteItemViewModel
    {
        public string RowId;
        public string Phrase;
        public string PhraseConfirmationTitle;
        public string PhraseConfirmationQuestion;
        public string Path;

        public ContactDetailDeleteItemViewModel(
            string rowId, 
            string phrase, 
            string phraseConfirmationTitle,
            string phraseConfirmationQuestion,
            string path)
        {
            RowId = rowId;
            Phrase = phrase.EscapeHtml();
            PhraseConfirmationTitle = phraseConfirmationTitle.EscapeHtml();
            PhraseConfirmationQuestion = phraseConfirmationQuestion.EscapeHtml();
            Path = path;
        }
    }

    public class ContactDetailDeleteViewModel
    {
        public string Title;
        public string Id;
        public List<ContactDetailDeleteItemViewModel> List;

        public ContactDetailDeleteViewModel(Translator translator, Session session, Contact contact)
        {
            Title = translator.Get("Contact.Detail.Delete.Title", "Title of the delete part of the contact detail page", "Delete").EscapeHtml();
            Id = contact.Id.Value.ToString();
            List = new List<ContactDetailDeleteItemViewModel>();

            if (!contact.Deleted &&
                session.HasAccess(contact, PartAccess.Contact, AccessRight.Write) &&
                (contact != session.User))
            {
                List.Add(new ContactDetailDeleteItemViewModel(
                    "contactDeleteMark",
                    translator.Get("Contact.Detail.Delete.Mark", "Mark as delete in the contact master data tab in the contact detail page", "Mark as deleted"),
                    translator.Get("Contact.Detail.Delete.Mark.Confirmation.Title", "Confirmation title when mark as delete in the contact master data tab in the contact detail page", "Mark as delete?"),
                    translator.Get("Contact.Detail.Delete.Mark.Confirmation.Question", "Confirmation question when mark as delete in the contact master data tab in the contact detail page", "Do you really wish to mark this contact as deleted?"),
                    "/contact/delete/mark/" + contact.Id.Value.ToString()));
            }

            if (contact.Deleted &&
                session.HasAccess(contact, PartAccess.Deleted, AccessRight.Write) &&
                (contact != session.User))
            {
                List.Add(new ContactDetailDeleteItemViewModel(
                    "contactDeleteUnmark",
                    translator.Get("Contact.Detail.Delete.Unmark", "Unmark as delete in the contact master data tab in the contact detail page", "Undelete"),
                    translator.Get("Contact.Detail.Delete.Unmark.Confirmation.Title", "Confirmation title when unmark as delete in the contact master data tab in the contact detail page", "Undelete?"),
                    translator.Get("Contact.Detail.Delete.Unmark.Confirmation.Question", "Confirmation question when unmark as delete in the contact master data tab in the contact detail page", "Do you really wish to undelete this contact?"),
                    "/contact/delete/unmark/" + contact.Id.Value.ToString()));
            }

            if (contact.Deleted &&
                session.HasAccess(contact, PartAccess.Deleted, AccessRight.Write) &&
                (contact != session.User))
            {
                List.Add(new ContactDetailDeleteItemViewModel(
                    "contactDeleteHard",
                    translator.Get("Contact.Detail.Delete.Hard", "Hard delete in the contact master data tab in the contact detail page", "Delete from database"),
                    translator.Get("Contact.Detail.Delete.Hard.Confirmation.Title", "Confirmation title when hard delete in the contact master data tab in the contact detail page", "Delete from database?"),
                    translator.Get("Contact.Detail.Delete.Hard.Confirmation.Question", "Confirmation question when hard delete in the contact master data tab in the contact detail page", "Do you wish to delete this contact from the database? This action cannot be undone."),
                    "/contact/delete/hard/" + contact.Id.Value.ToString()));
            }
        }
    }

    public class ContactDetailDeleteModule : PublicusModule
    {
        public ContactDetailDeleteModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/master/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/contactdetail_master_delete.sshtml", 
                            new ContactDetailDeleteViewModel(Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

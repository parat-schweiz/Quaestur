using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailDocumentItemViewModel
    {
        public string Id;
        public string Type;
        public string CreatedDate;
        public string FileName;
        public string PhraseDeleteConfirmationQuestion;

        public ContactDetailDocumentItemViewModel(Translator translator, Document document)
        {
            Id = document.Id.Value.ToString();
            Type = document.Type.Value.Translate(translator).EscapeHtml();
            CreatedDate = document.CreatedDate.Value.ToString("dd.MM.yyyy");
            FileName = document.FileName.Value.EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Contact.Detail.Master.Documents.Delete.Confirm.Question", "Delete document confirmation question", "Do you really wish to delete document {0}?", document.GetText(translator)).EscapeHtml();
        }
    }

    public class ContactDetailDocumentViewModel
    {
        public string Id;
        public string Editable;
        public List<ContactDetailDocumentItemViewModel> List;
        public string PhraseHeaderFile;
        public string PhraseHeaderType;
        public string PhraseHeaderCreatedDate;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public ContactDetailDocumentViewModel(Translator translator, IDatabase database, Session session, Contact contact)
        {
            Id = contact.Id.Value.ToString();
            List = new List<ContactDetailDocumentItemViewModel>(database
                .Query<Document>(DC.Equal("contactid", contact.Id.Value))
                .OrderBy(d => d.CreatedDate.Value)
                .Select(d => new ContactDetailDocumentItemViewModel(translator, d)));
            Editable =
                session.HasAccess(contact, PartAccess.TagAssignments, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderFile = translator.Get("Contact.Detail.Document.Header.File", "Column 'File' on the document tab of the contact detail page", "File").EscapeHtml();
            PhraseHeaderType = translator.Get("Contact.Detail.Document.Header.Type", "Column 'Type' on the document tab of the contact detail page", "Type").EscapeHtml();
            PhraseHeaderCreatedDate = translator.Get("Contact.Detail.Document.Header.CreatedDate", "Column 'CreatedDate' on the document tab of the contact detail page", "Created").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Contact.Detail.Master.Document.Delete.Confirm.Title", "Delete document confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class ContactDetailDocumentModule : PublicusModule
    {
        public ContactDetailDocumentModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/documents/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Documents, AccessRight.Read))
                    {
                        return View["View/contactdetail_documents.sshtml", 
                            new ContactDetailDocumentViewModel(Translator, Database, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

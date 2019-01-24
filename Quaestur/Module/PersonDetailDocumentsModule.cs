using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class PersonDetailDocumentItemViewModel
    {
        public string Id;
        public string Type;
        public string CreatedDate;
        public string FileName;
        public string PhraseDeleteConfirmationQuestion;

        public PersonDetailDocumentItemViewModel(Translator translator, Document document)
        {
            Id = document.Id.Value.ToString();
            Type = document.Type.Value.Translate(translator).EscapeHtml();
            CreatedDate = document.CreatedDate.Value.ToString("dd.MM.yyyy");
            FileName = document.FileName.Value.EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.Documents.Delete.Confirm.Question", "Delete document confirmation question", "Do you really wish to delete document {0}?", document.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonDetailDocumentViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailDocumentItemViewModel> List;
        public string PhraseHeaderFile;
        public string PhraseHeaderType;
        public string PhraseHeaderCreatedDate;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PersonDetailDocumentViewModel(Translator translator, IDatabase database, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailDocumentItemViewModel>(database
                .Query<Document>(DC.Equal("personid", person.Id.Value))
                .OrderBy(d => d.CreatedDate.Value)
                .Select(d => new PersonDetailDocumentItemViewModel(translator, d)));
            Editable =
                session.HasAccess(person, PartAccess.TagAssignments, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderFile = translator.Get("Person.Detail.Document.Header.File", "Column 'File' on the document tab of the person detail page", "File").EscapeHtml();
            PhraseHeaderType = translator.Get("Person.Detail.Document.Header.Type", "Column 'Type' on the document tab of the person detail page", "Type").EscapeHtml();
            PhraseHeaderCreatedDate = translator.Get("Person.Detail.Document.Header.CreatedDate", "Column 'CreatedDate' on the document tab of the person detail page", "Created").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.Document.Delete.Confirm.Title", "Delete document confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class PersonDetailDocumentModule : QuaesturModule
    {
        public PersonDetailDocumentModule()
        {
            RequireCompleteLogin();

            Get["/person/detail/documents/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Documents, AccessRight.Read))
                    {
                        return View["View/persondetail_documents.sshtml", 
                            new PersonDetailDocumentViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return null;
            };
        }
    }
}

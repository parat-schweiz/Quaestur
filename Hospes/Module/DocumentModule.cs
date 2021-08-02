using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class DocumentEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Type;
        public string Verifier;
        public string CreatedDate;
        public string FileName;
        public string FilePath;
        public string FileSize;
        public string FileData;
        public List<NamedIntViewModel> Types;
        public List<NamedIdViewModel> Verifiers;
        public string PhraseFieldType;
        public string PhraseFieldVerifier;
        public string PhraseFieldCreatedDate;
        public string PhraseFieldFile;

        public DocumentEditViewModel()
        { 
        }

        public DocumentEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Document.Edit.Title", "Title of the edit document dialog", "Edit document"), 
                   "documentEditDialog")
        {
            PhraseFieldType = translator.Get("Document.Edit.Field.Type", "Field 'Type' in the edit document dialog", "Type").EscapeHtml();
            PhraseFieldVerifier = translator.Get("Document.Edit.Field.Verifier", "Field 'Verifier' in the edit document dialog", "Verifier").EscapeHtml();
            PhraseFieldCreatedDate = translator.Get("Document.Edit.Field.CreatedDate", "Field 'CreatedDate' in the edit document dialog", "CreatedDate").EscapeHtml();
            PhraseFieldFile = translator.Get("Document.Edit.Field.File", "Field 'File' in the edit document dialog", "File").EscapeHtml();
            Type = string.Empty;
            Verifier = string.Empty;
            FileData = string.Empty;
        }

        public DocumentEditViewModel(Translator translator, IDatabase db, Session session, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            CreatedDate = string.Empty;
            FileName = string.Empty;
            FilePath = string.Empty;
            FileSize = string.Empty;
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, DocumentType.Other, false));
            Types.Add(new NamedIntViewModel(translator, DocumentType.Verification, false));
            Verifiers = new List<NamedIdViewModel>(db
                .Query<Person>()
                .Where(p => session.HasAccess(p, PartAccess.Anonymous, AccessRight.Read))
                .Select(p => new NamedIdViewModel(session, p, false))
                .OrderBy(p => p.Name));
        }

        public DocumentEditViewModel(Translator translator, IDatabase db, Session session, Document document)
            : this(translator)
        {
            Method = "edit";
            Id = document.Id.ToString();
            CreatedDate = document.CreatedDate.Value.FormatSwissDateDay();
            FileName = document.FileName.Value.EscapeHtml();
            FilePath = "/document/download/" + document.Id.Value.ToString();
            FileSize = "(" + document.Data.Value.Length.SizeFormat() + ")";
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, DocumentType.Other, document.Type.Value == DocumentType.Other));
            Types.Add(new NamedIntViewModel(translator, DocumentType.Verification, document.Type.Value == DocumentType.Verification));
            Verifiers = new List<NamedIdViewModel>(db
                .Query<Person>()
                .Where(p => session.HasAccess(p, PartAccess.Anonymous, AccessRight.Read))
                .Select(p => new NamedIdViewModel(session, p, document.Verifier.Value == p))
                .OrderBy(p => p.Name));
        }
    }

    public class DocumentModule : QuaesturModule
    {
        public DocumentModule()
        {
            RequireCompleteLogin();

            Get("/document/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var document = Database.Query<Document>(idString);

                if (document != null)
                {
                    if (HasAccess(document.Person.Value, PartAccess.Documents, AccessRight.Write))
                    {
                        return View["View/documentedit.sshtml",
                            new DocumentEditViewModel(Translator, Database, CurrentSession, document)];
                    }
                }

                return string.Empty;
            });
            Post("/document/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<DocumentEditViewModel>(ReadBody());
                var document = Database.Query<Document>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(document))
                {
                    if (status.HasAccess(document.Person.Value, PartAccess.Documents, AccessRight.Write))
                    {
                        status.AssignObjectIdString("Verifier", document.Verifier, model.Verifier);
                        status.AssignEnumIntString("Type", document.Type, model.Type);
                        status.AssignDateString("CreatedDate", document.CreatedDate, model.CreatedDate);
                        status.AssingDataUrlString("File", document.Data, document.ContentType, model.FileData, false);
                        status.AssignStringIfNotEmpty("FileName", document.FileName, model.FileName);

                        if (status.IsSuccess)
                        {
                            Database.Save(document);
                            Journal(document.Person.Value,
                                "Document.Journal.Edit",
                                "Journal entry edited document",
                                "Changed document {0}",
                                t => document.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/document/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Documents, AccessRight.Write))
                    {
                        return View["View/documentedit.sshtml",
                            new DocumentEditViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
            Post("/document/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<DocumentEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Documents, AccessRight.Write))
                    {
                        var document = new Document(Guid.NewGuid());
                        status.AssignObjectIdString("Verifier", document.Verifier, model.Verifier);
                        status.AssignEnumIntString("Type", document.Type, model.Type);
                        status.AssingDataUrlString("File", document.Data, document.ContentType, model.FileData, true);
                        status.AssignDateString("CreatedDate", document.CreatedDate, model.CreatedDate);
                        status.AssignStringRequired("FileName", document.FileName, model.FileName);
                        document.Person.Value = person;

                        if (status.IsSuccess)
                        {
                            Database.Save(document);
                            Journal(document.Person,
                                "Document.Journal.Add",
                                "Journal entry added document",
                                "Added document {0}",
                                t => document.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/document/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var document = Database.Query<Document>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(document))
                {
                    if (status.HasAccess(document.Person.Value, PartAccess.Documents, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            document.Delete(Database);

                            Journal(document.Person,
                                "Document.Journal.Delete",
                                "Journal entry deleted document",
                                "Deleted document {0}",
                                t => document.GetText(t));

                            transaction.Commit();
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/document/download/{id}", parameters =>
            {
                string idString = parameters.id;
                var document = Database.Query<Document>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(document))
                {
                    if (status.HasAccess(document.Person.Value, PartAccess.Documents, AccessRight.Read))
                    {
                        var stream = new MemoryStream(document.Data);
                        var response = new StreamResponse(() => stream, document.ContentType.Value);
                        Journal(document.Person,
                            "Document.Journal.Download",
                            "Journal entry downloaded document",
                            "Downloaded document {0}",
                            t => document.GetText(t));
                        return response.AsAttachment(document.FileName.Value);
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

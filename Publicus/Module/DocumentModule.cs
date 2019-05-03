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

namespace Publicus
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

        public DocumentEditViewModel(Translator translator, IDatabase db, Session session, Contact contact)
            : this(translator)
        {
            Method = "add";
            Id = contact.Id.ToString();
            CreatedDate = string.Empty;
            FileName = string.Empty;
            FilePath = string.Empty;
            FileSize = string.Empty;
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, DocumentType.Other, false));
            Types.Add(new NamedIntViewModel(translator, DocumentType.Verification, false));
            Verifiers = new List<NamedIdViewModel>(db
                .Query<Contact>()
                .Where(p => session.HasAccess(p, PartAccess.Anonymous, AccessRight.Read))
                .Select(p => new NamedIdViewModel(session, p, false))
                .OrderBy(p => p.Name));
        }

        public DocumentEditViewModel(Translator translator, IDatabase db, Session session, Document document)
            : this(translator)
        {
            Method = "edit";
            Id = document.Id.ToString();
            CreatedDate = document.CreatedDate.Value.ToString("dd.MM.yyyy");
            FileName = document.FileName.Value.EscapeHtml();
            FilePath = "/document/download/" + document.Id.Value.ToString();
            FileSize = "(" + document.Data.Value.Length.SizeFormat() + ")";
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, DocumentType.Other, document.Type.Value == DocumentType.Other));
            Types.Add(new NamedIntViewModel(translator, DocumentType.Verification, document.Type.Value == DocumentType.Verification));
            Verifiers = new List<NamedIdViewModel>(db
                .Query<Contact>()
                .Where(p => session.HasAccess(p, PartAccess.Anonymous, AccessRight.Read))
                .Select(p => new NamedIdViewModel(session, p, document.Verifier.Value == p))
                .OrderBy(p => p.Name));
        }
    }

    public class DocumentModule : PublicusModule
    {
        public DocumentModule()
        {
            this.RequiresAuthentication();

            Get["/document/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var document = Database.Query<Document>(idString);

                if (document != null)
                {
                    if (HasAccess(document.Contact.Value, PartAccess.Documents, AccessRight.Write))
                    {
                        return View["View/documentedit.sshtml",
                            new DocumentEditViewModel(Translator, Database, CurrentSession, document)];
                    }
                }

                return null;
            };
            Post["/document/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<DocumentEditViewModel>(ReadBody());
                var document = Database.Query<Document>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(document))
                {
                    if (status.HasAccess(document.Contact.Value, PartAccess.Documents, AccessRight.Write))
                    {
                        status.AssignObjectIdString("Verifier", document.Verifier, model.Verifier);
                        status.AssignEnumIntString("Type", document.Type, model.Type);
                        status.AssignDateString("CreatedDate", document.CreatedDate, model.CreatedDate);
                        status.AssingDataUrlString("File", document.Data, document.ContentType, model.FileData, false);
                        status.AssignStringIfNotEmpty("FileName", document.FileName, model.FileName);

                        if (status.IsSuccess)
                        {
                            Database.Save(document);
                            Journal(document.Contact.Value,
                                "Document.Journal.Edit",
                                "Journal entry edited document",
                                "Changed document {0}",
                                t => document.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/document/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Documents, AccessRight.Write))
                    {
                        return View["View/documentedit.sshtml",
                            new DocumentEditViewModel(Translator, Database, CurrentSession, contact)];
                    }
                }

                return null;
            };
            Post["/document/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<DocumentEditViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    if (status.HasAccess(contact, PartAccess.Documents, AccessRight.Write))
                    {
                        var document = new Document(Guid.NewGuid());
                        status.AssignObjectIdString("Verifier", document.Verifier, model.Verifier);
                        status.AssignEnumIntString("Type", document.Type, model.Type);
                        status.AssingDataUrlString("File", document.Data, document.ContentType, model.FileData, true);
                        status.AssignDateString("CreatedDate", document.CreatedDate, model.CreatedDate);
                        status.AssignStringRequired("FileName", document.FileName, model.FileName);
                        document.Contact.Value = contact;

                        if (status.IsSuccess)
                        {
                            Database.Save(document);
                            Journal(document.Contact,
                                "Document.Journal.Add",
                                "Journal entry added document",
                                "Added document {0}",
                                t => document.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/document/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var document = Database.Query<Document>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(document))
                {
                    if (status.HasAccess(document.Contact.Value, PartAccess.Documents, AccessRight.Write))
                    {
                        document.Delete(Database);
                        Journal(document.Contact,
                            "Document.Journal.Delete",
                            "Journal entry deleted document",
                            "Deleted document {0}",
                            t => document.GetText(t));
                    }
                }

                return status.CreateJsonData();
            };
            Get["/document/download/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var document = Database.Query<Document>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(document))
                {
                    if (status.HasAccess(document.Contact.Value, PartAccess.Documents, AccessRight.Read))
                    {
                        var stream = new MemoryStream(document.Data);
                        var response = new StreamResponse(() => stream, document.ContentType.Value);
                        Journal(document.Contact,
                            "Document.Journal.Download",
                            "Journal entry downloaded document",
                            "Downloaded document {0}",
                            t => document.GetText(t));
                        return response.AsAttachment(document.FileName.Value);
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

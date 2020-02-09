using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Nancy.Responses;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class SystemWideFileEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Type;
        public List<NamedIntViewModel> Types;
        public string FileName;
        public string FilePath;
        public string FileSize;
        public string FileData;
        public string PhraseFieldType;
        public string PhraseFieldFile;

        public SystemWideFileEditViewModel()
        {
        }

        public SystemWideFileEditViewModel(Translator translator)
            : base(translator, translator.Get("SystemWideFile.Edit.Title", "Title of the system wide file edit dialog", "Edit system wide file"), "systemWideFileEditDialog")
        {
            PhraseFieldType = translator.Get("SystemWideFile.Edit.Field.Type", "Field 'Type' in the system wide file edit dialog", "Type").EscapeHtml();
            PhraseFieldFile = translator.Get("SystemWideFile.Edit.Field.File", "Field 'File' in the system wide file edit dialog", "File").EscapeHtml();
            FileSize = string.Empty;
        }

        private List<NamedIntViewModel> CreateTypes(Translator translator, SystemWideFileType? selected, IEnumerable<SystemWideFileType> others)
        {
            var allTypes = new SystemWideFileType[]
            {
                SystemWideFileType.HeaderImage,
                SystemWideFileType.Favicon
            };
            return new List<NamedIntViewModel>(allTypes
                .Where(t => !others.Contains(t))
                .Select(t => new NamedIntViewModel(translator, t, selected == t)));
        }

        public SystemWideFileEditViewModel(Translator translator, IDatabase database, IEnumerable<SystemWideFileType> others)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Types = CreateTypes(translator, null, others);
            FileName = string.Empty;
            FilePath = string.Empty;
            FileSize = string.Empty;
        }

        public SystemWideFileEditViewModel(Translator translator, IDatabase database, IEnumerable<SystemWideFileType> others, SystemWideFile systemWideFile)
            : this(translator)
        {
            Method = "edit";
            Id = systemWideFile.Id.ToString();
            Types = CreateTypes(translator, systemWideFile.Type.Value, others);
            FileName = systemWideFile.FileName.Value.EscapeHtml();
            FilePath = "/systemwidefile/download/" + systemWideFile.Id.Value.ToString();
            FileSize = "(" + systemWideFile.Data.Value.Length.SizeFormat() + ")";
        }
    }

    public class SystemWideFileViewModel : MasterViewModel
    {
        public SystemWideFileViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("SystemWideFile.List.Title", "Title of the system wide file list page", "Countries"), 
            session)
        { 
        }
    }

    public class SystemWideFileListItemViewModel
    {
        public string Id;
        public string Type;
        public string FileName;
        public string FilePath;
        public string PhraseDeleteConfirmationQuestion;

        public SystemWideFileListItemViewModel(Translator translator, SystemWideFile systemWideFile)
        {
            Id = systemWideFile.Id.Value.ToString();
            Type = systemWideFile.Type.Value.Translate(translator).EscapeHtml();
            FileName = systemWideFile.FileName.Value.EscapeHtml();
            FilePath = "/systemwidefile/download/" + systemWideFile.Id.Value.ToString();
            PhraseDeleteConfirmationQuestion = translator.Get("SystemWideFile.List.Delete.Confirm.Question", "Delete system wide file confirmation question", "Do you really wish to delete system wide file {0}?", systemWideFile.GetText(translator));
        }
    }

    public class SystemWideFileListViewModel
    {
        public string PhraseHeaderType;
        public string PhraseHeaderFileName;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<SystemWideFileListItemViewModel> List;

        public SystemWideFileListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderType = translator.Get("SystemWideFile.List.Header.Type", "Column 'Type' in the system wide file list", "Type").EscapeHtml();
            PhraseHeaderFileName = translator.Get("SystemWideFile.List.Header.FileName", "Column 'File' in the system wide file list", "File").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("SystemWideFile.List.Delete.Confirm.Title", "Delete system wide file confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
            List = new List<SystemWideFileListItemViewModel>(
                database.Query<SystemWideFile>()
                .Select(c => new SystemWideFileListItemViewModel(translator, c)));
        }
    }

    public class SystemWideFileEdit : QuaesturModule
    {
        public SystemWideFileEdit()
        {
            RequireCompleteLogin();

            Get("/systemwidefile", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/systemwidefile.sshtml",
                        new SystemWideFileViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/systemwidefile/list", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/systemwidefilelist.sshtml",
                        new SystemWideFileListViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Get("/systemwidefile/edit/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var systemWideFile = Database.Query<SystemWideFile>(idString);

                    if (systemWideFile != null)
                    {
                        var others = Database.Query<SystemWideFile>()
                            .Where(f => f != systemWideFile)
                            .Select(f => f.Type.Value);
                        return View["View/systemwidefileedit.sshtml",
                            new SystemWideFileEditViewModel(Translator, Database, others, systemWideFile)];
                    }
                }
                return string.Empty;
            });
            Post("/systemwidefile/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<SystemWideFileEditViewModel>(ReadBody());
                    var systemWideFile = Database.Query<SystemWideFile>(idString);

                    if (status.ObjectNotNull(systemWideFile))
                    {
                        status.AssignEnumIntString("Type", systemWideFile.Type, model.Type);
                        status.AssingDataUrlString("File", systemWideFile.Data, systemWideFile.ContentType, model.FileData, true);
                        status.AssignStringIfNotEmpty("FileName", systemWideFile.FileName, model.FileName);

                        if (status.IsSuccess)
                        {
                            var others = Database.Query<SystemWideFile>()
                            .Where(f => f != systemWideFile)
                            .Select(f => f.Type.Value);
                            if (others.Contains(systemWideFile.Type.Value))
                            {
                                status.SetValidationError(
                                    "Type",
                                    "SystemWideFile.Edit.Validation.Duplicate",
                                    "Duplicate type in system wide file edit",
                                    "Duplicate type");
                            }
                        }

                        if (status.IsSuccess)
                        {
                            Database.Save(systemWideFile);
                            Notice("{0} changed system wide file {1}", CurrentSession.User.ShortHand, systemWideFile);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/systemwidefile/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    var others = Database.Query<SystemWideFile>()
                        .Select(f => f.Type.Value);
                    return View["View/systemwidefileedit.sshtml",
                        new SystemWideFileEditViewModel(Translator, Database, others)];
                }
                return string.Empty;
            });
            Post("/systemwidefile/add/new", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<SystemWideFileEditViewModel>(ReadBody());
                    var systemWideFile = new SystemWideFile(Guid.NewGuid());
                    status.AssignEnumIntString("Type", systemWideFile.Type, model.Type);
                    status.AssingDataUrlString("File", systemWideFile.Data, systemWideFile.ContentType, model.FileData, true);
                    status.AssignStringIfNotEmpty("FileName", systemWideFile.FileName, model.FileName);

                    if (status.IsSuccess)
                    {
                        var others = Database.Query<SystemWideFile>()
                            .Where(f => f != systemWideFile)
                            .Select(f => f.Type.Value);
                        if (others.Contains(systemWideFile.Type.Value))
                        {
                            status.SetValidationError(
                                "Type",
                                "SystemWideFile.Edit.Validation.Duplicate",
                                "Duplicate type in system wide file edit",
                                "Duplicate type");
                        }
                    }

                    if (status.IsSuccess)
                    {
                        Database.Save(systemWideFile);
                        Notice("{0} added system wide file {1}", CurrentSession.User.ShortHand, systemWideFile);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/systemwidefile/delete/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var systemWideFile = Database.Query<SystemWideFile>(idString);

                    if (systemWideFile != null)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            systemWideFile.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted system wide file {1}", CurrentSession.User.ShortHand, systemWideFile);
                        }
                    }
                }
                return string.Empty;
            });
            Get("/systemwidefile/download/{id}", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var systemWideFile = Database.Query<SystemWideFile>(idString);

                    if (status.ObjectNotNull(systemWideFile))
                    {
                        var stream = new MemoryStream(systemWideFile.Data);
                        var response = new StreamResponse(() => stream, systemWideFile.ContentType.Value);
                        Notice("{0} downloaded system wide file {1}", CurrentSession.User.ShortHand, systemWideFile);
                        return response.AsAttachment(systemWideFile.FileName.Value);
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

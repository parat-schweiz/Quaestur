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

        public SystemWideFileEditViewModel()
        {
            FileData = string.Empty;
        }

        public SystemWideFileEditViewModel(Translator translator)
            : base(translator, translator.Get("SystemWideFile.Edit.Title", "Title of the system wide file edit dialog", "Edit system wide file"), "systemWideFileEditDialog")
        {
            FileSize = string.Empty;
        }

        public SystemWideFileEditViewModel(Translator translator, IDatabase database)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, SystemWideFileType.HeaderImage, false));
            Types.Add(new NamedIntViewModel(translator, SystemWideFileType.Favicon, false));
            FileName = string.Empty;
            FilePath = string.Empty;
            FileSize = string.Empty;
        }

        public SystemWideFileEditViewModel(Translator translator, IDatabase database, SystemWideFile systemWideFile)
            : this(translator)
        {
            Method = "edit";
            Id = systemWideFile.Id.ToString();
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, SystemWideFileType.HeaderImage, SystemWideFileType.HeaderImage == systemWideFile.Type.Value));
            Types.Add(new NamedIntViewModel(translator, SystemWideFileType.Favicon, SystemWideFileType.Favicon == systemWideFile.Type.Value));
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
        public string Name;
        public string FileName;
        public string FilePath;
        public string PhraseDeleteConfirmationQuestion;

        public SystemWideFileListItemViewModel(Translator translator, SystemWideFile systemWideFile)
        {
            Id = systemWideFile.Id.Value.ToString();
            Name = systemWideFile.Type.Value.Translate(translator).EscapeHtml();
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
                        return View["View/systemwidefileedit.sshtml",
                            new SystemWideFileEditViewModel(Translator, Database, systemWideFile)];
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
                        status.AssingDataUrlString("File", systemWideFile.Data, systemWideFile.ContentType, model.FileData, false);
                        status.AssignStringIfNotEmpty("FileName", systemWideFile.FileName, model.FileName);

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
                    return View["View/systemwidefileedit.sshtml",
                        new SystemWideFileEditViewModel(Translator, Database)];
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
                    status.AssingDataUrlString("File", systemWideFile.Data, systemWideFile.ContentType, model.FileData, false);
                    status.AssignStringIfNotEmpty("FileName", systemWideFile.FileName, model.FileName);

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

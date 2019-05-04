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

namespace Quaestur
{
    public class ExportEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Name;
        public string SelectOrganization;
        public string SelectTag;
        public string SelectLanguage;
        public string[] ExportColumns;
        public List<NamedIdViewModel> Organizations;
        public List<NamedIdViewModel> Tags;
        public List<NamedIntViewModel> Languages;
        public List<NamedStringViewModel> Columns;
        public string PhraseFieldName;
        public string PhraseFieldSelectOrganization;
        public string PhraseFieldSelectTag;
        public string PhraseFieldSelectLanguage;
        public string PhraseFieldExportColumns;

        public ExportEditViewModel()
        {
        }

        public ExportEditViewModel(Translator translator)
            : base(translator,
                   translator.Get("Export.Edit.Title", "Title of the edit export dialog", "Edit export"),
                   "exportEditDialog")
        {
            PhraseFieldName = translator.Get("Export.Edit.Field.Name", "Field 'Name' in the edit export dialog", "Name").EscapeHtml();
            PhraseFieldSelectOrganization = translator.Get("Export.Edit.Field.Select organization", "Field 'SelectOrganization' in the edit export dialog", "Select by Organization").EscapeHtml();
            PhraseFieldSelectTag = translator.Get("Export.Edit.Field.SelectTag", "Field 'Select tag' in the edit export dialog", "Select by Tag").EscapeHtml();
            PhraseFieldSelectLanguage = translator.Get("Export.Edit.Field.SelectLanguage", "Field 'Select language' in the edit export dialog", "Select by Language").EscapeHtml();
            PhraseFieldExportColumns = translator.Get("Export.Edit.Field.ExportColumns", "Field 'Export columns' in the edit export dialog", "Export columns").EscapeHtml();
        }

        public ExportEditViewModel(Translator translator, IDatabase db, Session session)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = string.Empty;
            SelectOrganization = string.Empty;
            SelectTag = string.Empty;
            SelectLanguage = string.Empty;
            Organizations = new List<NamedIdViewModel>(db
                .Query<Organization>()
                .Where(o => session.HasAccess(o, PartAccess.Demography, AccessRight.Read))
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
            if (session.HasSystemWideAccess(PartAccess.Demography, AccessRight.Read))
                Organizations.Add(new NamedIdViewModel(translator.Get("Export.Edit.Field.SelectOrganization.None", "No selection in the select organization field of the edit export page", "None"), false, true));
            Tags = new List<NamedIdViewModel>(db
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            Tags.Add(new NamedIdViewModel(translator.Get("Export.Edit.Field.SelectTag.None", "No selection in the select tag field of the edit export page", "None"), false, true));
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, Language.English, false));
            Languages.Add(new NamedIntViewModel(translator, Language.German, false));
            Languages.Add(new NamedIntViewModel(translator, Language.French, false));
            Languages.Add(new NamedIntViewModel(translator, Language.Italian, false));
            Languages.Add(new NamedIntViewModel(translator.Get("Export.Edit.Field.SelectLanguage.None", "No selection in the select language field of the edit export page", "None"), false, true));
            var columns = new ExportColumnManager(translator);
            Columns = new List<NamedStringViewModel>(columns.Columns.Select(c => new NamedStringViewModel(c.Id, c.Title, false)));
        }

        public ExportEditViewModel(Translator translator, IDatabase db, Session session, Export export)
            : this(translator)
        {
            Method = "edit";
            Id = export.Id.ToString();
            Name = export.Name.Value.EscapeHtml();
            SelectOrganization = string.Empty;
            SelectTag = string.Empty;
            SelectLanguage = string.Empty;
            Organizations = new List<NamedIdViewModel>(db
                .Query<Organization>()
                .Where(o => session.HasAccess(o, PartAccess.Demography, AccessRight.Read))
                .Select(o => new NamedIdViewModel(translator, o, o == export.SelectOrganization.Value))
                .OrderBy(o => o.Name));
            if (session.HasSystemWideAccess(PartAccess.Demography, AccessRight.Read))
                Organizations.Add(new NamedIdViewModel(translator.Get("Export.Edit.Field.SelectOrganization.None", "No selection in the select organization field of the edit export page", "None"), false, export.SelectOrganization.Value == null));
            Tags = new List<NamedIdViewModel>(db
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, t == export.SelectTag.Value))
                .OrderBy(t => t.Name));
            Tags.Add(new NamedIdViewModel(translator.Get("Export.Edit.Field.SelectTag.None", "No selection in the select tag field of the edit export page", "None"), false, export.SelectTag.Value == null));
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, Language.English, export.SelectLanguage.Value == Language.English));
            Languages.Add(new NamedIntViewModel(translator, Language.German, export.SelectLanguage.Value == Language.German));
            Languages.Add(new NamedIntViewModel(translator, Language.French, export.SelectLanguage.Value == Language.French));
            Languages.Add(new NamedIntViewModel(translator, Language.Italian, export.SelectLanguage.Value == Language.Italian));
            Languages.Add(new NamedIntViewModel(translator.Get("Export.Edit.Field.SelectLanguage.None", "No selection in the select language field of the edit export page", "None"), false, export.SelectLanguage.Value == null));
            var columns = new ExportColumnManager(translator);
            Columns = new List<NamedStringViewModel>(columns.Columns.Select(c => new NamedStringViewModel(c.Id, c.Title, false)));
        }
    }

    public class ExportViewModel : MasterViewModel
    {
        public ExportViewModel(Translator translator, Session session)
            : base(translator, 
                   translator.Get("Export.List.Title", "Title of the exports list page", "Exports"), 
                   session)
        { 
        }
    }

    public class ExportListItemViewModel
    {
        public string Id;
        public string Name;
        public string PhraseDeleteConfirmationQuestion;

        public ExportListItemViewModel(Translator translator, Export export)
        {
            Id = export.Id.Value.ToString();
            Name = export.Name.Value.EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Export.List.Delete.Confirm.Question", "Delete export confirmation question", "Do you really wish to delete export {0}?", export.GetText(translator)).EscapeHtml();
        }
    }

    public class ExportListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseExportDownload;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<ExportListItemViewModel> List;

        public ExportListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Export.List.Header.Name", "Column 'Name' in the export list page", "Name").EscapeHtml();
            PhraseExportDownload = translator.Get("Export.List.Link.Download", "Download link in the export list page", "Download").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Export.List.Delete.Confirm.Title", "Delete export confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Export.List.Delete.Confirm.Info", "Delete export confirmation info", "This will remove that export from all postal addresses.").EscapeHtml();
            List = new List<ExportListItemViewModel>(
                database.Query<Export>()
                .Select(c => new ExportListItemViewModel(translator, c))
                .OrderBy(c => c.Name));
        }
    }

    public class ExportEdit : QuaesturModule
    {
        public ExportEdit()
        {
            RequireCompleteLogin();

            Get["/export"] = parameters =>
            {
                if (HasAnyOrganizationAccess(PartAccess.Demography, AccessRight.Read))
                {
                    return View["View/export.sshtml",
                        new ExportViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            };
            Get["/export/list"] = parameters =>
            {
                if (HasAnyOrganizationAccess(PartAccess.Demography, AccessRight.Read))
                {
                    return View["View/exportlist.sshtml",
                        new ExportListViewModel(Translator, Database)];
                }
                return null;
            };
            Get["/export/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var export = Database.Query<Export>(idString);

                if (export != null)
                {
                    if (IsPermittedExport(export))
                    {
                        return View["View/exportedit.sshtml",
                            new ExportEditViewModel(Translator, Database, CurrentSession, export)];
                    }
                }
                return null;
            };
            Post["/export/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<ExportEditViewModel>(ReadBody());
                var export = Database.Query<Export>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(export))
                {
                    status.AssignStringRequired("Name", export.Name, model.Name);
                    status.AssignObjectIdString("SelectOrganization", export.SelectOrganization, model.SelectOrganization);
                    status.AssignObjectIdString("SelectTag", export.SelectTag, model.SelectTag);
                    status.AssignEnumIntString("SelectLanguage", export.SelectLanguage, model.SelectLanguage);
                    status.AssignStringList("ExportColumns", export.ExportColumns, model.ExportColumns);

                    if (status.IsSuccess)
                    {
                        if (IsPermittedExport(export))
                        {
                            Database.Save(export);
                            Notice("{0} changed export {1}", CurrentSession.User.ShortHand, export);
                        }
                        else
                        {
                            status.SetErrorAccessDenied();
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/export/add"] = parameters =>
            {
                if (HasAnyOrganizationAccess(PartAccess.Demography, AccessRight.Read))
                {
                    return View["View/exportedit.sshtml",
                        new ExportEditViewModel(Translator, Database, CurrentSession)];
                }
                return null;
            };
            Post["/export/add/new"] = parameters =>
            {
                string idString = parameters.id;
                var body = ReadBody();
                var model = JsonConvert.DeserializeObject<ExportEditViewModel>(body);
                var export = new Export(Guid.NewGuid());
                var status = CreateStatus();
                status.AssignStringRequired("Name", export.Name, model.Name);
                status.AssignObjectIdString("SelectOrganization", export.SelectOrganization, model.SelectOrganization);
                status.AssignObjectIdString("SelectTag", export.SelectTag, model.SelectTag);
                status.AssignEnumIntString("SelectLanguage", export.SelectLanguage, model.SelectLanguage);
                status.AssignStringList("ExportColumns", export.ExportColumns, model.ExportColumns);

                if (status.IsSuccess)
                {
                    if (IsPermittedExport(export))
                    {
                        Database.Save(export);
                        Notice("{0} added export {1}", CurrentSession.User.ShortHand, export);
                    }
                    else
                    {
                        status.SetErrorAccessDenied(); 
                    }
                }

                return status.CreateJsonData();
            };
            Get["/export/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var export = Database.Query<Export>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(export))
                {
                    if (IsPermittedExport(export))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            export.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted export {1}", CurrentSession.User.ShortHand, export);
                        }
                    }
                    else
                    {
                        status.SetErrorAccessDenied();
                    }
                }

                return status.CreateJsonData();
            };
            Get["/export/download/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var export = Database.Query<Export>(idString);

                if (export != null)
                {
                    if (IsPermittedExport(export))
                    {
                        var manager = new ExportColumnManager(Translator);
                        var stream = new MemoryStream();
                        var textWriter = new StreamWriter(stream);
                        textWriter.WriteLine(manager.ConstructHeader(export.ExportColumns.Value));

                        foreach (var person in Query(export))
                        {
                            textWriter.WriteLine(manager.ConstructRow(person, export.ExportColumns.Value)); 
                        }

                        textWriter.Flush();
                        stream.Position = 0;

                        var response = new StreamResponse(() => stream, "test/csv");
                        Notice("{0} exported data", CurrentSession.User.ShortHand);
                        return response.AsAttachment("export.csv");
                    }
                }

                return null;
            };
        }

        private IEnumerable<Person> Query(Export export)
        {
            return Database.Query<Person>()
                .Where(p => export.SelectOrganization.Value == null || p.ActiveMemberships.Any(m => m.Organization.Value == export.SelectOrganization.Value))
                .Where(p => export.SelectTag.Value == null || p.TagAssignments.Any(t => t.Tag.Value == export.SelectTag.Value))
                .Where(p => export.SelectLanguage.Value == null || p.Language.Value == export.SelectLanguage.Value)
                .OrderBy(p => p.Number.Value);
        }

        private bool IsPermittedExport(Export export)
        {
            var columns = new ExportColumnManager(Translator);

            if (export.SelectOrganization.Value == null)
            {
                return columns
                    .ComputeRequiredAccess(export.ExportColumns.Value)
                    .All(pa => HasSystemWideAccess(pa, AccessRight.Read));
            }
            else
            {
                return columns
                    .ComputeRequiredAccess(export.ExportColumns.Value)
                    .All(pa => HasAccess(export.SelectOrganization.Value, pa, AccessRight.Read));
            }
        }
    }

    public class ExportColumnManager
    {
        private Translator _translator;
        public List<ExportColumn> Columns { get; private set; }

        private void Add(string id, string title, Func<Person, string> getter, PartAccess access)
        {
            Columns.Add(new ExportColumn(
                id,
                 _translator.Get("Export.Column." + id, "Column '" + id + "' in the export", title),
                 getter,
                 access));
        }

        public IEnumerable<PartAccess> ComputeRequiredAccess(IEnumerable<string> columnIds)
        {
            return columnIds
                .Select(id => Columns.Where(c => c.Id == id).Single())
                .Select(c => c.Access)
                .Distinct();
        }

        public string ConstructHeader(IEnumerable<string> columns)
        {
            return string.Join(";", Columns
                .Where(c => columns.Contains(c.Id))
                .Select(c => "\"" + c.Title + "\""));
        }

        public string ConstructRow(Person person, IEnumerable<string> columns)
        {
            return string.Join(";", Columns
                .Where(c => columns.Contains(c.Id))
                .Select(c => "\"" + c.Getter(person) + "\""));
        }

        public ExportColumnManager(Translator translator)
        {
            _translator = translator;
            Columns = new List<ExportColumn>();
            Add("Id", "Id", p => p.Id.Value.ToString(), PartAccess.Demography);
            Add("Number", "Number", p => p.Number.Value.ToString(), PartAccess.Demography);
            Add("UserName", "Username", p => p.UserName.Value, PartAccess.Anonymous);
            Add("Title", "Title", p => p.Title.Value, PartAccess.Demography);
            Add("LastName", "Last name", p => p.LastName.Value, PartAccess.Demography);
            Add("FirstName", "First name", p => p.FirstName.Value, PartAccess.Demography);
            Add("MiddleNames", "Middle names", p => p.MiddleNames.Value, PartAccess.Demography);
            Add("FirstNames", "First names", p => p.FullFirstNames, PartAccess.Demography);
            Add("ShortHand", "Shorthand", p => p.ShortHand, PartAccess.Demography);
            Add("ShortTitleAndNames", "Short title and names", p => p.ShortTitleAndNames, PartAccess.Contact);
            Add("MailAddress", "E-Mail address", p => p.PrimaryMailAddress, PartAccess.Contact);
            Add("PhoneNumber", "Phone number", p => p.PrimaryPhoneNumber, PartAccess.Contact);
            Add("CareOf", "c/o", p => p.PrimaryPostalAddress.CareOfOrEmpty(), PartAccess.Contact);
            Add("Street", "Street", p => p.PrimaryPostalAddress.StreetOrEmpty(), PartAccess.Contact);
            Add("PostOfficeBox", "P.O. Box", p => p.PrimaryPostalAddress.PostOfficeBoxOrEmpty(), PartAccess.Contact);
            Add("Place", "Place", p => p.PrimaryPostalAddress.PlaceOrEmpty(), PartAccess.Contact);
            Add("Country", "Country", p => p.PrimaryPostalAddress.CountryOrEmpty(translator), PartAccess.Contact);
            Add("State", "State", p => p.PrimaryPostalAddress.StateOrEmpty(translator), PartAccess.Contact);
            Add("StateOrCountry", "State/Country", p => p.PrimaryPostalAddress.StateOrCountry(translator), PartAccess.Contact);
            Add("StreetOrPostOfficeBox", "Street/P.O. Box", p => p.PrimaryPostalAddress.StreetOrPostOfficeBox, PartAccess.Contact);
            Add("PlaceWithPostalCode", "Place with Postal Code", p => p.PrimaryPostalAddress.PlaceWithPostalCode, PartAccess.Contact);
            Add("AddressText", "Address text", p => p.PrimaryPostalAddress.Text(translator), PartAccess.Contact);
        }
    }

    public class ExportColumn
    { 
        public string Id { get; private set; }
        public string Title { get; private set; }
        public Func<Person, string> Getter { get; private set; }
        public PartAccess Access { get; private set; }

        public ExportColumn(string id, string title, Func<Person, string> getter, PartAccess access)
        {
            Id = id;
            Title = title;
            Getter = getter;
            Access = access;
        }
    }
}

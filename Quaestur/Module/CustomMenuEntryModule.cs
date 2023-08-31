using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class CustomMenuEntryEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public List<MultiItemViewModel> LinkUrl;
        public List<NamedIdViewModel> Parents;
        public List<NamedIdViewModel> Pages;
        public string Parent;
        public string Page;
        public string Ordering;
        public string PhraseFieldParent;
        public string PhraseFieldPage;
        public string PhraseFieldOrdering;

        public CustomMenuEntryEditViewModel()
        { 
        }

        public CustomMenuEntryEditViewModel(Translator translator)
            : base(translator, translator.Get("CustomMenuEntry.Edit.Title", "Title of the customMenuEntry edit dialog", "Edit customMenuEntry"), "customMenuEntryEditDialog")
        {
            PhraseFieldParent = translator.Get("CustomMenuEntry.Edit.Field.Parent", "Parent field in the county client edit dialog", "Parent");
            PhraseFieldPage = translator.Get("CustomMenuEntry.Edit.Field.Page", "Page field in the county client edit dialog", "Page");
            PhraseFieldOrdering = translator.Get("CustomMenuEntry.Edit.Field.Ordering", "Ordering field in the county client edit dialog", "Ordering");
        }

        public CustomMenuEntryEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("CustomMenuEntry.Edit.Field.Name", "Name field in the custom menu entry edit dialog", "Name ({0})", new MultiLanguageString());
            LinkUrl = translator.CreateLanguagesMultiItem("CustomMenuEntry.Edit.Field.LinkUrl", "LinkUrl field in the custom menu entry edit dialog", "Link URL ({0})", new MultiLanguageString());
            Parent = string.Empty;
            Page = string.Empty;
            Ordering = "0";
            Parents = db.Query<CustomMenuEntry>()
            .Where(e => e.Parent.Value == null)
                .Select(e => new NamedIdViewModel(translator, e, false))
                .OrderBy(e => e.Name).ToList();
            Parents.Add(new NamedIdViewModel(translator.Get("CustomMenuEntry.Edit.Field.Parent.None", "No selection in the select parent field of the edit custom menu entry page", "None"), false, true));
            Pages = db.Query<CustomPage>()
                .Select(e => new NamedIdViewModel(translator, e, false))
                .OrderBy(e => e.Name).ToList();
            Pages.Add(new NamedIdViewModel(translator.Get("CustomMenuEntry.Edit.Field.Page.None", "No selection in the select page field of the edit custom menu entry page", "None"), false, true));
        }

        public CustomMenuEntryEditViewModel(Translator translator, IDatabase db, CustomMenuEntry customMenuEntry)
            : this(translator)
        {
            Method = "edit";
            Id = customMenuEntry.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("CustomMenuEntry.Edit.Field.Name", "Name field in the customMenuEntry edit dialog", "Name ({0})", customMenuEntry.Name.Value);
            LinkUrl = translator.CreateLanguagesMultiItem("CustomMenuEntry.Edit.Field.LinkUrl", "LinkUrl field in the customMenuEntry edit dialog", "Link URL ({0})", customMenuEntry.LinkUrl.Value);
            Parent = string.Empty;
            Page = string.Empty;
            Ordering = customMenuEntry.Ordering.Value.ToString();
            Parents = db.Query<CustomMenuEntry>()
                .Where(e => e.Parent.Value == null && e != customMenuEntry)
                .Select(e => new NamedIdViewModel(translator, e, e == customMenuEntry.Parent.Value))
                .OrderBy(e => e.Name).ToList();
            Parents.Add(new NamedIdViewModel(translator.Get("CustomMenuEntry.Edit.Field.Parent.None", "No selection in the select parent field of the edit custom menu entry page", "None"), false, customMenuEntry.Parent.Value == null));
            Pages = db.Query<CustomPage>()
                .Select(e => new NamedIdViewModel(translator, e, e == customMenuEntry.Page.Value))
                .OrderBy(e => e.Name).ToList();
            Pages.Add(new NamedIdViewModel(translator.Get("CustomMenuEntry.Edit.Field.Page.None", "No selection in the select page field of the edit custom menu entry page", "None"), false, customMenuEntry.Page.Value == null));
        }
    }

    public class CustomMenuEntryViewModel : MasterViewModel
    {
        public CustomMenuEntryViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, 
            translator.Get("CustomMenuEntry.List.Title", "Title of the customMenuEntry list page", "Countries"), 
            session)
        { 
        }
    }

    public class CustomMenuEntryListItemViewModel
    {
        public string Id;
        public string Name;
        public string Ordering;
        public string PhraseDeleteConfirmationQuestion;

        public CustomMenuEntryListItemViewModel(Translator translator, CustomMenuEntry customMenuEntry)
        {
            Id = customMenuEntry.Id.Value.ToString();
            Name = customMenuEntry.Name.Value[translator.Language].EscapeHtml();
            if (customMenuEntry.Parent.Value != null)
            {
                Name = customMenuEntry.Parent.Value.Name.Value[translator.Language].EscapeHtml() + " / " + Name;
                Ordering = string.Format("{0:0000000000}.{1:0000000000}", customMenuEntry.Parent.Value.Ordering.Value, customMenuEntry.Ordering.Value);
            }
            else
            {
                Ordering = string.Format("{0:0000000000}", customMenuEntry.Ordering.Value);
            }
            PhraseDeleteConfirmationQuestion = translator.Get("CustomMenuEntry.List.Delete.Confirm.Question", "Delete customMenuEntry confirmation question", "Do you really wish to delete customMenuEntry {0}?", customMenuEntry.GetText(translator));
        }
    }

    public class CustomMenuEntryListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<CustomMenuEntryListItemViewModel> List;

        public CustomMenuEntryListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderName = translator.Get("CustomMenuEntry.List.Header.Name", "Column 'Name' in the custom menu entry list", "Name").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("CustomMenuEntry.List.Delete.Confirm.Title", "Delete custom menu entry confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("CustomMenuEntry.List.Delete.Confirm.Info", "Delete custom menu entry confirmation info", "This will also delete all postal addresses in that customMenuEntry.").EscapeHtml();
            List = database.Query<CustomMenuEntry>()
                .Select(e => new CustomMenuEntryListItemViewModel(translator, e))
                .OrderBy(e => e.Ordering).ToList();
        }
    }

    public class CustomMenuEntryEdit : QuaesturModule
    {
        public CustomMenuEntryEdit()
        {
            RequireCompleteLogin();

            Get("/custommenuentry", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/custommenuentry.sshtml",
                        new CustomMenuEntryViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/custommenuentry/list", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/custommenuentrylist.sshtml",
                        new CustomMenuEntryListViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Get("/custommenuentry/edit/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var customMenuEntry = Database.Query<CustomMenuEntry>(idString);

                    if (customMenuEntry != null)
                    {
                        return View["View/custommenuentryedit.sshtml",
                            new CustomMenuEntryEditViewModel(Translator, Database, customMenuEntry)];
                    }
                }
                return string.Empty;
            });
            Post("/custommenuentry/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<CustomMenuEntryEditViewModel>(ReadBody());
                    var customMenuEntry = Database.Query<CustomMenuEntry>(idString);

                    if (status.ObjectNotNull(customMenuEntry))
                    {
                        status.AssignMultiLanguageRequired("Name", customMenuEntry.Name, model.Name);
                        status.AssignMultiLanguageFree("LinkUrl", customMenuEntry.LinkUrl, model.LinkUrl);
                        status.AssignObjectIdString("Parent", customMenuEntry.Parent, model.Parent);
                        status.AssignObjectIdString("Page", customMenuEntry.Page, model.Page);
                        status.AssignInt32String("Ordering", customMenuEntry.Ordering, model.Ordering);

                        if (status.IsSuccess)
                        {
                            Database.Save(customMenuEntry);
                            Notice("{0} changed custom menu entry {1}", CurrentSession.User.ShortHand, customMenuEntry);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/custommenuentry/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/custommenuentryedit.sshtml",
                        new CustomMenuEntryEditViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Post("/custommenuentry/add/new", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<CustomMenuEntryEditViewModel>(ReadBody());
                    var customMenuEntry = new CustomMenuEntry(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", customMenuEntry.Name, model.Name);
                    status.AssignMultiLanguageFree("LinkUrl", customMenuEntry.LinkUrl, model.LinkUrl);
                    status.AssignObjectIdString("Parent", customMenuEntry.Parent, model.Parent);
                    status.AssignObjectIdString("Page", customMenuEntry.Page, model.Page);
                    status.AssignInt32String("Ordering", customMenuEntry.Ordering, model.Ordering);

                    if (status.IsSuccess)
                    {
                        Database.Save(customMenuEntry);
                        Notice("{0} added custom menu entry {1}", CurrentSession.User.ShortHand, customMenuEntry);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/custommenuentry/delete/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var customMenuEntry = Database.Query<CustomMenuEntry>(idString);

                    if (customMenuEntry != null)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            customMenuEntry.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted custom menu entry {1}", CurrentSession.User.ShortHand, customMenuEntry);
                        }
                    }
                }
                return string.Empty;
            });
        }
    }
}

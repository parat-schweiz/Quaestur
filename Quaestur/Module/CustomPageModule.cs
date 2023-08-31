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
    public class PageViewModel : MasterViewModel
    {
        public string Content;

        public PageViewModel(IDatabase database, Translator translator, Session session, CustomPage page)
            : base(database, translator, page.Name.Value[translator.Language], session)
        {
            Content = page.Content.Value[translator.Language];
        }
    }

    public class CustomPageEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public List<MultiItemViewModel> Content;
        public string PhraseButtonSave;
        public string PhraseButtonCancel;

        public CustomPageEditViewModel()
        {
        }

        public CustomPageEditViewModel(Translator translator, IDatabase db, Session session)
            : base(db, translator, translator.Get("CustomPage.Edit.Title", "Title of the custom page edit dialog", "Edit customPage"), session)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("CustomPage.Edit.Field.Name", "Name field in the custom page edit dialog", "Name ({0})", new MultiLanguageString());
            Content = translator.CreateLanguagesMultiItem("CustomPage.Edit.Field.Content", "Content field in the custom page edit dialog", "Content ({0})", new MultiLanguageString());
            PhraseButtonSave = translator.Get("CustomPage.Edit.Button.Save", "Save button in the custom page edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("CustomPage.Edit.Button.Cancel", "Cancel button in the custom page edit page", "Cancel").EscapeHtml();
        }

        public CustomPageEditViewModel(Translator translator, IDatabase db, Session session, CustomPage customPage)
            : base(db, translator, translator.Get("CustomPage.Edit.Title", "Title of the custom page edit dialog", "Edit customPage"), session)
        {
            Method = "edit";
            Id = customPage.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("CustomPage.Edit.Field.Name", "Name field in the custom page edit dialog", "Name ({0})", customPage.Name.Value);
            Content = translator.CreateLanguagesMultiItem("CustomPage.Edit.Field.Content", "Content field in the custom page edit dialog", "Content ({0})", customPage.Content.Value);
            PhraseButtonSave = translator.Get("CustomPage.Edit.Button.Save", "Save button in the custom page edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("CustomPage.Edit.Button.Cancel", "Cancel button in the custom page edit page", "Cancel").EscapeHtml();
        }
    }

    public class CustomPageViewModel : MasterViewModel
    {
        public CustomPageViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, 
            translator.Get("CustomPage.List.Title", "Title of the customPage list page", "Countries"), 
            session)
        { 
        }
    }

    public class CustomPageListItemViewModel
    {
        public string Id;
        public string Name;
        public string PhraseDeleteConfirmationQuestion;

        public CustomPageListItemViewModel(Translator translator, CustomPage customPage)
        {
            Id = customPage.Id.Value.ToString();
            Name = customPage.Name.Value[translator.Language].EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("CustomPage.List.Delete.Confirm.Question", "Delete custom page confirmation question", "Do you really wish to delete custom page {0}?", customPage.GetText(translator));
        }
    }

    public class CustomPageListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<CustomPageListItemViewModel> List;

        public CustomPageListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderName = translator.Get("CustomPage.List.Header.Name", "Column 'Name' in the custom page list", "Name").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("CustomPage.List.Delete.Confirm.Title", "Delete custom page confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("CustomPage.List.Delete.Confirm.Info", "Delete custom page confirmation info", "This will also delete all postal addresses in that country.").EscapeHtml();
            List = new List<CustomPageListItemViewModel>(
                database.Query<CustomPage>()
                .Select(c => new CustomPageListItemViewModel(translator, c)));
        }
    }

    public class CustomPageEdit : QuaesturModule
    {
        public CustomPageEdit()
        {
            RequireCompleteLogin();

            Get("/page/{id}", parameters =>
            {
                string idString = parameters.id;
                var customPage = Database.Query<CustomPage>(idString);

                if (customPage != null)
                {
                    return View["View/page.sshtml",
                        new PageViewModel(Database, Translator, CurrentSession, customPage)];
                }

                return string.Empty;
            });
            Get("/custompage", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/custompage.sshtml",
                        new CustomPageViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/custompage/list", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/custompagelist.sshtml",
                        new CustomPageListViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Get("/custompage/edit/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var customPage = Database.Query<CustomPage>(idString);

                    if (customPage != null)
                    {
                        return View["View/custompageedit.sshtml",
                            new CustomPageEditViewModel(Translator, Database, CurrentSession, customPage)];
                    }
                    else
                    {
                        return View["View/custompageedit.sshtml",
                            new CustomPageEditViewModel(Translator, Database, CurrentSession)];
                    }
                }
                else
                {
                    return AccessDenied();
                }
            });
            Post("/custompage/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<CustomPageEditViewModel>(ReadBody());
                    var customPage = Database.Query<CustomPage>(idString);

                    if (status.ObjectNotNull(customPage))
                    {
                        status.AssignMultiLanguageRequired("Name", customPage.Name, model.Name);
                        status.AssignMultiLanguageFree("Content", customPage.Content, model.Content);

                        if (status.IsSuccess)
                        {
                            Database.Save(customPage);
                            Notice("{0} changed custom page {1}", CurrentSession.User.ShortHand, customPage);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/custompage/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/custompageedit.sshtml",
                        new CustomPageEditViewModel(Translator, Database, CurrentSession)];
                }
                else
                {
                    return AccessDenied();
                }
            });
            Post("/custompage/add/new", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<CustomPageEditViewModel>(ReadBody());
                    var customPage = new CustomPage(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", customPage.Name, model.Name);
                    status.AssignMultiLanguageFree("Content", customPage.Content, model.Content);

                    if (status.IsSuccess)
                    {
                        Database.Save(customPage);
                        Notice("{0} added custom page {1}", CurrentSession.User.ShortHand, customPage);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/custompage/delete/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var customPage = Database.Query<CustomPage>(idString);

                    if (customPage != null)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            customPage.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted custom page {1}", CurrentSession.User.ShortHand, customPage);
                        }
                    }
                }

                return string.Empty;
            });
        }
    }
}

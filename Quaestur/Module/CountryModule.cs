﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class CountryEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Code;
        public string PhraseFieldCode;

        public CountryEditViewModel()
        { 
        }

        public CountryEditViewModel(Translator translator)
            : base(translator, translator.Get("Country.Edit.Title", "Title of the country edit dialog", "Edit country"), "countryEditDialog")
        {
            PhraseFieldCode = translator.Get("Country.Edit.Field.Code", "Code field in the county client edit dialog", "Code");
        }

        public CountryEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Country.Edit.Field.Name", "Name field in the country edit dialog", "Name ({0})", new MultiLanguageString());
            Code = string.Empty;
        }

        public CountryEditViewModel(Translator translator, IDatabase db, Country country)
            : this(translator)
        {
            Method = "edit";
            Id = country.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Country.Edit.Field.Name", "Name field in the country edit dialog", "Name ({0})", country.Name.Value);
            Code = country.Code.Value;
        }
    }

    public class CountryViewModel : MasterViewModel
    {
        public CountryViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, 
            translator.Get("Country.List.Title", "Title of the country list page", "Countries"), 
            session)
        { 
        }
    }

    public class CountryListItemViewModel
    {
        public string Id;
        public string Name;
        public string PhraseDeleteConfirmationQuestion;

        public CountryListItemViewModel(Translator translator, Country country)
        {
            Id = country.Id.Value.ToString();
            Name = country.Name.Value[translator.Language].EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Country.List.Delete.Confirm.Question", "Delete country confirmation question", "Do you really wish to delete country {0}?", country.GetText(translator));
        }
    }

    public class CountryListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<CountryListItemViewModel> List;

        public CountryListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Country.List.Header.Name", "Column 'Name' in the country list", "Name").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Country.List.Delete.Confirm.Title", "Delete country confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Country.List.Delete.Confirm.Info", "Delete country confirmation info", "This will also delete all postal addresses in that country.").EscapeHtml();
            List = new List<CountryListItemViewModel>(
                database.Query<Country>()
                .Select(c => new CountryListItemViewModel(translator, c)));
        }
    }

    public class CountryEdit : QuaesturModule
    {
        public CountryEdit()
        {
            RequireCompleteLogin();

            Get("/country", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/country.sshtml",
                        new CountryViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/country/list", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/countrylist.sshtml",
                        new CountryListViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Get("/country/edit/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var country = Database.Query<Country>(idString);

                    if (country != null)
                    {
                        return View["View/countryedit.sshtml",
                            new CountryEditViewModel(Translator, Database, country)];
                    }
                }
                return string.Empty;
            });
            Post("/country/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<CountryEditViewModel>(ReadBody());
                    var country = Database.Query<Country>(idString);

                    if (status.ObjectNotNull(country))
                    {
                        status.AssignMultiLanguageRequired("Name", country.Name, model.Name);
                        status.AssignStringFree("Code", country.Code, model.Code);

                        if (status.IsSuccess)
                        {
                            Database.Save(country);
                            Notice("{0} changed country {1}", CurrentSession.User.ShortHand, country);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/country/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/countryedit.sshtml",
                        new CountryEditViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Post("/country/add/new", parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<CountryEditViewModel>(ReadBody());
                    var country = new Country(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", country.Name, model.Name);
                    status.AssignStringFree("Code", country.Code, model.Code);

                    if (status.IsSuccess)
                    {
                        Database.Save(country);
                        Notice("{0} added country {1}", CurrentSession.User.ShortHand, country);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/country/delete/{id}", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var country = Database.Query<Country>(idString);

                    if (country != null)
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            country.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted country {1}", CurrentSession.User.ShortHand, country);
                        }
                    }
                }
                return string.Empty;
            });
        }
    }
}

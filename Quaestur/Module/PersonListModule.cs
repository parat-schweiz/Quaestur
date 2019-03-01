using System.Collections.Generic;
using System.Linq;
using System;
using Nancy;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Quaestur
{
    public class PersonListItemViewModel
    {
        public bool ShowNumber;
        public bool ShowUser;
        public bool ShowName;
        public bool ShowStreet;
        public bool ShowPlace;
        public bool ShowState;
        public bool ShowMail;
        public bool ShowPhone;

        public string Id;
        public string Number;
        public string UserName;
        public string LastName;
        public string FirstNames;
        public string MailAddress;
        public string PhoneNumber;
        public string Street;
        public string Place;
        public string State;

        public PersonListItemViewModel(Translator translator, Person person, PersonListDataViewModel parent, Session session)
        {
            ShowNumber = parent.ShowNumber;
            ShowUser = parent.ShowUser;
            ShowName = parent.ShowName;
            ShowStreet = parent.ShowStreet;
            ShowPlace = parent.ShowPlace;
            ShowState = parent.ShowState;
            ShowMail = parent.ShowMail;
            ShowPhone = parent.ShowPhone;

            var contactAccess = session.HasAccess(person, PartAccess.Contact, AccessRight.Read);

            Id = person.Id.ToString();
            Number = contactAccess ?
                person.Number.ToString() :
                string.Empty;
            UserName = person.UserName.Value.EscapeHtml();
            LastName = contactAccess ?
                person.LastName.Value.EscapeHtml() :
                string.Empty;
            FirstNames = contactAccess ? 
                person.ShortTitleAndNames.EscapeHtml() :
                string.Empty;
            MailAddress = contactAccess ? 
                person.PrimaryMailAddress.EscapeHtml() :
                string.Empty;
            PhoneNumber = contactAccess ? 
                person.PrimaryPhoneNumber.EscapeHtml() :
                string.Empty;
            Place = contactAccess ?
                person.PostalAddresses
                .OrderBy(p => p.Precedence.Value)
                .Select(p => p.PlaceWithPostalCode.EscapeHtml())
                .FirstOrDefault() ?? string.Empty :
                string.Empty;
            Street = contactAccess ?
                person.PostalAddresses
                .OrderBy(p => p.Precedence.Value)
                .Select(p => p.StreetOrPostOfficeBox.EscapeHtml())
                .FirstOrDefault() ?? string.Empty :
                string.Empty;
            State = contactAccess ?
                person.PostalAddresses
                .OrderBy(p => p.Precedence.Value)
                .Select(p => p.StateOrCountry(translator).EscapeHtml())
                .FirstOrDefault() ?? string.Empty :
                string.Empty;
        }
    }

    public class PersonListDataViewModel
    {
        public bool ShowNumber;
        public bool ShowUser;
        public bool ShowName;
        public bool ShowStreet;
        public bool ShowPlace;
        public bool ShowState;
        public bool ShowMail;
        public bool ShowPhone;
        public List<PersonListItemViewModel> List;

        public string PhraseHeaderNumber = string.Empty;
        public string PhraseHeaderUser = string.Empty;
        public string PhraseHeaderLastName = string.Empty;
        public string PhraseHeaderFirstNames = string.Empty;
        public string PhraseHeaderStreet = string.Empty;
        public string PhraseHeaderPlace = string.Empty;
        public string PhraseHeaderState = string.Empty;
        public string PhraseHeaderMail = string.Empty;
        public string PhraseHeaderPhone = string.Empty;

        public PersonListDataViewModel(Translator translator, IEnumerable<Person> persons, SearchSettings settings, Session session)
        {
            ShowNumber = settings.ShowNumber.Value;
            ShowUser = settings.ShowUser.Value;
            ShowName = settings.ShowName.Value;
            ShowStreet = settings.ShowStreet.Value;
            ShowPlace = settings.ShowPlace.Value;
            ShowState = settings.ShowState.Value;
            ShowMail = settings.ShowMail.Value;
            ShowPhone = settings.ShowPhone.Value;
            List = new List<PersonListItemViewModel>(persons.Select(p => new PersonListItemViewModel(translator, p, this, session)));

            PhraseHeaderNumber = translator.Get("Person.List.Header.Number", "Column 'Number' in the person list page", "#").EscapeHtml();
            PhraseHeaderUser = translator.Get("Person.List.Header.UserName", "Column 'Username' in the person list page", "Username").EscapeHtml();
            PhraseHeaderLastName = translator.Get("Person.List.Header.LastName", "Column 'Last name' in the person list page", "Last name").EscapeHtml();
            PhraseHeaderFirstNames = translator.Get("Person.List.Header.FirstNames", "Column 'First names' in the person list page", "First names").EscapeHtml();
            PhraseHeaderStreet = translator.Get("Person.List.Header.Street", "Column 'Street' in the person list page", "Street").EscapeHtml();
            PhraseHeaderPlace = translator.Get("Person.List.Header.Place", "Column 'Place' in the person list page", "Place").EscapeHtml();
            PhraseHeaderState = translator.Get("Person.List.Header.State", "Column 'State' in the person list page", "State/Country").EscapeHtml();
            PhraseHeaderMail = translator.Get("Person.List.Header.Mail", "Column 'E-Mail' in the person list page", "E-Mail").EscapeHtml();
            PhraseHeaderPhone = translator.Get("Person.List.Header.Phone", "Column 'Phone' in the person list page", "Phone").EscapeHtml();
        }
    }

    public class PersonPageViewModel
    {
        public string Index;
        public string Number;
        public string State;

        public PersonPageViewModel(int index, bool active)
        {
            Index = index.ToString();
            Number = (index + 1).ToString();
            State = active ? "active" : string.Empty;
        }
    }

    public class PersonItemsPerPageViewModel
    {
        public string Count;
        public string State;

        public PersonItemsPerPageViewModel(int count, bool active)
        {
            Count = count.ToString();
            State = active ? "active" : string.Empty;
        }
    }

    public class PersonPagesViewModel
    {
        public string CurrentPageNumber;
        public string CurrentItemsPerPage;
        public List<PersonPageViewModel> Pages;
        public List<PersonItemsPerPageViewModel> ItemsPerPage;

        public string PhrasePage = string.Empty;
        public string PhrasePerPage = string.Empty;

        public PersonPagesViewModel(Translator translator, int pageCount, SearchSettings settings)
        {
            CurrentPageNumber = (settings.CurrentPage.Value + 1).ToString();
            Pages = new List<PersonPageViewModel>();
            for (int i = 0; i < pageCount; i++)
            {
                Pages.Add(new PersonPageViewModel(i, settings.CurrentPage.Value == i));
            }
            CurrentItemsPerPage = settings.ItemsPerPage.Value.ToString();
            ItemsPerPage = new List<PersonItemsPerPageViewModel>();
            foreach (int i in new int[] { 10, 15, 20, 25, 30, 40, 50 })
            {
                ItemsPerPage.Add(new PersonItemsPerPageViewModel(i, settings.ItemsPerPage.Value == i));
            }

            PhrasePage = translator.Get("Person.List.Pages.Page", "In paging of the person list page", "Page").EscapeHtml();
            PhrasePerPage = translator.Get("Person.List.Pages.PerPage", "In paging of the person list page", "per Page").EscapeHtml();
        }
    }

    public class PersonListViewModel : MasterViewModel
    {
        public string PhraseShowNumber = string.Empty;
        public string PhraseShowUser = string.Empty;
        public string PhraseShowName = string.Empty;
        public string PhraseShowStreet = string.Empty;
        public string PhraseShowPlace = string.Empty;
        public string PhraseShowState = string.Empty;
        public string PhraseShowMail = string.Empty;
        public string PhraseShowPhone = string.Empty;
        public string PhraseShowColumns = string.Empty;
        public string PhraseSearch = string.Empty;
        public string PhraseFilter = string.Empty;

        public PersonListViewModel(Translator translator, Session session)
            : base(translator, 
                   translator.Get("Person.List.Title", "Title of the person list page", "Liste"), 
                   session)
        {
            PhraseShowNumber = translator.Get("Person.List.Show.Number", "Show column 'Number' in the person list page", "#").EscapeHtml();
            PhraseShowUser = translator.Get("Person.List.Show.UserName", "Show column 'Username' in the person list page", "Username").EscapeHtml();
            PhraseShowName = translator.Get("Person.List.Show.Name", "Show columns 'Name' in the person list page", "Name").EscapeHtml();
            PhraseShowStreet = translator.Get("Person.List.Show.Street", "Show column 'Street' in the person list page", "Street").EscapeHtml();
            PhraseShowPlace = translator.Get("Person.List.Show.Place", "Show column 'Place' in the person list page", "Place").EscapeHtml();
            PhraseShowState = translator.Get("Person.List.Show.State", "Show column 'State/Country' in the person list page", "State/Country").EscapeHtml();
            PhraseShowMail = translator.Get("Person.List.Show.Mail", "Show column 'E-Mail' in the person list page", "E-Mail").EscapeHtml();
            PhraseShowPhone = translator.Get("Person.List.Show.Phone", "Show column 'Phone' in the person list page", "Phone").EscapeHtml();
            PhraseShowColumns = translator.Get("Person.List.Show.Columns", "Dropdown 'Columns' in the person list page", "Columns").EscapeHtml();
            PhraseSearch = translator.Get("Person.List.Search", "Hint in the search box of the person list page", "Search").EscapeHtml();
            PhraseFilter = translator.Get("Person.List.Filter", "Button 'Filter' on the person list page", "Filter").EscapeHtml();
        }
    }

    public class SearchSettingsUpdate
    {
        public Guid Id;
        public string Name;
        public string FilterText;
        public int ItemsPerPage;
        public int CurrentPage;
        public bool ShowNumber;
        public bool ShowUser;
        public bool ShowName;
        public bool ShowStreet;
        public bool ShowPlace;
        public bool ShowState;
        public bool ShowMail;
        public bool ShowPhone;

        public SearchSettingsUpdate()
        { }

        public SearchSettingsUpdate(SearchSettings settings)
        {
            Id = settings.Id.Value;
            Name = settings.Name.Value;
            FilterText = settings.FilterText.Value;
            ItemsPerPage = settings.ItemsPerPage.Value;
            CurrentPage = settings.CurrentPage.Value;
            ShowNumber = settings.ShowNumber.Value;
            ShowUser = settings.ShowUser.Value;
            ShowName = settings.ShowName.Value;
            ShowStreet = settings.ShowStreet.Value;
            ShowPlace = settings.ShowPlace.Value;
            ShowState = settings.ShowState.Value;
            ShowMail = settings.ShowMail.Value;
            ShowPhone = settings.ShowPhone.Value;
        }

        public void Apply(SearchSettings settings)
        {
            settings.Name.Value = Name;
            settings.FilterText.Value = FilterText;
            settings.ItemsPerPage.Value = ItemsPerPage;
            settings.CurrentPage.Value = CurrentPage;
            settings.ShowNumber.Value = ShowNumber;
            settings.ShowUser.Value = ShowUser;
            settings.ShowName.Value = ShowName;
            settings.ShowStreet.Value = ShowStreet;
            settings.ShowPlace.Value = ShowPlace;
            settings.ShowState.Value = ShowState;
            settings.ShowMail.Value = ShowMail;
            settings.ShowPhone.Value = ShowPhone;
        }
    }

    public class PersonListModule : QuaesturModule
    {
        private bool Filter(Person person, SearchSettings settings)
        {
            var textFilter =
                person.Number.ToString() == settings.FilterText.Value ||
                person.UserName.Value.Contains(settings.FilterText.Value) ||
                person.FullName.Contains(settings.FilterText.Value) ||
                person.ServiceAddresses.Any(a => a.Address.Value.Contains(settings.FilterText.Value)) ||
                person.PostalAddresses.Any(a => a.Text(Translator).Contains(settings.FilterText.Value));
            var accessFilter = HasAccess(person, PartAccess.Anonymous, AccessRight.Read);
            return textFilter && accessFilter;
        }

        public PersonListModule()
        {
            RequireCompleteLogin();

            Get["/person/list"] = parameters =>
            {
                return View["View/personlist.sshtml", new PersonListViewModel(Translator, CurrentSession)];
            };
            Get["/person/list/settings/list"] = parameters =>
            {
                var settingsList = Database
                    .Query<SearchSettings>(DC.Equal("personid", CurrentSession.User.Id.Value))
                    .ToList();
                var result = new JArray();

                if (!settingsList.Any())
                {
                    var settings = new SearchSettings(Guid.NewGuid());
                    settings.Person.Value = CurrentSession.User;
                    settings.Name.Value = Translate("Person.List.Settings.DefaultName", "Default name for new search settings", "Default");
                    Database.Save(settings);
                    settingsList.Add(settings);
                }

                foreach (var settings in settingsList)
                {
                    result.Add(
                        new JObject(
                            new JProperty("Id", settings.Id.Value), 
                            new JProperty("Name", settings.Name.Value)));
                }

                return result.ToString();
            };
            Get["/person/list/settings/get/{ssid}"] = parameters =>
            {
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);

                if (settings == null || 
                    settings.Person.Value != CurrentSession.User)
                {
                    return null;
                }

                var update = new SearchSettingsUpdate(settings);
                return JsonConvert.SerializeObject(update).ToString();
            };
            Post["/person/list/settings/set/{ssid}"] = parameters =>
            {
                var update = JsonConvert.DeserializeObject<SearchSettingsUpdate>(ReadBody());
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);
                var status = CreateStatus();

                if (settings == null)
                {
                    settings = new SearchSettings(Guid.NewGuid());
                    settings.Person.Value = CurrentSession.User;
                }
                else if (settings.Person.Value != CurrentSession.User)
                {
                    status.SetErrorAccessDenied(); 
                }

                update.Apply(settings);
                Database.Save(settings);
                return status.CreateJsonData();
            };
            Get["/person/list/data/{ssid}"] = parameters =>
            {
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);
                if (settings == null) return null;
                var persons = Database.Query<Person>()
                    .Where(p => Filter(p, settings));
                var skip = settings.ItemsPerPage * settings.CurrentPage;
                if (skip > persons.Count()) skip = 0;
                var page = persons
                    .OrderBy(p => p.LastName.Value)
                    .Skip(skip)
                    .Take(settings.ItemsPerPage);
                return View["View/personlist_data.sshtml", new PersonListDataViewModel(Translator, page, settings, CurrentSession)];
            };
            Get["/person/list/pages/{ssid}"] = parameters =>
            {
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);
                if (settings == null) return null;
                var personCount = Database.Query<Person>()
                    .Count(p => Filter(p, settings));
                var pageCount = (personCount / settings.ItemsPerPage) + Math.Min(personCount % settings.ItemsPerPage, 1);

                if (settings.CurrentPage.Value >= pageCount)
                {
                    settings.CurrentPage.Value = 0;
                    Database.Save(settings);
                }

                return View["View/personlist_pages.sshtml", new PersonPagesViewModel(Translator, pageCount, settings)];
            };
        }
    }
}

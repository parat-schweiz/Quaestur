using System.Collections.Generic;
using System.Linq;
using System;
using Nancy;
using Nancy.Security;
using Newtonsoft.Json;

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
            ShowNumber = settings.ShowNumber;
            ShowUser = settings.ShowUser;
            ShowName = settings.ShowName;
            ShowStreet = settings.ShowStreet;
            ShowPlace = settings.ShowPlace;
            ShowState = settings.ShowState;
            ShowMail = settings.ShowMail;
            ShowPhone = settings.ShowPhone;
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
            CurrentPageNumber = (settings.CurrentPage + 1).ToString();
            Pages = new List<PersonPageViewModel>();
            for (int i = 0; i < pageCount; i++)
            {
                Pages.Add(new PersonPageViewModel(i, settings.CurrentPage == i));
            }
            CurrentItemsPerPage = settings.ItemsPerPage.ToString();
            ItemsPerPage = new List<PersonItemsPerPageViewModel>();
            foreach (int i in new int[] { 10, 15, 20, 25, 30, 40, 50 })
            {
                ItemsPerPage.Add(new PersonItemsPerPageViewModel(i, settings.ItemsPerPage == i));
            }

            PhrasePage = translator.Get("Person.List.Pages.Page", "In paging of the person list page", "Page").EscapeHtml();
            PhrasePerPage = translator.Get("Person.List.Pages.PerPage", "In paging of the person list page", "per Page").EscapeHtml();
        }
    }

    public class SearchSettings
    {
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

    public class PersonListModule : QuaesturModule
    {
        private bool Filter(Person person, SearchSettings settings)
        {
            var textFilter =
                person.Number.ToString() == settings.FilterText ||
                person.UserName.Value.Contains(settings.FilterText) ||
                person.FullName.Contains(settings.FilterText) ||
                person.ServiceAddresses.Any(a => a.Address.Value.Contains(settings.FilterText)) ||
                person.PostalAddresses.Any(a => a.Text(Translator).Contains(settings.FilterText));
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
            Post["/person/list/data"] = parameters =>
            {
                var settings = JsonConvert.DeserializeObject<SearchSettings>(ReadBody());
                var page = Database.Query<Person>()
                    .Where(p => Filter(p, settings))
                    .OrderBy(p => p.LastName.Value)
                    .Skip(settings.ItemsPerPage * settings.CurrentPage)
                    .Take(settings.ItemsPerPage);
                return View["View/personlist_data.sshtml", new PersonListDataViewModel(Translator, page, settings, CurrentSession)];
            };
            Post["/person/list/pages"] = parameters =>
            {
                var settings = JsonConvert.DeserializeObject<SearchSettings>(ReadBody());
                var personCount = Database.Query<Person>()
                    .Where(p => Filter(p, settings))
                    .Count();
                var pageCount = (personCount / settings.ItemsPerPage) + Math.Min(personCount % settings.ItemsPerPage, 1);
                return View["View/personlist_pages.sshtml", new PersonPagesViewModel(Translator, pageCount, settings)];
            };
        }
    }
}

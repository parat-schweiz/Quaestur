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
        public bool ShowMemberships;
        public bool ShowVotingRight;
        public bool ShowRoles;
        public bool ShowTags;

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
        public string Memberships;
        public string VotingRight;
        public string Roles;
        public string Tags;

        public PersonListItemViewModel(IDatabase database, Translator translator, Session session, Person person, SearchSettings settings)
        {
            ShowNumber = settings.Columns.Value.HasFlag(PersonColumns.Number);
            ShowUser = settings.Columns.Value.HasFlag(PersonColumns.User);
            ShowName = settings.Columns.Value.HasFlag(PersonColumns.Name);
            ShowStreet = settings.Columns.Value.HasFlag(PersonColumns.Street);
            ShowPlace = settings.Columns.Value.HasFlag(PersonColumns.Place);
            ShowState = settings.Columns.Value.HasFlag(PersonColumns.State);
            ShowMail = settings.Columns.Value.HasFlag(PersonColumns.Mail);
            ShowPhone = settings.Columns.Value.HasFlag(PersonColumns.Phone);
            ShowMemberships = settings.Columns.Value.HasFlag(PersonColumns.Memberships);
            ShowVotingRight = settings.Columns.Value.HasFlag(PersonColumns.VotingRight);
            ShowRoles = settings.Columns.Value.HasFlag(PersonColumns.Roles);
            ShowTags = settings.Columns.Value.HasFlag(PersonColumns.Tags);

            var contactAccess = session.HasAccess(person, PartAccess.Contact, AccessRight.Read);
            var membershipsAccess = session.HasAccess(person, PartAccess.Membership, AccessRight.Read);
            var tagsAccess = session.HasAccess(person, PartAccess.TagAssignments, AccessRight.Read);
            var rolesAccess = session.HasAccess(person, PartAccess.RoleAssignments, AccessRight.Read);

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
            Memberships = membershipsAccess ?
                string.Join(", ",
                person.Memberships
                .Select(m => m.Organization.Value.GetText(translator) + " / " + m.Type.Value.GetText(translator))
                .OrderBy(m => m)) :
                string.Empty;
            VotingRight = membershipsAccess ?
                person.HasVotingRight(database, translator) :
                string.Empty;
            Roles = rolesAccess ?
                string.Join(", ",
                person.RoleAssignments
                .Select(r => r.Role.Value.GetText(translator))
                .OrderBy(r => r)) :
                string.Empty;
            Tags = tagsAccess ?
                string.Join(", ",
                person.TagAssignments
                .Select(t => t.Tag.Value.GetText(translator))
                .OrderBy(t => t)) :
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
        public bool ShowMemberships;
        public bool ShowVotingRight;
        public bool ShowRoles;
        public bool ShowTags;
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
        public string PhraseHeaderMemberships = string.Empty;
        public string PhraseHeaderVotingRight = string.Empty;
        public string PhraseHeaderRoles = string.Empty;
        public string PhraseHeaderTags = string.Empty;

        public PersonListDataViewModel(IDatabase database, Translator translator, IEnumerable<Person> persons, SearchSettings settings, Session session)
        {
            ShowNumber = settings.Columns.Value.HasFlag(PersonColumns.Number);
            ShowUser = settings.Columns.Value.HasFlag(PersonColumns.User);
            ShowName = settings.Columns.Value.HasFlag(PersonColumns.Name);
            ShowStreet = settings.Columns.Value.HasFlag(PersonColumns.Street);
            ShowPlace = settings.Columns.Value.HasFlag(PersonColumns.Place);
            ShowState = settings.Columns.Value.HasFlag(PersonColumns.State);
            ShowMail = settings.Columns.Value.HasFlag(PersonColumns.Mail);
            ShowPhone = settings.Columns.Value.HasFlag(PersonColumns.Phone);
            ShowMemberships = settings.Columns.Value.HasFlag(PersonColumns.Memberships);
            ShowVotingRight = settings.Columns.Value.HasFlag(PersonColumns.VotingRight);
            ShowRoles = settings.Columns.Value.HasFlag(PersonColumns.Roles);
            ShowTags = settings.Columns.Value.HasFlag(PersonColumns.Tags);
            List = new List<PersonListItemViewModel>(persons.Select(p => new PersonListItemViewModel(database, translator, session, p, settings)));

            PhraseHeaderNumber = translator.Get("Person.List.Header.Number", "Column 'Number' in the person list page", "#").EscapeHtml();
            PhraseHeaderUser = translator.Get("Person.List.Header.UserName", "Column 'Username' in the person list page", "Username").EscapeHtml();
            PhraseHeaderLastName = translator.Get("Person.List.Header.LastName", "Column 'Last name' in the person list page", "Last name").EscapeHtml();
            PhraseHeaderFirstNames = translator.Get("Person.List.Header.FirstNames", "Column 'First names' in the person list page", "First names").EscapeHtml();
            PhraseHeaderStreet = translator.Get("Person.List.Header.Street", "Column 'Street' in the person list page", "Street").EscapeHtml();
            PhraseHeaderPlace = translator.Get("Person.List.Header.Place", "Column 'Place' in the person list page", "Place").EscapeHtml();
            PhraseHeaderState = translator.Get("Person.List.Header.State", "Column 'State' in the person list page", "State/Country").EscapeHtml();
            PhraseHeaderMail = translator.Get("Person.List.Header.Mail", "Column 'E-Mail' in the person list page", "E-Mail").EscapeHtml();
            PhraseHeaderPhone = translator.Get("Person.List.Header.Phone", "Column 'Phone' in the person list page", "Phone").EscapeHtml();
            PhraseHeaderMemberships = translator.Get("Person.List.Header.Memberships", "Column 'Memberships' in the person list page", "Memberships").EscapeHtml();
            PhraseHeaderVotingRight = translator.Get("Person.List.Header.VotingRight", "Column 'Voting right' in the person list page", "Voting right").EscapeHtml();
            PhraseHeaderRoles = translator.Get("Person.List.Header.Roles", "Column 'Roles' in the person list page", "Roles").EscapeHtml();
            PhraseHeaderTags = translator.Get("Person.List.Header.Tags", "Column 'Tags' in the person list page", "Tags").EscapeHtml();
        }
    }

    public class PersonPageViewModel
    {
        public string Index;
        public string Number;
        public string Options;

        public PersonPageViewModel(int index, bool selected)
        {
            Index = index.ToString();
            Number = (index + 1).ToString();
            Options = selected ? "selected" : string.Empty;
        }
    }

    public class PersonItemsPerPageViewModel
    {
        public string Count;
        public string Options;

        public PersonItemsPerPageViewModel(int count, bool selected)
        {
            Count = count.ToString();
            Options = selected ? "selected" : string.Empty;
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
        public string PhraseSearch = string.Empty;
        public string PhraseFilter = string.Empty;
        public List<NamedIdViewModel> Memberships;
        public List<NamedIdViewModel> Tags;
        public List<NamedIntViewModel> Columns;

        public PersonListViewModel(IDatabase database, Translator translator, Session session)
            : base(translator, 
                   translator.Get("Person.List.Title", "Title of the person list page", "Liste"), 
                   session)
        {
            PhraseSearch = translator.Get("Person.List.Search", "Hint in the search box of the person list page", "Search").EscapeHtml();
            PhraseFilter = translator.Get("Person.List.Filter", "Button 'Filter' on the person list page", "Filter").EscapeHtml();

            Memberships = new List<NamedIdViewModel>(database
                .Query<MembershipType>()
                .Select(mt => new NamedIdViewModel(translator, mt.Organization.Value, mt, false))
                .OrderBy(mt => mt.Name));
            Memberships.Add(new NamedIdViewModel(translator.Get("Person.List.Filter.None", "None filter value", "None"), false, true));
            Tags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            Tags.Add(new NamedIdViewModel(translator.Get("Person.List.Filter.None", "None filter value", "None"), false, true));
            Columns = new List<NamedIntViewModel>(PersonColumnsExtensions.Flags
                .Select(f => new NamedIntViewModel(translator, f, false)));
        }
    }

    public class SearchSettingsUpdate
    {
        public Guid Id;
        public string Name;
        public string FilterMembershipId;
        public string FilterTagId;
        public string FilterText;
        public int[] Columns;
        public int ItemsPerPage;
        public int CurrentPage;

        public SearchSettingsUpdate()
        { }

        public SearchSettingsUpdate(SearchSettings settings)
        {
            Id = settings.Id.Value;
            Name = settings.Name.Value;
            FilterMembershipId =
                settings.FilterMembership.Value == null ?
                string.Empty :
                settings.FilterMembership.Value.Id.Value.ToString();
            FilterTagId =
                settings.FilterTag.Value == null ?
                string.Empty :
                settings.FilterTag.Value.Id.Value.ToString();
            FilterText = settings.FilterText.Value;
            ItemsPerPage = settings.ItemsPerPage.Value;
            CurrentPage = settings.CurrentPage.Value;
            Columns = PersonColumnsExtensions.Flags
                .Where(f => settings.Columns.Value.HasFlag(f))
                .Select(f => (int)f)
                .ToArray();
        }

        public void Apply(IDatabase database, SearchSettings settings)
        {
            settings.Name.Value = Name;
            settings.FilterMembership.Value = database.Query<MembershipType>(FilterMembershipId);
            settings.FilterTag.Value = database.Query<Tag>(FilterTagId);
            settings.FilterText.Value = FilterText;
            settings.ItemsPerPage.Value = ItemsPerPage;
            settings.CurrentPage.Value = CurrentPage;
            var flag = PersonColumns.None;
            foreach (var f in Columns)
            {
                flag |= (PersonColumns)f;
            }
            settings.Columns.Value = flag;
        }
    }

    public class PersonListModule : QuaesturModule
    {
        private bool Filter(Person person, SearchSettings settings)
        {
            var membershipFilter =
                settings.FilterMembership.Value == null ||
                person.Memberships.Any(m => m.Type.Value == settings.FilterMembership.Value);
            var tagFilter =
                settings.FilterTag.Value == null ||
                person.TagAssignments.Any(t => t.Tag.Value == settings.FilterTag.Value);
            var textFilter =
                person.Number.ToString() == settings.FilterText.Value ||
                person.UserName.Value.Contains(settings.FilterText.Value) ||
                person.FullName.Contains(settings.FilterText.Value) ||
                person.ServiceAddresses.Any(a => a.Address.Value.Contains(settings.FilterText.Value)) ||
                person.PostalAddresses.Any(a => a.Text(Translator).Contains(settings.FilterText.Value));
            var accessFilter = HasAccess(person, PartAccess.Anonymous, AccessRight.Read);
            return membershipFilter && tagFilter && textFilter && accessFilter;
        }

        public PersonListModule()
        {
            RequireCompleteLogin();

            Get["/person/list"] = parameters =>
            {
                return View["View/personlist.sshtml", new PersonListViewModel(Database, Translator, CurrentSession)];
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

                update.Apply(Database, settings);
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
                    .OrderBy(p => p.SortName)
                    .Skip(skip)
                    .Take(settings.ItemsPerPage);
                return View["View/personlist_data.sshtml", new PersonListDataViewModel(Database, Translator, page, settings, CurrentSession)];
            };
            Get["/person/list/pages/{ssid}"] = parameters =>
            {
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);
                if (settings == null) return null;
                var personCount = Database.Query<Person>()
                    .Count(p => Filter(p, settings));
                var itemsPerPage = Math.Max(1, settings.ItemsPerPage.Value);
                var pageCount = Math.Max(1, (personCount / itemsPerPage) + Math.Min(personCount % itemsPerPage, 1));

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

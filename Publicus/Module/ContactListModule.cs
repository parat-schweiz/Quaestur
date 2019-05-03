using System.Collections.Generic;
using System.Linq;
using System;
using Nancy;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Publicus
{
    public class ContactListItemViewModel
    {
        public bool ShowOrganization;
        public bool ShowName;
        public bool ShowStreet;
        public bool ShowPlace;
        public bool ShowState;
        public bool ShowMail;
        public bool ShowPhone;
        public bool ShowSubscriptions;
        public bool ShowTags;

        public string Id;
        public string Organization;
        public string LastName;
        public string FirstNames;
        public string MailAddress;
        public string PhoneNumber;
        public string Street;
        public string Place;
        public string State;
        public string Subscriptions;
        public string Tags;

        public ContactListItemViewModel(IDatabase database, Translator translator, Contact contact, Session session, SearchSettings settings)
        {
            ShowOrganization = settings.Columns.Value.HasFlag(ContactColumns.Organization);
            ShowName = settings.Columns.Value.HasFlag(ContactColumns.Name);
            ShowStreet = settings.Columns.Value.HasFlag(ContactColumns.Street);
            ShowPlace = settings.Columns.Value.HasFlag(ContactColumns.Place);
            ShowState = settings.Columns.Value.HasFlag(ContactColumns.State);
            ShowMail = settings.Columns.Value.HasFlag(ContactColumns.Mail);
            ShowPhone = settings.Columns.Value.HasFlag(ContactColumns.Phone);
            ShowSubscriptions = settings.Columns.Value.HasFlag(ContactColumns.Subscriptions);
            ShowTags = settings.Columns.Value.HasFlag(ContactColumns.Tags);

            var contactAccess = session.HasAccess(contact, PartAccess.Contact, AccessRight.Read);
            var subscriptionsAccess = session.HasAccess(contact, PartAccess.Subscription, AccessRight.Read);
            var tagsAccess = session.HasAccess(contact, PartAccess.TagAssignments, AccessRight.Read);

            Id = contact.Id.ToString();
            Organization = contact.Organization.Value.EscapeHtml();
            LastName = contactAccess ?
                contact.LastName.Value.EscapeHtml() :
                string.Empty;
            FirstNames = contactAccess ? 
                contact.ShortTitleAndNames.EscapeHtml() :
                string.Empty;
            MailAddress = contactAccess ? 
                contact.PrimaryMailAddress.EscapeHtml() :
                string.Empty;
            PhoneNumber = contactAccess ? 
                contact.PrimaryPhoneNumber.EscapeHtml() :
                string.Empty;
            Place = contactAccess ?
                contact.PostalAddresses
                .OrderBy(p => p.Precedence.Value)
                .Select(p => p.PlaceWithPostalCode.EscapeHtml())
                .FirstOrDefault() ?? string.Empty :
                string.Empty;
            Street = contactAccess ?
                contact.PostalAddresses
                .OrderBy(p => p.Precedence.Value)
                .Select(p => p.StreetOrPostOfficeBox.EscapeHtml())
                .FirstOrDefault() ?? string.Empty :
                string.Empty;
            State = contactAccess ?
                contact.PostalAddresses
                .OrderBy(p => p.Precedence.Value)
                .Select(p => p.StateOrCountry(translator).EscapeHtml())
                .FirstOrDefault() ?? string.Empty :
                string.Empty;
            Subscriptions = subscriptionsAccess ?
                string.Join(", ",
                contact.Subscriptions
                .Select(m => m.Feed.Value.GetText(translator))
                .OrderBy(m => m)) :
                string.Empty;
            Tags = tagsAccess ?
                string.Join(", ",
                contact.TagAssignments
                .Select(t => t.Tag.Value.GetText(translator))
                .OrderBy(t => t)) :
                string.Empty;
        }
    }

    public class ContactListDataViewModel
    {
        public bool ShowOrganization;
        public bool ShowName;
        public bool ShowStreet;
        public bool ShowPlace;
        public bool ShowState;
        public bool ShowMail;
        public bool ShowPhone;
        public bool ShowSubscriptions;
        public bool ShowTags;
        public List<ContactListItemViewModel> List;

        public string PhraseHeaderOrganization = string.Empty;
        public string PhraseHeaderLastName = string.Empty;
        public string PhraseHeaderFirstNames = string.Empty;
        public string PhraseHeaderStreet = string.Empty;
        public string PhraseHeaderPlace = string.Empty;
        public string PhraseHeaderState = string.Empty;
        public string PhraseHeaderMail = string.Empty;
        public string PhraseHeaderPhone = string.Empty;
        public string PhraseHeaderSubscriptions = string.Empty;
        public string PhraseHeaderTags = string.Empty;

        public ContactListDataViewModel(IDatabase database, Translator translator, IEnumerable<Contact> contacts, SearchSettings settings, Session session)
        {
            ShowOrganization = settings.Columns.Value.HasFlag(ContactColumns.Organization);
            ShowName = settings.Columns.Value.HasFlag(ContactColumns.Name);
            ShowStreet = settings.Columns.Value.HasFlag(ContactColumns.Street);
            ShowPlace = settings.Columns.Value.HasFlag(ContactColumns.Place);
            ShowState = settings.Columns.Value.HasFlag(ContactColumns.State);
            ShowMail = settings.Columns.Value.HasFlag(ContactColumns.Mail);
            ShowPhone = settings.Columns.Value.HasFlag(ContactColumns.Phone);
            ShowSubscriptions = settings.Columns.Value.HasFlag(ContactColumns.Subscriptions);
            ShowTags = settings.Columns.Value.HasFlag(ContactColumns.Tags);
            List = new List<ContactListItemViewModel>(contacts.Select(p => new ContactListItemViewModel(database, translator, p, session, settings)));

            PhraseHeaderOrganization = translator.Get("Contact.List.Header.Organization", "Column 'Organization' in the contact list page", "Organization").EscapeHtml();
            PhraseHeaderLastName = translator.Get("Contact.List.Header.LastName", "Column 'Last name' in the contact list page", "Last name").EscapeHtml();
            PhraseHeaderFirstNames = translator.Get("Contact.List.Header.FirstNames", "Column 'First names' in the contact list page", "First names").EscapeHtml();
            PhraseHeaderStreet = translator.Get("Contact.List.Header.Street", "Column 'Street' in the contact list page", "Street").EscapeHtml();
            PhraseHeaderPlace = translator.Get("Contact.List.Header.Place", "Column 'Place' in the contact list page", "Place").EscapeHtml();
            PhraseHeaderState = translator.Get("Contact.List.Header.State", "Column 'State' in the contact list page", "State/Country").EscapeHtml();
            PhraseHeaderMail = translator.Get("Contact.List.Header.Mail", "Column 'E-Mail' in the contact list page", "E-Mail").EscapeHtml();
            PhraseHeaderPhone = translator.Get("Contact.List.Header.Phone", "Column 'Phone' in the contact list page", "Phone").EscapeHtml();
            PhraseHeaderSubscriptions = translator.Get("Contact.List.Header.Subscriptions", "Column 'Subscriptions' in the contact list page", "Subscriptions").EscapeHtml();
            PhraseHeaderTags = translator.Get("Contact.List.Header.Tags", "Column 'Tags' in the contact list page", "Tags").EscapeHtml();
        }
    }

    public class ContactPageViewModel
    {
        public string Index;
        public string Number;
        public string Options;

        public ContactPageViewModel(int index, bool selected)
        {
            Index = index.ToString();
            Number = (index + 1).ToString();
            Options = selected ? "selected" : string.Empty;
        }
    }

    public class ContactItemsPerPageViewModel
    {
        public string Count;
        public string Options;

        public ContactItemsPerPageViewModel(int count, bool selected)
        {
            Count = count.ToString();
            Options = selected ? "selected" : string.Empty;
        }
    }

    public class ContactPagesViewModel
    {
        public string CurrentPageNumber;
        public string CurrentItemsPerPage;
        public List<ContactPageViewModel> Pages;
        public List<ContactItemsPerPageViewModel> ItemsPerPage;

        public string PhrasePage = string.Empty;
        public string PhrasePerPage = string.Empty;

        public ContactPagesViewModel(Translator translator, int pageCount, SearchSettings settings)
        {
            CurrentPageNumber = (settings.CurrentPage + 1).ToString();
            Pages = new List<ContactPageViewModel>();
            for (int i = 0; i < pageCount; i++)
            {
                Pages.Add(new ContactPageViewModel(i, settings.CurrentPage == i));
            }
            CurrentItemsPerPage = settings.ItemsPerPage.ToString();
            ItemsPerPage = new List<ContactItemsPerPageViewModel>();
            foreach (int i in new int[] { 10, 15, 20, 25, 30, 40, 50 })
            {
                ItemsPerPage.Add(new ContactItemsPerPageViewModel(i, settings.ItemsPerPage == i));
            }

            PhrasePage = translator.Get("Contact.List.Pages.Page", "In paging of the contact list page", "Page").EscapeHtml();
            PhrasePerPage = translator.Get("Contact.List.Pages.PerPage", "In paging of the contact list page", "per Page").EscapeHtml();
        }
    }

    public class ContactListViewModel : MasterViewModel
    {
        public string PhraseSearch = string.Empty;
        public string PhraseFilter = string.Empty;
        public List<NamedIdViewModel> Subscriptions;
        public List<NamedIdViewModel> Tags;
        public List<NamedIntViewModel> Columns;

        public ContactListViewModel(IDatabase database, Translator translator, Session session)
            : base(translator, 
                   translator.Get("Contact.List.Title", "Title of the contact list page", "Liste"), 
                   session)
        {
            PhraseSearch = translator.Get("Contact.List.Search", "Hint in the search box of the contact list page", "Search").EscapeHtml();
            PhraseFilter = translator.Get("Contact.List.Filter", "Button 'Filter' on the contact list page", "Filter").EscapeHtml();

            Subscriptions = new List<NamedIdViewModel>(database
                .Query<Feed>()
                .Select(f => new NamedIdViewModel(translator, f, false))
                .OrderBy(f => f.Name));
            Subscriptions.Add(new NamedIdViewModel(translator.Get("Contact.List.Filter.None", "None filter value", "None"), false, true));
            Tags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            Tags.Add(new NamedIdViewModel(translator.Get("Contact.List.Filter.None", "None filter value", "None"), false, true));
            Columns = new List<NamedIntViewModel>(ContactColumnsExtensions.Flags
                .Select(f => new NamedIntViewModel(translator, f, false)));
        }
    }

    public class SearchSettingsUpdate
    {
        public Guid Id;
        public string Name;
        public string FilterSubscriptionId;
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
            FilterSubscriptionId =
                settings.FilterSubscription.Value == null ?
                string.Empty :
                settings.FilterSubscription.Value.Id.Value.ToString();
            FilterTagId =
                settings.FilterTag.Value == null ?
                string.Empty :
                settings.FilterTag.Value.Id.Value.ToString();
            FilterText = settings.FilterText.Value;
            ItemsPerPage = settings.ItemsPerPage.Value;
            CurrentPage = settings.CurrentPage.Value;
            Columns = ContactColumnsExtensions.Flags
                .Where(f => settings.Columns.Value.HasFlag(f))
                .Select(f => (int)f)
                .ToArray();
        }

        public void Apply(IDatabase database, SearchSettings settings)
        {
            settings.Name.Value = Name;
            settings.FilterSubscription.Value = database.Query<Feed>(FilterSubscriptionId);
            settings.FilterTag.Value = database.Query<Tag>(FilterTagId);
            settings.FilterText.Value = FilterText;
            settings.ItemsPerPage.Value = ItemsPerPage;
            settings.CurrentPage.Value = CurrentPage;
            var flag = ContactColumns.None;
            foreach (var f in Columns)
            {
                flag |= (ContactColumns)f;
            }
            settings.Columns.Value = flag;
        }
    }

    public class ContactListModule : PublicusModule
    {
        private bool Filter(Contact contact, SearchSettings settings)
        {
            var subscriptionFilter =
                settings.FilterSubscription.Value == null ||
                contact.Subscriptions.Any(m => m.Feed.Value == settings.FilterSubscription.Value);
            var tagFilter =
                settings.FilterTag.Value == null ||
                contact.TagAssignments.Any(t => t.Tag.Value == settings.FilterTag.Value);
            var textFilter =
                contact.Organization.Value.Contains(settings.FilterText) ||
                contact.FullName.Contains(settings.FilterText) ||
                contact.ServiceAddresses.Any(a => a.Address.Value.Contains(settings.FilterText)) ||
                contact.PostalAddresses.Any(a => a.Text(Translator).Contains(settings.FilterText));
            var accessFilter = HasAccess(contact, PartAccess.Anonymous, AccessRight.Read);
            return subscriptionFilter && tagFilter && textFilter && accessFilter;
        }

        public ContactListModule()
        {
            this.RequiresAuthentication();

            Get["/contact/list"] = parameters =>
            {
                return View["View/contactlist.sshtml", new ContactListViewModel(Database, Translator, CurrentSession)];
            };
            Get["/contact/list/settings/list"] = parameters =>
            {
                var settingsList = Database
                    .Query<SearchSettings>(DC.Equal("userid", CurrentSession.User.Id.Value))
                    .ToList();
                var result = new JArray();

                if (!settingsList.Any())
                {
                    var settings = new SearchSettings(Guid.NewGuid());
                    settings.User.Value = CurrentSession.User;
                    settings.Name.Value = Translate("Contact.List.Settings.DefaultName", "Default name for new search settings", "Default");
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
            Get["/contact/list/settings/get/{ssid}"] = parameters =>
            {
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);

                if (settings == null ||
                    settings.User.Value != CurrentSession.User)
                {
                    return null;
                }

                var update = new SearchSettingsUpdate(settings);
                return JsonConvert.SerializeObject(update).ToString();
            };
            Post["/contact/list/settings/set/{ssid}"] = parameters =>
            {
                var update = JsonConvert.DeserializeObject<SearchSettingsUpdate>(ReadBody());
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);
                var status = CreateStatus();

                if (settings == null)
                {
                    settings = new SearchSettings(Guid.NewGuid());
                    settings.User.Value = CurrentSession.User;
                }
                else if (settings.User.Value != CurrentSession.User)
                {
                    status.SetErrorAccessDenied();
                }

                update.Apply(Database, settings);
                Database.Save(settings);
                return status.CreateJsonData();
            };
            Get["/contact/list/data/{ssid}"] = parameters =>
            {
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);
                if (settings == null) return null;
                var contacts = Database.Query<Contact>()
                    .Where(c => Filter(c, settings));
                var skip = settings.ItemsPerPage * settings.CurrentPage;
                if (skip > contacts.Count()) skip = 0;
                var page = contacts
                    .OrderBy(c => c.SortName)
                    .Skip(skip)
                    .Take(settings.ItemsPerPage);
                return View["View/contactlist_data.sshtml", new ContactListDataViewModel(Database, Translator, page, settings, CurrentSession)];
            };
            Get["/contact/list/pages/{ssid}"] = parameters =>
            {
                string searchSettingsId = parameters.ssid;
                var settings = Database.Query<SearchSettings>(searchSettingsId);
                if (settings == null) return null;
                var personCount = Database.Query<Contact>()
                    .Count(c => Filter(c, settings));
                var itemsPerPage = Math.Max(1, settings.ItemsPerPage.Value);
                var pageCount = Math.Max(1, (personCount / itemsPerPage) + Math.Min(personCount % itemsPerPage, 1));

                if (settings.CurrentPage.Value >= pageCount)
                {
                    settings.CurrentPage.Value = 0;
                    Database.Save(settings);
                }

                return View["View/contactlist_pages.sshtml", new ContactPagesViewModel(Translator, pageCount, settings)];
            };
        }
    }
}

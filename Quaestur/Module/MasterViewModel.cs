using System.Collections.Generic;
using System.Linq;
using System;
using Nancy;
using Newtonsoft.Json;

namespace Quaestur
{
    public class PostResult
    {
        public bool IsSuccess;
        public string MessageType;
        public string MessageText;

        public static string Success(string message)
        {
            var status = new PostResult();
            status.IsSuccess = true;
            status.MessageType = "success";
            status.MessageText = message;
            return JsonConvert.SerializeObject(status); 
        }

        public static string Failed(string message)
        {
            var status = new PostResult();
            status.IsSuccess = false;
            status.MessageType = "warning";
            status.MessageText = message;
            return JsonConvert.SerializeObject(status);
        }
    }

    public class MasterViewModel
    {
        public string UserId = String.Empty;
        public string UserName = string.Empty;
        public string Title = string.Empty;
        public bool NavBar = false;
        public bool NavCustom = false;
        public bool NavPersonNew = false;
        public bool NavExport = false;
        public bool NavMailing = false;
        public bool NavBallot = false;
        public bool NavOrganization = false;
        public bool NavSettings = false;
        public string PhraseMenuPersons = string.Empty;
        public string PhraseMenuPersonsList = string.Empty;
        public string PhraseMenuPersonNew = string.Empty;
        public string PhraseMenuExport = string.Empty;
        public string PhraseMenuOrganizations = string.Empty;
        public string PhraseMenuCustom = string.Empty;
        public string PhraseMenuCountries = string.Empty;
        public string PhraseMenuStates = string.Empty;
        public string PhraseMenuTags = string.Empty;
        public string PhraseMenuPhrases = string.Empty;
        public string PhraseMenuMailings = string.Empty;
        public string PhraseMenuNewMailing = string.Empty;
        public string PhraseMenuListMailings = string.Empty;
        public string PhraseMenuMailingElement = string.Empty;
        public string PhraseMenuProfile = string.Empty;
        public string PhraseMenuChangePassword = string.Empty;
        public string PhraseMenuLogout = string.Empty;
        public string PhraseMenuSettings = string.Empty;
        public string PhraseMenuOAuth2Clients = string.Empty;
        public string PhraseMenuBallot = string.Empty;
        public string PhraseMenuBallotList = string.Empty;
        public string PhraseMenuBallotTemplates = string.Empty;

        public MasterViewModel()
        {
        }

        public MasterViewModel(Translator translator, string title, Session session)
        {
            Title = title.EscapeHtml();
            UserId = session != null ? session.User.Id.Value.ToString() : string.Empty;
            UserName = session != null ? session.User.UserName.Value : string.Empty;
            NavBar = session != null;
            NavCustom = session != null && session.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write);
            NavPersonNew = session != null && session.HasPersonNewAccess();
            NavExport = session != null && session.HasAnyOrganizationAccess(PartAccess.Contact, AccessRight.Read);
            NavMailing = session != null && session.HasAnyOrganizationAccess(PartAccess.Contact, AccessRight.Write);
            NavBallot = session != null && session.HasAnyOrganizationAccess(PartAccess.Ballot, AccessRight.Read);
            NavOrganization = session != null && session.HasAnyOrganizationAccess(PartAccess.Structure, AccessRight.Read);
            NavSettings = session != null && session.HasAnyOrganizationAccess(PartAccess.Crypto, AccessRight.Read);
            PhraseMenuPersons = translator.Get("Master.Menu.Persons", "Item 'Persons' in the main menu", "Persons").EscapeHtml();
            PhraseMenuPersonsList = translator.Get("Master.Menu.Persons.List", "Item 'List' under 'Persons' in the main menu", "List").EscapeHtml();
            PhraseMenuPersonNew = translator.Get("Master.Menu.Persons.New", "Item 'New' under 'Persons' in the main menu", "New").EscapeHtml();
            PhraseMenuExport = translator.Get("Master.Menu.Persons.Export", "Item 'Export' under 'Persons' in the main menu", "Export").EscapeHtml();
            PhraseMenuCustom = translator.Get("Master.Menu.Custom", "Item 'Custom' in the main menu", "Custom").EscapeHtml();
            PhraseMenuOrganizations = translator.Get("Master.Menu.Custom.Organizations", "Item 'Organizations' in the main menu", "Organizations").EscapeHtml();
            PhraseMenuCountries = translator.Get("Master.Menu.Custom.Countries", "Item 'Countries' under 'Custom' in the main menu", "Countries").EscapeHtml();
            PhraseMenuStates = translator.Get("Master.Menu.Custom.States", "Item 'States' under 'Custom' in the main menu", "States").EscapeHtml();
            PhraseMenuTags = translator.Get("Master.Menu.Custom.Tags", "Item 'Tags' under 'Custom' in the main menu", "Tags").EscapeHtml();
            PhraseMenuPhrases = translator.Get("Master.Menu.Custom.Translations", "Item 'Translations' under 'Custom' in the main menu", "Translations").EscapeHtml();
            PhraseMenuMailings = translator.Get("Master.Menu.Mailings", "Item 'Mailings' in the main menu", "Mailings").EscapeHtml();
            PhraseMenuNewMailing = translator.Get("Master.Menu.Mailings.New", "Item 'New mailing' under 'Mailings' in the main menu", "New mailing").EscapeHtml();
            PhraseMenuListMailings = translator.Get("Master.Menu.Mailings.List", "Item 'List mailings' under 'Mailings' in the main menu", "List").EscapeHtml();
            PhraseMenuMailingElement = translator.Get("Master.Menu.Mailings.Elements", "Item 'Elements' under 'Mailings' in the main menu", "Elements").EscapeHtml();
            PhraseMenuProfile = translator.Get("Master.Menu.User.Profile", "Item 'Profile' under user in the main menu", "Profile").EscapeHtml();
            PhraseMenuChangePassword = translator.Get("Master.Menu.User.ChangePassword", "Item 'Change password' under user in the main menu", "Change password").EscapeHtml();
            PhraseMenuLogout = translator.Get("Master.Menu.User.Logout", "Item 'Logout' under user in the main menu", "Logut").EscapeHtml();
            PhraseMenuSettings = translator.Get("Master.Menu.Settings", "Menu 'Settings' in the main menu", "Settings").EscapeHtml();
            PhraseMenuOAuth2Clients = translator.Get("Master.Menu.Settings.OAuth2Clients", "Item 'OAuth2 Clients' under settings in the main menu", "OAuth2 Clients").EscapeHtml();
            PhraseMenuBallot = translator.Get("Master.Menu.Settings.Ballot", "Menu 'Ballot' in the main menu", "Ballots").EscapeHtml();
            PhraseMenuBallotList = translator.Get("Master.Menu.Settings.BallotList", "Item 'List' under ballots in the main menu", "List").EscapeHtml();
            PhraseMenuBallotTemplates = translator.Get("Master.Menu.Settings.BallotTemplates", "Item 'Templates' under ballots in the main menu", "Tempaltes").EscapeHtml();
        }
    }

    public class InfoViewModel : MasterViewModel
    {
        public string Text { get; private set; }
        public string LinkTitle { get; private set; }
        public string LinkAddress { get; private set; }

        public InfoViewModel(Translator translator, string title, string text, string linkTitle, string linkAddress)
            : base(translator, title, null)
        {
            Text = text.EscapeHtml();
            LinkTitle = linkTitle.EscapeHtml();
            LinkAddress = linkAddress;
        }
    }

    public class AccessDeniedViewModel : InfoViewModel
    {
        public AccessDeniedViewModel(Translator translator)
            : base(translator,
                   translator.Get("Access.Denied.Title", "Title of the access denied page", "Access Denied"),
                   translator.Get("Access.Denied.Text", "Text on the access denied page", "Access to this page is denied."),
                   translator.Get("Access.Denied.Back", "Back text on the access denied page", "Back to dashboard"),
                   "/")
        {
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System;
using Nancy;
using Newtonsoft.Json;

namespace Publicus
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
        public bool NavContactNew = false;
        public bool NavExport = false;
        public bool NavMailing = false;
        public bool NavFeed = false;
        public bool NavSettings = false;
        public string PhraseMenuContacts = string.Empty;
        public string PhraseMenuContactsList = string.Empty;
        public string PhraseMenuContactNew = string.Empty;
        public string PhraseMenuExport = string.Empty;
        public string PhraseMenuFeeds = string.Empty;
        public string PhraseMenuCustom = string.Empty;
        public string PhraseMenuCountries = string.Empty;
        public string PhraseMenuStates = string.Empty;
        public string PhraseMenuTags = string.Empty;
        public string PhraseMenuPhrases = string.Empty;
        public string PhraseMenuMailings = string.Empty;
        public string PhraseMenuNewMailing = string.Empty;
        public string PhraseMenuListMailings = string.Empty;
        public string PhraseMenuMailingElement = string.Empty;
        public string PhraseMenuLogout = string.Empty;
        public string PhraseMenuSettings = string.Empty;

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
            NavContactNew = session != null && session.HasContactNewAccess();
            NavExport = session != null && session.HasAnyFeedAccess(PartAccess.Contact, AccessRight.Read);
            NavMailing = session != null && session.HasAnyFeedAccess(PartAccess.Contact, AccessRight.Write);
            NavFeed = session != null && session.HasAnyFeedAccess(PartAccess.Structure, AccessRight.Read);
            NavSettings = session != null && session.HasAnyFeedAccess(PartAccess.Crypto, AccessRight.Read);
            PhraseMenuContacts = translator.Get("Master.Menu.Contacts", "Item 'Contacts' in the main menu", "Contacts").EscapeHtml();
            PhraseMenuContactsList = translator.Get("Master.Menu.Contacts.List", "Item 'List' under 'Contacts' in the main menu", "List").EscapeHtml();
            PhraseMenuContactNew = translator.Get("Master.Menu.Contacts.New", "Item 'New' under 'Contacts' in the main menu", "New").EscapeHtml();
            PhraseMenuExport = translator.Get("Master.Menu.Contacts.Export", "Item 'Export' under 'Contacts' in the main menu", "Export").EscapeHtml();
            PhraseMenuCustom = translator.Get("Master.Menu.Custom", "Item 'Custom' in the main menu", "Custom").EscapeHtml();
            PhraseMenuFeeds = translator.Get("Master.Menu.Custom.Feeds", "Item 'Feeds' in the main menu", "Feeds").EscapeHtml();
            PhraseMenuCountries = translator.Get("Master.Menu.Custom.Countries", "Item 'Countries' under 'Custom' in the main menu", "Countries").EscapeHtml();
            PhraseMenuStates = translator.Get("Master.Menu.Custom.States", "Item 'States' under 'Custom' in the main menu", "States").EscapeHtml();
            PhraseMenuTags = translator.Get("Master.Menu.Custom.Tags", "Item 'Tags' under 'Custom' in the main menu", "Tags").EscapeHtml();
            PhraseMenuPhrases = translator.Get("Master.Menu.Custom.Translations", "Item 'Translations' under 'Custom' in the main menu", "Translations").EscapeHtml();
            PhraseMenuMailings = translator.Get("Master.Menu.Mailings", "Item 'Mailings' in the main menu", "Mailings").EscapeHtml();
            PhraseMenuNewMailing = translator.Get("Master.Menu.Mailings.New", "Item 'New mailing' under 'Mailings' in the main menu", "New mailing").EscapeHtml();
            PhraseMenuListMailings = translator.Get("Master.Menu.Mailings.List", "Item 'List mailings' under 'Mailings' in the main menu", "List").EscapeHtml();
            PhraseMenuMailingElement = translator.Get("Master.Menu.Mailings.Elements", "Item 'Elements' under 'Mailings' in the main menu", "Elements").EscapeHtml();
            PhraseMenuLogout = translator.Get("Master.Menu.User.Logout", "Item 'Logout' under user in the main menu", "Logut").EscapeHtml();
            PhraseMenuSettings = translator.Get("Master.Menu.Settings", "Menu 'Settings' in the main menu", "Settings").EscapeHtml();
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

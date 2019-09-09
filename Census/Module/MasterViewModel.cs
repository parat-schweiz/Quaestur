using System.Collections.Generic;
using System.Linq;
using System;
using Nancy;
using Newtonsoft.Json;
using SiteLibrary;

namespace Census
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
        public bool NavOrganization = false;
        public bool NavSettings = false;
        public string PhraseMenuQuestionaires = string.Empty;
        public string PhraseMenuQuestionaireList = string.Empty;
        public string PhraseMenuOrganizations = string.Empty;
        public string PhraseMenuCustom = string.Empty;
        public string PhraseMenuPhrases = string.Empty;
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
            NavOrganization = session != null && session.HasAnyOrganizationAccess(PartAccess.Structure, AccessRight.Read);
            NavSettings = session != null && session.HasAnyOrganizationAccess(PartAccess.Crypto, AccessRight.Read);
            PhraseMenuQuestionaires = translator.Get("Master.Menu.Questionaires", "Item 'Questionaires' in the main menu", "Questionaires").EscapeHtml();
            PhraseMenuQuestionaireList = translator.Get("Master.Menu.Custom.QuestionaireList", "Item 'Questionaire list' in the main menu", "List").EscapeHtml();
            PhraseMenuCustom = translator.Get("Master.Menu.Custom", "Item 'Custom' in the main menu", "Custom").EscapeHtml();
            PhraseMenuOrganizations = translator.Get("Master.Menu.Custom.Organizations", "Item 'Organizations' in the main menu", "Organizations").EscapeHtml();
            PhraseMenuPhrases = translator.Get("Master.Menu.Custom.Translations", "Item 'Translations' under 'Custom' in the main menu", "Translations").EscapeHtml();
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

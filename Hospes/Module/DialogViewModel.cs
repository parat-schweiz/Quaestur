using System;
using SiteLibrary;

namespace Hospes
{
    public class DialogViewModel
    {
        public string Title;
        public string DialogId;
        public string ButtonId;
        public string PhraseButtonOk;
        public string PhraseButtonCancel;

        public DialogViewModel()
        {
        }

        public DialogViewModel(Translator translator, string title, string dialogId)
        {
            PhraseButtonOk = translator.Get("Dialog.Button.OK", "Button 'OK' in any dialog", "OK").EscapeHtml();
            PhraseButtonCancel = translator.Get("Dialog.Button.Cancel", "Button 'Cancel' in any dialog", "Cancel").EscapeHtml();
            Title = title.EscapeHtml();
            DialogId = dialogId;
            ButtonId = dialogId + "Button";
        }
    }
}

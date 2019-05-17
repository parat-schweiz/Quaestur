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
    public class SendingTemplateLanguageEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public string Name;
        public string ParentLink;
        public string MailSubject;
        public string MailHtmlText;
        public string LetterLatex;
        public string PhraseFieldMailSubject;
        public string PhraseFieldMailHtmlText;
        public string PhraseFieldLetterLatex;
        public string PhraseButtonCancel;
        public string PhraseButtonSave;
        public string HtmlEditorId;

        public SendingTemplateLanguageEditViewModel()
        {
        }

        public SendingTemplateLanguageEditViewModel(Translator translator, Session session)
            : base(translator,
            translator.Get("SendingTemplateLanguage.Edit.Title", "Title of the sendingTemplateLanguage edit dialog", "Edit bill sending template"),
            session)
        {
            PhraseFieldMailSubject = translator.Get("SendingTemplateLanguage.Edit.Field.MailSubject", "Mail subject field in the bill template edit page", "Mail subject").EscapeHtml();
            PhraseFieldMailHtmlText = translator.Get("SendingTemplateLanguage.Edit.Field.MailHtmlText", "Mail text field in the bill template edit page", "Mail text").EscapeHtml();
            PhraseFieldLetterLatex = translator.Get("SendingTemplateLanguage.Edit.Field.LetterLatex", "Letter LaTeX field in the bill template edit page", "Letter LaTeX").EscapeHtml();
            PhraseButtonCancel = translator.Get("SendingTemplateLanguage.Edit.Button.Cancel", "Cancel button in the bill template edit page", "Cancel").EscapeHtml();
            PhraseButtonSave = translator.Get("SendingTemplateLanguage.Edit.Button.Save", "Save button in the bill template edit page", "Save").EscapeHtml();
            HtmlEditorId = Guid.NewGuid().ToString();
        }

        public SendingTemplateLanguageEditViewModel(Translator translator, IDatabase db, Session session, SendingTemplate sendingTemplate, Language language)
            : this(translator, session)
        {
            Method = "add";
            Id = string.Format("{0}/{1}", sendingTemplate.Id.Value, (int)language);
            var parent = sendingTemplate.Parent(db);
            Name = string.Format("{0} / {1} / {2}", parent.GetText(translator), sendingTemplate.TranslateFieldName(db, translator), language.Translate(translator));
            MailSubject = string.Empty;
            MailHtmlText = string.Empty;
            LetterLatex = string.Empty;
            ParentLink = CreateParentLink(sendingTemplate);
        }

        public SendingTemplateLanguageEditViewModel(Translator translator, IDatabase db, Session session, SendingTemplateLanguage sendingTemplateLanguage)
            : this(translator, session)
        {
            Method = "edit";
            Id = sendingTemplateLanguage.Id.Value.ToString();
            var parent = sendingTemplateLanguage.Template.Value.Parent(db);
            Name = string.Format("{0} / {1} / {2}", parent.GetText(translator), sendingTemplateLanguage.Template.Value.TranslateFieldName(db, translator), sendingTemplateLanguage.Language.Value.Translate(translator));
            MailSubject = sendingTemplateLanguage.MailSubject.Value;
            MailHtmlText = sendingTemplateLanguage.MailHtmlText.Value;
            LetterLatex = sendingTemplateLanguage.LetterLatex.Value;
            ParentLink = CreateParentLink(sendingTemplateLanguage.Template.Value);
        }

        private string CreateParentLink(SendingTemplate sendingTemplate)
        {
            switch (sendingTemplate.ParentType.Value)
            {
                case SendingTemplateParentType.BallotTemplate:
                    return "/ballottemplate/edit/" + sendingTemplate.ParentId.ToString();
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class SendingTemplateLanguageModule : QuaesturModule
    {
        public SendingTemplateLanguageModule()
        {
            RequireCompleteLogin();

            Get("/sendingtemplatelanguage/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var sendingTemplateLanguage = Database.Query<SendingTemplateLanguage>(idString);

                if (sendingTemplateLanguage != null)
                {
                    if (sendingTemplateLanguage.HasAccess(Database, CurrentSession, AccessRight.Write))
                    {
                        return View["View/sendingtemplatelanguageedit.sshtml",
                            new SendingTemplateLanguageEditViewModel(Translator, Database, CurrentSession, sendingTemplateLanguage)];
                    }
                }

                return null;
            });
            Post("/sendingtemplatelanguage/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<SendingTemplateLanguageEditViewModel>(ReadBody());
                var sendingTemplateLanguage = Database.Query<SendingTemplateLanguage>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(sendingTemplateLanguage))
                {
                    if (sendingTemplateLanguage.HasAccess(Database, CurrentSession, AccessRight.Write))
                    {
                        status.AssignStringRequired("MailSubject", sendingTemplateLanguage.MailSubject, model.MailSubject);
                        status.AssignStringFree("LetterLatex", sendingTemplateLanguage.LetterLatex, model.LetterLatex);
                        var worker = new HtmlWorker(model.MailHtmlText);
                        sendingTemplateLanguage.MailHtmlText.Value = worker.CleanHtml;
                        sendingTemplateLanguage.MailPlainText.Value = worker.PlainText;

                        if (status.IsSuccess)
                        {
                            Database.Save(sendingTemplateLanguage);
                            Notice("{0} changed sending sending language {1}", CurrentSession.User.ShortHand, sendingTemplateLanguage);
                        }
                    }
                    else
                    {
                        status.SetErrorAccessDenied(); 
                    }
                }

                return status.CreateJsonData();
            });
            Get("/sendingtemplatelanguage/add/{id}/{lang}", parameters =>
            {
                string idString = parameters.id;
                var sendingTemplate = Database.Query<SendingTemplate>(idString);
                string langString = parameters.lang;
                var language = (Language)int.Parse(langString);

                if (sendingTemplate != null)
                {
                    if (sendingTemplate.HasAccess(Database, CurrentSession, AccessRight.Write))
                    {
                        return View["View/sendingtemplatelanguageedit.sshtml",
                            new SendingTemplateLanguageEditViewModel(Translator, Database, CurrentSession, sendingTemplate, language)];
                    }
                }

                return null;
            });
            Post("/sendingtemplatelanguage/add/{id}/{lang}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<SendingTemplateLanguageEditViewModel>(ReadBody());
                var sendingTemplate = Database.Query<SendingTemplate>(idString);
                var status = CreateStatus();
                string langString = parameters.lang;
                var language = (Language)int.Parse(langString);

                if (status.ObjectNotNull(sendingTemplate))
                {
                    if (sendingTemplate.HasAccess(Database, CurrentSession, AccessRight.Write))
                    {
                        var sendingTemplateLanguage = new SendingTemplateLanguage(Guid.NewGuid());
                        status.AssignStringRequired("MailSubject", sendingTemplateLanguage.MailSubject, model.MailSubject);
                        status.AssignStringFree("LetterLatex", sendingTemplateLanguage.LetterLatex, model.LetterLatex);
                        var worker = new HtmlWorker(model.MailHtmlText);
                        sendingTemplateLanguage.MailHtmlText.Value = worker.CleanHtml;
                        sendingTemplateLanguage.MailPlainText.Value = worker.PlainText;
                        sendingTemplateLanguage.Template.Value = sendingTemplate;
                        sendingTemplateLanguage.Language.Value = language;

                        if (status.IsSuccess)
                        {
                            Database.Save(sendingTemplateLanguage);
                            Notice("{0} added sending sending language {1}", CurrentSession.User.ShortHand, sendingTemplateLanguage);
                        }
                    }
                    else
                    {
                        status.SetErrorAccessDenied();
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

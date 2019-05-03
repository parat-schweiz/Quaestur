using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class MailingElementEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public string Owner;
        public string Name;
        public string Type;
        public string HtmlText;
        public List<NamedIntViewModel> Types;
        public List<NamedIdViewModel> Owners;
        public string HtmlEditorId;
        public string PhraseFieldOwner;
        public string PhraseFieldName;
        public string PhraseFieldType;
        public string PhraseFieldHtmlText;
        public string PhraseButtonCancel;
        public string PhraseButtonSave;

        public MailingElementEditViewModel()
        { 
        }

        public MailingElementEditViewModel(Translator translator, Session session)
            : base(translator, translator.Get("MailingElement.Edit.Title", "Title of the mailingElement edit dialog", "Edit mailing element"), session)
        {
            PhraseFieldOwner = translator.Get("MailingElement.Edit.Field.Owner", "Owner field in the mailing element edit page", "Owner").EscapeHtml();
            PhraseFieldName = translator.Get("MailingElement.Edit.Field.Name", "Name field in the mailing element edit page", "Name").EscapeHtml();
            PhraseFieldType = translator.Get("MailingElement.Edit.Field.Type", "Type field in the mailing element edit page", "Type").EscapeHtml();
            PhraseFieldHtmlText = translator.Get("MailingElement.Edit.Field.HtmlText", "Text field in the mailing element edit page", "Text").EscapeHtml();
            PhraseButtonCancel = translator.Get("MailingElement.Edit.Button.Cancel", "Cancel button in the mailing element edit page", "Cancel").EscapeHtml();
            PhraseButtonSave = translator.Get("MailingElement.Edit.Button.Save", "Save button in the mailing element edit page", "Save").EscapeHtml();
        }

        public MailingElementEditViewModel(Translator translator, IDatabase db, Session session)
            : this(translator, session)
        {
            Method = "add";
            Id = "new";
            Owner = string.Empty;
            Name = string.Empty;
            Type = string.Empty;
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, MailingElementType.Header, false));
            Types.Add(new NamedIntViewModel(translator, MailingElementType.Footer, false));
            HtmlText = string.Empty;
            HtmlEditorId = Guid.NewGuid().ToString();
            Owners = new List<NamedIdViewModel>(db
                .Query<Feed>()
                .Where(o => session.HasAccess(o, PartAccess.Mailings, AccessRight.Write))
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
        }

        public MailingElementEditViewModel(Translator translator, IDatabase db, Session session, MailingElement mailingElement)
            : this(translator, session)
        {
            Method = "edit";
            Id = mailingElement.Id.ToString();
            Owner = string.Empty;
            Name = mailingElement.Name.Value.EscapeHtml();
            Type = ((int)mailingElement.Type.Value).ToString();
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, MailingElementType.Header, mailingElement.Type.Value == MailingElementType.Header));
            Types.Add(new NamedIntViewModel(translator, MailingElementType.Footer, mailingElement.Type.Value == MailingElementType.Footer));
            HtmlText = mailingElement.HtmlText.Value;
            HtmlEditorId = Guid.NewGuid().ToString();
            Owners = new List<NamedIdViewModel>(db
                .Query<Feed>()
                .Where(o => session.HasAccess(o, PartAccess.Mailings, AccessRight.Write))
                .Select(o => new NamedIdViewModel(translator, o, mailingElement.Owner.Value == o))
                .OrderBy(o => o.Name));
        }
    }

    public class MailingElementViewModel : MasterViewModel
    {
        public MailingElementViewModel(Translator translator, Session session)
            : base(translator, translator.Get("MailingElement.List.Title", "Title of the mailingElement list page", "Mailing elements"), 
            session)
        { 
        }
    }

    public class MailingElementListItemViewModel
    {
        public string Id;
        public string Owner;
        public string Name;
        public string Type;
        public string PhraseDeleteConfirmationQuestion;

        public MailingElementListItemViewModel(Translator translator, MailingElement mailingElement)
        {
            Id = mailingElement.Id.Value.ToString();
            Owner = mailingElement.Owner.Value.Name.Value[translator.Language].EscapeHtml();
            Name = mailingElement.Name.Value.EscapeHtml();
            Type = mailingElement.Type.Value.Translate(translator).EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("MailingElement.List.Delete.Confirm.Question", "Delete mailing element confirmation question", "Do you really wish to delete mailing element {0}?", mailingElement.GetText(translator)).EscapeHtml();
        }
    }

    public class MailingElementListViewModel
    {
        public string PhraseHeaderOwner;
        public string PhraseHeaderName;
        public string PhraseHeaderType;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<MailingElementListItemViewModel> List;

        public MailingElementListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderOwner = translator.Get("MailingElement.List.Header.Owner", "Column 'Owner' in the mailing element list", "Owner");
            PhraseHeaderName = translator.Get("MailingElement.List.Header.Name", "Column 'Name' in the mailing element list", "Name");
            PhraseHeaderType = translator.Get("MailingElement.List.Header.Type", "Column 'Type' in the mailing element list", "Type");
            PhraseDeleteConfirmationTitle = translator.Get("MailingElement.List.Delete.Confirm.Title", "Delete mailing element confirmation title", "Delete?");
            PhraseDeleteConfirmationInfo = translator.Get("MailingElement.List.Delete.Confirm.Info", "Delete mailing element confirmation info", "That mailing element will be removed for all mailings.");
            List = new List<MailingElementListItemViewModel>(database
                .Query<MailingElement>()
                .Select(e => new MailingElementListItemViewModel(translator, e))
                .OrderBy(e => e.Name));
        }
    }

    public class MailingElementEdit : PublicusModule
    {
        public MailingElementEdit()
        {
            this.RequiresAuthentication();

            Get["/mailingelement"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Mailings, AccessRight.Read))
                {
                    return View["View/mailingelement.sshtml",
                        new MailingElementViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            };
            Get["/mailingelement/list"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Mailings, AccessRight.Read))
                {
                    return View["View/mailingelementlist.sshtml",
                        new MailingElementListViewModel(Translator, Database)];
                }
                return null;
            };
            Get["/mailingelement/edit/{id}"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Mailings, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var mailingElement = Database.Query<MailingElement>(idString);

                    if (mailingElement != null)
                    {
                        return View["View/mailingelementedit.sshtml",
                            new MailingElementEditViewModel(Translator, Database, CurrentSession, mailingElement)];
                    }
                }

                return null;
            };
            Post["/mailingelement/edit/{id}"] = parameters =>
            {
                var status = CreateStatus();
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<MailingElementEditViewModel>(ReadBody());
                    var mailingElement = Database.Query<MailingElement>(idString);

                if (status.ObjectNotNull(mailingElement))
                {
                    if (status.HasAccess(mailingElement.Owner.Value, PartAccess.Mailings, AccessRight.Write))
                    {
                        status.AssignObjectIdString("Owner", mailingElement.Owner, model.Owner);
                        status.AssignStringRequired("Name", mailingElement.Name, model.Name);
                        status.AssignEnumIntString("Type", mailingElement.Type, model.Type);
                        var worker = new HtmlWorker(model.HtmlText);
                        mailingElement.HtmlText.Value = worker.CleanHtml;
                        mailingElement.PlainText.Value = worker.PlainText;

                        if (status.IsSuccess)
                        {
                            Database.Save(mailingElement);
                            Notice("{0} changed mailing element {1}", CurrentSession.User.UserName.Value, mailingElement);
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/mailingelement/add"] = parameters =>
            {
                if (HasAnyFeedAccess(PartAccess.Mailings, AccessRight.Write))
                {
                    return View["View/mailingelementedit.sshtml",
                        new MailingElementEditViewModel(Translator, Database, CurrentSession)];
                }
                return AccessDenied();
            };
            Post["/mailingelement/add/new"] = parameters =>
            {
                var model = JsonConvert.DeserializeObject<MailingElementEditViewModel>(ReadBody());
                var status = CreateStatus();
                var mailingElement = new MailingElement(Guid.NewGuid());
                status.AssignObjectIdString("Owner", mailingElement.Owner, model.Owner);
                status.AssignStringRequired("Name", mailingElement.Name, model.Name);
                status.AssignEnumIntString("Type", mailingElement.Type, model.Type);
                var worker = new HtmlWorker(model.HtmlText);
                mailingElement.HtmlText.Value = worker.CleanHtml;
                mailingElement.PlainText.Value = worker.PlainText;

                if (status.HasAccess(mailingElement.Owner.Value, PartAccess.Mailings, AccessRight.Write))
                {
                    if (status.IsSuccess)
                    {
                        Database.Save(mailingElement);
                        Notice("{0} added mailing element {1}", CurrentSession.User.UserName.Value, mailingElement);
                    }
                }

                return status.CreateJsonData();
            };
            Get["/mailingelement/delete/{id}"] = parameters =>
            {
                var status = CreateStatus();
                string idString = parameters.id;
                var mailingElement = Database.Query<MailingElement>(idString);

                if (status.ObjectNotNull(mailingElement))
                {
                    if (status.HasAccess(mailingElement.Owner.Value, PartAccess.Mailings, AccessRight.Write))
                    {
                        mailingElement.Delete(Database);
                        Notice("{0} deleted mailing element {1}", CurrentSession.User.UserName.Value, mailingElement);
                    }
                }
                return status.CreateJsonData();
            };
        }
    }
}

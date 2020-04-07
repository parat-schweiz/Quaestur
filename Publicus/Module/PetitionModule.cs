using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibrary;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Publicus
{
    public class PetitionEditViewModel : MasterViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Label;
        public string Group;
        public List<NamedIdViewModel> Groups;
        public List<MultiItemViewModel> Text;
        public List<MultiItemViewModel> WebAddress;
        public List<MultiItemViewModel> ShareText;
        public string PetitionTag;
        public List<NamedIdViewModel> PetitionTags;
        public string SpecialNewsletterTag;
        public List<NamedIdViewModel> SpecialNewsletterTags;
        public string GeneralNewsletterTag;
        public List<NamedIdViewModel> GeneralNewsletterTags;
        public string ShowPubliclyTag;
        public List<NamedIdViewModel> ShowPubliclyTags;
        public List<NamedIdViewModel> ConfirmationMails;
        public string[] ConfirmationMailTemplates;
        public string PhraseFieldGroup;
        public string PhraseFieldPetitionTag;
        public string PhraseFieldSpecialNewsletterTag;
        public string PhraseFieldGeneralNewsletterTag;
        public string PhraseFieldShowPubliclyTag;
        public string PhraseFieldConfirmationMailTemplates;
        public string PhraseButtonSave;
        public string PhraseButtonCancel;

        public PetitionEditViewModel()
        { 
        }

        public PetitionEditViewModel(Translator translator, Session session)
            : base(translator, translator.Get("Petition.Edit.Title", "Title of the petition edit page", "Edit petition"), session)
        {
            PhraseFieldGroup = translator.Get("Petition.Edit.Field.Group", "Group field in the petition edit page", "Group");
            PhraseButtonSave = translator.Get("Petition.Edit.Button.Save", "Save button in the petition edit page", "Save").EscapeHtml();
            PhraseButtonCancel = translator.Get("Petition.Edit.Button.Cancel", "Cancel button in the petition edit page", "Cancel").EscapeHtml();
            PhraseFieldPetitionTag = translator.Get("Petition.Edit.Field.PetitionTag", "Petition tag field in the petition edit page", "Petition tag");
            PhraseFieldSpecialNewsletterTag = translator.Get("Petition.Edit.Field.SpecialNewsletterTag", "Special mewsletter tag field in the petition edit page", "Special mewsletter tag");
            PhraseFieldGeneralNewsletterTag = translator.Get("Petition.Edit.Field.GeneralNewsletterTag", "General newsletter tag field in the petition edit page", "General newsletter tag");
            PhraseFieldShowPubliclyTag = translator.Get("Petition.Edit.Field.ShowPubliclyTag", "Show publicly tag field in the petition edit page", "Show publicly tag");
            PhraseFieldConfirmationMailTemplates = translator.Get("Petition.Edit.Field.ConfirmationMailTemplates", "Confirmation mail templates field in the petition edit page", "Confirmation mail templates");
        }

        public PetitionEditViewModel(IDatabase database, Translator translator, Session session)
            : this(translator, session)
        {
            Method = "add";
            Id = "new";
            Label = translator.CreateLanguagesMultiItem("Petition.Edit.Field.Label", "Label field in the petition edit dialog", "Label ({0})", new MultiLanguageString());
            Text = translator.CreateLanguagesMultiItem("Petition.Edit.Field.Text", "Text field in the petition edit dialog", "Text ({0})", new MultiLanguageString());
            WebAddress = translator.CreateLanguagesMultiItem("Petition.Edit.Field.WebAddress", "Web address field in the petition edit dialog", "Web address ({0})", new MultiLanguageString());
            ShareText = translator.CreateLanguagesMultiItem("Petition.Edit.Field.ShareText", "Share text field in the petition edit dialog", "Share text ({0})", new MultiLanguageString());
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Petition, AccessRight.Write))
                .Select(g => new NamedIdViewModel(translator, g, false))
                .OrderBy(g => g.Name));
            PetitionTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            SpecialNewsletterTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            GeneralNewsletterTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            ShowPubliclyTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
            ConfirmationMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.AssignmentType.Value == TemplateAssignmentType.Petition)
                .Where(t => session.HasAccess(t.Feed.Value, PartAccess.Petition, AccessRight.Read))
                .Select(t => new NamedIdViewModel(translator, t, false)));
        }

        public PetitionEditViewModel(IDatabase database, Translator translator, Session session, Petition petition)
            : this(translator, session)
        {
            Method = "edit";
            Id = petition.Id.ToString();
            Label = translator.CreateLanguagesMultiItem("Petition.Edit.Field.Label", "Label field in the petition edit dialog", "Label ({0})", petition.Label.Value);
            Text = translator.CreateLanguagesMultiItem("Petition.Edit.Field.Text", "Text field in the petition edit dialog", "Text ({0})", petition.Text.Value);
            WebAddress = translator.CreateLanguagesMultiItem("Petition.Edit.Field.WebAddress", "Web address field in the petition edit dialog", "Web address ({0})", petition.WebAddress.Value);
            ShareText = translator.CreateLanguagesMultiItem("Petition.Edit.Field.ShareText", "Share text field in the petition edit dialog", "Share text ({0})", petition.ShareText.Value);
            Groups = new List<NamedIdViewModel>(database
                .Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Petition, AccessRight.Write))
                .Select(g => new NamedIdViewModel(translator, g, g == petition.Group.Value))
                .OrderBy(g => g.Name));
            PetitionTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, t == petition.PetitionTag.Value))
                .OrderBy(t => t.Name));
            SpecialNewsletterTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, t == petition.SpecialNewsletterTag.Value))
                .OrderBy(t => t.Name));
            GeneralNewsletterTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, t == petition.GeneralNewsletterTag.Value))
                .OrderBy(t => t.Name));
            ShowPubliclyTags = new List<NamedIdViewModel>(database
                .Query<Tag>()
                .Select(t => new NamedIdViewModel(translator, t, t == petition.ShowPubliclyTag.Value))
                .OrderBy(t => t.Name));
            ConfirmationMails = new List<NamedIdViewModel>(database
                .Query<MailTemplate>()
                .Where(t => t.AssignmentType.Value == TemplateAssignmentType.Petition)
                .Where(t => session.HasAccess(t.Feed.Value, PartAccess.Petition, AccessRight.Read))
                .Select(t => new NamedIdViewModel(translator, t, petition.ConfirmationMails(database).Any(x => x.Template.Value == t))));
        }
    }

    public class PetitionViewModel : MasterViewModel
    {
        public PetitionViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("Petition.List.Title", "Title of the petition list page", "Petitions"), 
            session)
        { 
        }
    }

    public class PetitionListItemViewModel
    {
        public string Id;
        public string Group;
        public string Label;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public PetitionListItemViewModel(IDatabase database, Session session, Translator translator, Petition petition)
        {
            Id = petition.Id.Value.ToString();
            Group = petition.Group.Value.GetText(translator).EscapeHtml();
            Label = petition.Label.Value[translator.Language].EscapeHtml();
            Editable = session.HasAccess(petition.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Write) ? "editable" : string.Empty;
            PhraseDeleteConfirmationQuestion = translator.Get("Petition.List.Delete.Confirm.Question", "Delete petition confirmation question", "Do you really wish to delete petition {0}?", petition.GetText(translator));
        }
    }

    public class PetitionListViewModel
    {
        public string PhraseHeaderGroup;
        public string PhraseHeaderLabel;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<PetitionListItemViewModel> List;

        public PetitionListViewModel(IDatabase database, Translator translator, Session session)
        {
            PhraseHeaderGroup = translator.Get("Petition.List.Header.Group", "Column 'Group' in the petition list", "Group").EscapeHtml();
            PhraseHeaderLabel = translator.Get("Petition.List.Header.Label", "Column 'Label' in the petition list", "Label").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Petition.List.Delete.Confirm.Title", "Delete petition confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Petition.List.Delete.Confirm.Info", "Delete petition confirmation info", "This will also delete all assignments of this petition.").EscapeHtml();
            List = new List<PetitionListItemViewModel>(
                database.Query<Petition>()
                .Where(t => session.HasAccess(t.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Read))
                .Select(c => new PetitionListItemViewModel(database, session, translator, c))
                .OrderBy(t => t.Label));
        }
    }

    public class PetitionEdit : PublicusModule
    {
        public PetitionEdit()
        {
            this.RequiresAuthentication();

            Get("/petition", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/petition.sshtml",
                        new PetitionViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/petition/list", parameters =>
            {
                if (SomeAccess(AccessRight.Read))
                {
                    return View["View/petitionlist.sshtml",
                        new PetitionListViewModel(Database, Translator, CurrentSession)];
                }
                return AccessDenied();
            });
            Get("/petition/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);

                if (petition != null)
                {
                    if (HasAccess(petition.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Write))
                    {
                        return View["View/petitionedit.sshtml",
                            new PetitionEditViewModel(Database, Translator, CurrentSession, petition)];
                    }
                }

                return string.Empty;
            });
            Post("/petition/edit/{id}", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PetitionEditViewModel>(ReadBody());
                var petition = Database.Query<Petition>(idString);

                if (status.ObjectNotNull(petition))
                {
                    if (HasAccess(petition.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Label", petition.Label, model.Label);
                        status.AssignObjectIdString("Group", petition.Group, model.Group);
                        status.AssignMultiLanguageFree("Text", petition.Text, model.Text);
                        status.AssignMultiLanguageFree("WebAddress", petition.WebAddress, model.WebAddress);
                        status.AssignMultiLanguageFree("ShareText", petition.ShareText, model.ShareText);
                        status.AssignObjectIdString("PetitionTag", petition.PetitionTag, model.PetitionTag);
                        status.AssignObjectIdString("SpecialNewsletterTag", petition.SpecialNewsletterTag, model.SpecialNewsletterTag);
                        status.AssignObjectIdString("GeneralNewsletterTag", petition.GeneralNewsletterTag, model.GeneralNewsletterTag);
                        status.AssignObjectIdString("ShowPubliclyTag", petition.ShowPubliclyTag, model.ShowPubliclyTag);

                        if (status.IsSuccess)
                        {
                            if (status.HasAccess(petition.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Write))
                            {
                                using (var transaction = Database.BeginTransaction())
                                {
                                    Database.Save(petition);
                                    status.UpdateMailTemplates(Database, petition.ConfirmationMail, model.ConfirmationMailTemplates);
                                    transaction.Commit();
                                    Notice("{0} changed petition {1}", CurrentSession.User.UserName.Value, petition);
                                }
                            }
                        }
                    }
                    else
                    {
                        status.SetErrorAccessDenied(); 
                    }
                }

                return status.CreateJsonData();
            });
            Get("/petition/add", parameters =>
            {
                if (SomeAccess(AccessRight.Write))
                {
                    return View["View/petitionedit.sshtml",
                        new PetitionEditViewModel(Database, Translator, CurrentSession)];
                }
                return string.Empty;
            });
            Post("/petition/add/new", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PetitionEditViewModel>(ReadBody());
                var petition = new Petition(Guid.NewGuid());
                status.AssignMultiLanguageRequired("Label", petition.Label, model.Label);
                status.AssignObjectIdString("Group", petition.Group, model.Group);
                status.AssignMultiLanguageFree("Text", petition.Text, model.Text);
                status.AssignMultiLanguageFree("WebAddress", petition.WebAddress, model.WebAddress);
                status.AssignMultiLanguageFree("ShareText", petition.ShareText, model.ShareText);
                status.AssignObjectIdString("PetitionTag", petition.PetitionTag, model.PetitionTag);
                status.AssignObjectIdString("SpecialNewsletterTag", petition.SpecialNewsletterTag, model.SpecialNewsletterTag);
                status.AssignObjectIdString("GeneralNewsletterTag", petition.GeneralNewsletterTag, model.GeneralNewsletterTag);
                status.AssignObjectIdString("ShowPubliclyTag", petition.ShowPubliclyTag, model.ShowPubliclyTag);
                petition.EmailKey.Value = Rng.Get(32);

                if (status.IsSuccess)
                {
                    if (status.HasAccess(petition.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            Database.Save(petition);
                            status.UpdateMailTemplates(Database, petition.ConfirmationMail, model.ConfirmationMailTemplates);
                            transaction.Commit();
                            Notice("{0} added petition {1}", CurrentSession.User.UserName.Value, petition);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/petition/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);

                if (petition != null)
                {
                    if (HasAccess(petition.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            petition.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted petition {1}", CurrentSession.User.UserName.Value, petition.GetText(Translator));
                        }
                    }
                }

                return string.Empty;
            });
            Get("/petition/copy/{id}", parameters =>
            {
                string idString = parameters.id;
                var petition = Database.Query<Petition>(idString);

                if (petition != null)
                {
                    if (HasAccess(petition.Group.Value.Feed.Value, PartAccess.Petition, AccessRight.Write))
                    {
                        var newPetition = new Petition(Guid.NewGuid());
                        newPetition.Group.Value = petition.Group.Value;
                        newPetition.Label.Value = petition.Label.Value +
                            Translate("Petition.Copy.Postfix", "Postfix of copied petitions", " (Copy)");
                        newPetition.Text.Value = petition.Text.Value;
                        newPetition.PetitionTag.Value = petition.PetitionTag.Value;
                        newPetition.SpecialNewsletterTag.Value = petition.SpecialNewsletterTag.Value;
                        newPetition.GeneralNewsletterTag.Value = petition.GeneralNewsletterTag.Value;
                        newPetition.EmailKey.Value = Rng.Get(32);

                        using (var transaction = Database.BeginTransaction())
                        {
                            foreach (var oldAssignment in petition.ConfirmationMails(Database))
                            {
                                var newAssignment = new MailTemplateAssignment(Guid.NewGuid());
                                newAssignment.Template.Value = oldAssignment.Template.Value;
                                newAssignment.FieldName.Value = oldAssignment.FieldName.Value;
                                newAssignment.AssignedType.Value = oldAssignment.AssignedType.Value;
                                newAssignment.AssignedId.Value = newPetition.Id.Value;
                                Database.Save(newAssignment);
                            }

                            Database.Save(newPetition);
                            transaction.Commit();

                            Notice("{0} added petition {1}", CurrentSession.User.UserName.Value, newPetition.GetText(Translator));
                        }
                    }
                }

                return string.Empty;
            });
        }

        public bool SomeAccess(AccessRight right)
        {
            return HasAnyFeedAccess(PartAccess.Petition, right);
        }
    }
}

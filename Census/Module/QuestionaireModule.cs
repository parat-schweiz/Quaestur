using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Census
{
    public class QuestionaireEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Owner;
        public List<NamedIdViewModel> Groups;
        public string PhraseFieldOwner;

        public QuestionaireEditViewModel()
        {
        }

        public QuestionaireEditViewModel(Translator translator)
            : base(translator, translator.Get("Questionaire.Edit.Title", "Title of the questionaire edit dialog", "Edit questionaire"), "questionaireEditDialog")
        {
            PhraseFieldOwner = translator.Get("Questionaire.Edit.Field.Owner", "Owner field in the questionaire edit dialog", "Owner").EscapeHtml();
        }

        public QuestionaireEditViewModel(Translator translator, IDatabase database, Session session)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Questionaire.Edit.Field.Name", "Name field in the questionaire edit dialog", "Name ({0})", new MultiLanguageString());
            Groups = new List<NamedIdViewModel>(database.Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Questionaire, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, false)));
        }

        public QuestionaireEditViewModel(Translator translator, IDatabase database, Session session, Questionaire questionaire)
            : this(translator)
        {
            Method = "edit";
            Id = questionaire.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Questionaire.Edit.Field.Name", "Name field in the questionaire edit dialog", "Name ({0})", questionaire.Name.Value);
            Groups = new List<NamedIdViewModel>(database.Query<Group>()
                .Where(g => session.HasAccess(g, PartAccess.Questionaire, AccessRight.Read))
                .Select(g => new NamedIdViewModel(translator, g, g == questionaire.Owner.Value)));
        }
    }

    public class QuestionaireViewModel : MasterViewModel
    {
        public QuestionaireViewModel(Translator translator, Session session)
            : base(translator,
            translator.Get("Questionaire.List.Title", "Title of the questionaire list page", "Questionaires"),
            session)
        {
        }
    }

    public class QuestionaireListItemViewModel
    {
        public string Id;
        public string Name;
        public string Owner;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public QuestionaireListItemViewModel(Translator translator, Session session, Questionaire questionaire)
        {
            Id = questionaire.Id.Value.ToString();
            Name = questionaire.Name.Value[translator.Language].EscapeHtml();
            Owner = questionaire.Owner.Value.Name.Value[translator.Language].EscapeHtml();
            Editable =
                session.HasAccess(questionaire.Owner.Value, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Questionaire.List.Delete.Confirm.Question", "Delete questionaire confirmation question", "Do you really wish to delete questionaire {0}?", questionaire.GetText(translator)).EscapeHtml();
        }
    }

    public class QuestionaireListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseHeaderOwner;
        public string PhraseHeaderSections;
        public string PhraseHeaderVariables;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<QuestionaireListItemViewModel> List;
        public bool AddAccess;

        public QuestionaireListViewModel(Translator translator, Session session, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Questionaire.List.Header.Name", "Column 'Name' in the questionaire list", "Name").EscapeHtml();
            PhraseHeaderOwner = translator.Get("Questionaire.List.Header.Owner", "Column 'Owner' in the questionaire list", "Owner").EscapeHtml();
            PhraseHeaderSections = translator.Get("Questionaire.List.Header.Sections", "Column 'Sections' in the questionaire list", "Sections").EscapeHtml();
            PhraseHeaderVariables = translator.Get("Questionaire.List.Header.Variables", "Column 'Variables' in the questionaire list", "Variables").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Questionaire.List.Delete.Confirm.Title", "Delete questionaire confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Questionaire.List.Delete.Confirm.Info", "Delete questionaire confirmation info", "This will also delete all questions and options under that questionaire.").EscapeHtml();
            List = new List<QuestionaireListItemViewModel>();
            var questionaires = database.Query<Questionaire>();

            foreach (var questionaire in questionaires
                .Where(q => session.HasAccess(q.Owner.Value, PartAccess.Questionaire, AccessRight.Read))
                .OrderBy(o => o.Name.Value[translator.Language]))
            {
                List.Add(new QuestionaireListItemViewModel(translator, session, questionaire));
            }

            AddAccess = session.HasAnyOrganizationAccess(PartAccess.Questionaire, AccessRight.Write);
        }
    }

    public class QuestionaireModule : CensusModule
    {
        public QuestionaireModule()
        {
            this.RequiresAuthentication();

            Get("/questionaire", parameters =>
            {
                return View["View/questionaire.sshtml",
                    new QuestionaireViewModel(Translator, CurrentSession)];
            });
            Get("/questionaire/list", parameters =>
            {
                return View["View/questionairelist.sshtml",
                    new QuestionaireListViewModel(Translator, CurrentSession, Database)];
            });
            Get("/questionaire/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);

                if (questionaire != null)
                {
                    if (HasAccess(questionaire.Owner.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/questionaireedit.sshtml",
                            new QuestionaireEditViewModel(Translator, Database, CurrentSession, questionaire)];
                    }
                }

                return null;
            });
            Post("/questionaire/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<QuestionaireEditViewModel>(ReadBody());
                var questionaire = Database.Query<Questionaire>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(questionaire))
                {
                    if (status.HasAccess(questionaire.Owner.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", questionaire.Name, model.Name);
                        status.AssignObjectIdString("Owner", questionaire.Owner, model.Owner);

                        if (status.IsSuccess)
                        {
                            Database.Save(questionaire);
                            Notice("{0} changed questionaire {1}", CurrentSession.User.UserName.Value, questionaire);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/questionaire/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    return View["View/questionaireedit.sshtml",
                        new QuestionaireEditViewModel(Translator, Database, CurrentSession)];
                }
                return null;
            });
            Post("/questionaire/add/new", parameters =>
            {
                var model = JsonConvert.DeserializeObject<QuestionaireEditViewModel>(ReadBody());
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var questionaire = new Questionaire(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", questionaire.Name, model.Name);
                    status.AssignObjectIdString("Owner", questionaire.Owner, model.Owner);

                    if (status.IsSuccess)
                    {
                        Database.Save(questionaire);
                        Notice("{0} added questionaire {1}", CurrentSession.User.UserName.Value, questionaire);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/questionaire/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(questionaire))
                {
                    if (status.HasAccess(questionaire.Owner.Value, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            questionaire.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted questionaire {1}", CurrentSession.User.UserName.Value, questionaire);
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

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
    public class SectionEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;

        public SectionEditViewModel()
        {
        }

        public SectionEditViewModel(Translator translator)
            : base(translator, translator.Get("Section.Edit.Title", "Title of the section edit dialog", "Edit section"), "sectionEditDialog")
        {
        }

        public SectionEditViewModel(Translator translator, IDatabase db, Session session, Questionaire questionaire)
            : this(translator)
        {
            Method = "add";
            Id = questionaire.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("Section.Edit.Field.Name", "Name field in the section edit dialog", "Name ({0})", new MultiLanguageString());
        }

        public SectionEditViewModel(Translator translator, IDatabase db, Session session, Section section)
            : this(translator)
        {
            Method = "edit";
            Id = section.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Section.Edit.Field.Name", "Name field in the section edit dialog", "Name ({0})", section.Name.Value);
         }
    }

    public class SectionViewModel : MasterViewModel
    {
        public string Id;

        public SectionViewModel(Translator translator, Session session, Questionaire questionaire)
            : base(translator,
            translator.Get("Section.List.Title", "Title of the section list page", "Sections"),
            session)
        {
            Id = questionaire.Id.Value.ToString();
        }
    }

    public class SectionListItemViewModel
    {
        public string Id;
        public string Name;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public SectionListItemViewModel(Translator translator, Session session, Section section)
        {
            Id = section.Id.Value.ToString();
            Name = section.Name.Value[translator.Language];
            Editable =
                session.HasAccess(section.Questionaire.Value.Owner.Value, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Section.List.Delete.Confirm.Section", "Delete section confirmation section", "Do you really wish to delete section {0}?", section.GetText(translator));
        }
    }

    public class SectionListViewModel
    {
        public string Id;
        public string Name;
        public string PhraseHeaderQuestionaire;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public string PhraseHeaderQuestions;
        public List<SectionListItemViewModel> List;
        public bool AddAccess;

        public SectionListViewModel(Translator translator, IDatabase database, Session session, Questionaire questionaire)
        {
            Id = questionaire.Id.Value.ToString();
            Name = questionaire.Name.Value[translator.Language];
            PhraseHeaderQuestionaire = translator.Get("Section.List.Header.Questionaire", "Header part 'Questionaire' in the section list", "Questionaire");
            PhraseDeleteConfirmationTitle = translator.Get("Section.List.Delete.Confirm.Title", "Delete section confirmation title", "Delete?");
            PhraseDeleteConfirmationInfo = translator.Get("Section.List.Delete.Confirm.Info", "Delete section confirmation info", "This will also delete all us under that section.");
            PhraseHeaderQuestions = translator.Get("Section.List.Header.Questions", "Header part 'Questions' in the section list", "Questions");
            List = new List<SectionListItemViewModel>(
                questionaire.Sections
                .Select(g => new SectionListItemViewModel(translator, session, g))
                .OrderBy(g => g.Name));
            AddAccess = session.HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Write);
        }
    }

    public class SectionEdit : CensusModule
    {
        public SectionEdit()
        {
            this.RequiresAuthentication();

            Get("/section/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);

                if (questionaire != null)
                {
                    if (HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/section.sshtml",
                            new SectionViewModel(Translator, CurrentSession, questionaire)];
                    }
                }

                return null;
            });
            Get("/section/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);

                if (questionaire != null)
                {
                    if (HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/sectionlist.sshtml",
                            new SectionListViewModel(Translator, Database, CurrentSession, questionaire)];
                    }
                }

                return null;
            });
            Get("/section/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var section = Database.Query<Section>(idString);

                if (section != null)
                {
                    if (HasAccess(section.Questionaire.Value.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/sectionedit.sshtml",
                        new SectionEditViewModel(Translator, Database, CurrentSession, section)];
                    }
                }

                return null;
            });
            Post("/section/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<SectionEditViewModel>(ReadBody());
                var section = Database.Query<Section>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(section))
                {
                    if (status.HasAccess(section.Questionaire.Value.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", section.Name, model.Name);

                        if (status.IsSuccess)
                        {
                            Database.Save(section);
                            Notice("{0} changed section {1}", CurrentSession.User.UserName.Value, section);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/section/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);

                if (questionaire != null)
                {
                    if (HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/sectionedit.sshtml",
                            new SectionEditViewModel(Translator, Database, CurrentSession, questionaire)];
                    }
                }

                return null;
            });
            Post("/section/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(questionaire))
                {
                    if (status.HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<SectionEditViewModel>(ReadBody());
                        var section = new Section(Guid.NewGuid());
                        status.AssignMultiLanguageRequired("Name", section.Name, model.Name);

                        section.Questionaire.Value = questionaire;

                        if (status.IsSuccess)
                        {
                            Database.Save(section);
                            Notice("{0} added section {1}", CurrentSession.User.UserName.Value, section);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/section/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var section = Database.Query<Section>(idString);

                if (section != null)
                {
                    if (HasAccess(section.Questionaire.Value.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            section.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted section {1}", CurrentSession.User.UserName.Value, section);
                        }
                    }
                }

                return string.Empty;
            });
        }
    }
}

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
    public class QuestionEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Text;
        public string Type;
        public List<NamedIntViewModel> Types;
        public string PhraseFieldType;

        public QuestionEditViewModel()
        {
        }

        public QuestionEditViewModel(Translator translator)
            : base(translator, translator.Get("Question.Edit.Title", "Title of the question edit dialog", "Edit question"), "questionEditDialog")
        {
            PhraseFieldType = translator.Get("Question.Edit.Field.Type", "Type field in the question edit dialog", "Type").EscapeHtml();
            Types = new List<NamedIntViewModel>();
        }

        public QuestionEditViewModel(Translator translator, IDatabase db, Session session, Section section)
            : this(translator)
        {
            Method = "add";
            Id = section.Id.Value.ToString();
            Text = translator.CreateLanguagesMultiItem("Question.Edit.Field.Text", "Text field in the question edit dialog", "Text ({0})", new MultiLanguageString());
            Types.Add(new NamedIntViewModel(translator, QuestionType.SelectOne, false));
            Types.Add(new NamedIntViewModel(translator, QuestionType.SelectMany, false));
        }

        public QuestionEditViewModel(Translator translator, IDatabase db, Session session, Question question)
            : this(translator)
        {
            Method = "edit";
            Id = question.Id.ToString();
            Text = translator.CreateLanguagesMultiItem("Question.Edit.Field.Text", "Text field in the question edit dialog", "Text ({0})", question.Text.Value);
            Types.Add(new NamedIntViewModel(translator, QuestionType.SelectOne, question.Type.Value == QuestionType.SelectOne));
            Types.Add(new NamedIntViewModel(translator, QuestionType.SelectMany, question.Type.Value == QuestionType.SelectMany));
         }
    }

    public class QuestionViewModel : MasterViewModel
    {
        public string Id;

        public QuestionViewModel(Translator translator, Session session, Section section)
            : base(translator,
            translator.Get("Question.List.Title", "Title of the question list page", "Questions"),
            session)
        {
            Id = section.Id.Value.ToString();
        }
    }

    public class QuestionListItemViewModel
    {
        public string Id;
        public string Text;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;
        public string PhraseHeaderOptions;

        public QuestionListItemViewModel(Translator translator, Session session, Question question)
        {
            Id = question.Id.Value.ToString();
            Text = question.Text.Value[translator.Language];
            Editable =
                session.HasAccess(question.Owner, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Question.List.Delete.Confirm.Question", "Delete question confirmation question", "Do you really wish to delete question {0}?", question.GetText(translator));
            switch (question.Type.Value)
            {
                case QuestionType.SelectOne:
                case QuestionType.SelectMany:
                    PhraseHeaderOptions = translator.Get("Question.List.Header.Options", "Link 'Options' caption in the question list", "Options");
                    break;
                default:
                    PhraseHeaderOptions = string.Empty;
                    break;
             }
        }
    }

    public class QuestionListViewModel
    {
        public string ParentId;
        public string Id;
        public string Name;
        public string PhraseHeaderSection;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<QuestionListItemViewModel> List;
        public bool AddAccess;

        public QuestionListViewModel(Translator translator, IDatabase database, Session session, Section section)
        {
            ParentId = section.Questionaire.Value.Id.Value.ToString();
            Id = section.Id.Value.ToString();
            Name = section.Name.Value[translator.Language];
            PhraseHeaderSection = translator.Get("Question.List.Header.Section", "Header part 'Section' in the question list", "Section");
            PhraseDeleteConfirmationTitle = translator.Get("Question.List.Delete.Confirm.Title", "Delete question confirmation title", "Delete?");
            PhraseDeleteConfirmationInfo = translator.Get("Question.List.Delete.Confirm.Info", "Delete question confirmation info", "This will also delete all options under that question.");
            List = new List<QuestionListItemViewModel>(
                section.Questions
                .Select(g => new QuestionListItemViewModel(translator, session, g))
                .OrderBy(g => g.Text));
            AddAccess = session.HasAccess(section.Owner, PartAccess.Questionaire, AccessRight.Write);
        }
    }

    public class QuestionEdit : CensusModule
    {
        public QuestionEdit()
        {
            this.RequiresAuthentication();

            Get("/question/{id}", parameters =>
            {
                string idString = parameters.id;
                var section = Database.Query<Section>(idString);

                if (section != null)
                {
                    if (HasAccess(section.Owner, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/question.sshtml",
                            new QuestionViewModel(Translator, CurrentSession, section)];
                    }
                }

                return null;
            });
            Get("/question/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var section = Database.Query<Section>(idString);

                if (section != null)
                {
                    if (HasAccess(section.Owner, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/questionlist.sshtml",
                            new QuestionListViewModel(Translator, Database, CurrentSession, section)];
                    }
                }

                return null;
            });
            Get("/question/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var question = Database.Query<Question>(idString);

                if (question != null)
                {
                    if (HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/questionedit.sshtml",
                        new QuestionEditViewModel(Translator, Database, CurrentSession, question)];
                    }
                }

                return null;
            });
            Post("/question/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<QuestionEditViewModel>(ReadBody());
                var question = Database.Query<Question>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(question))
                {
                    if (status.HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", question.Text, model.Text);
                        status.AssignEnumIntString("Type", question.Type, model.Type);       

                        if (status.IsSuccess)
                        {
                            Database.Save(question);
                            Notice("{0} changed question {1}", CurrentSession.User.UserName.Value, question);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/question/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var section = Database.Query<Section>(idString);

                if (section != null)
                {
                    if (HasAccess(section.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/questionedit.sshtml",
                            new QuestionEditViewModel(Translator, Database, CurrentSession, section)];
                    }
                }

                return null;
            });
            Post("/question/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var section = Database.Query<Section>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(section))
                {
                    if (status.HasAccess(section.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<QuestionEditViewModel>(ReadBody());
                        var question = new Question(Guid.NewGuid());
                        status.AssignMultiLanguageRequired("Name", question.Text, model.Text);
                        status.AssignEnumIntString("Type", question.Type, model.Type);

                        question.Section.Value = section;

                        if (status.IsSuccess)
                        {
                            Database.Save(question);
                            Notice("{0} added question {1}", CurrentSession.User.UserName.Value, question);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/question/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var question = Database.Query<Question>(idString);

                if (question != null)
                {
                    if (HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            question.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted question {1}", CurrentSession.User.UserName.Value, question);
                        }
                    }
                }

                return string.Empty;
            });
        }
    }
}

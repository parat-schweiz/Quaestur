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
    public class VariableEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Type;
        public List<NamedIntViewModel> Types;
        public string PhraseFieldType;
        public string InitialValue;
        public string PhraseFieldInitialValue;

        public VariableEditViewModel()
        {
        }

        public VariableEditViewModel(Translator translator)
            : base(translator, translator.Get("Variable.Edit.Title", "Title of the variable edit dialog", "Edit variable"), "variableEditDialog")
        {
            PhraseFieldType = translator.Get("Variable.Edit.Field.Type", "Type field in the variable edit dialog", "Type").EscapeHtml();
            PhraseFieldInitialValue = translator.Get("Variable.Edit.Field.InitialValue", "Initial value field in the variable edit dialog", "Initial value").EscapeHtml();
            Types = new List<NamedIntViewModel>();
        }

        public VariableEditViewModel(Translator translator, IDatabase db, Session session, Questionaire questionaire)
            : this(translator)
        {
            Method = "add";
            Id = questionaire.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("Variable.Edit.Field.Name", "Name field in the variable edit dialog", "Name ({0})", new MultiLanguageString());
            InitialValue = string.Empty;
            Types.Add(new NamedIntViewModel(translator, VariableType.Boolean, false));
            Types.Add(new NamedIntViewModel(translator, VariableType.Integer, false));
            Types.Add(new NamedIntViewModel(translator, VariableType.String, false));
            Types.Add(new NamedIntViewModel(translator, VariableType.ListOfBooleans, false));
            Types.Add(new NamedIntViewModel(translator, VariableType.ListOfIntegers, false));
            Types.Add(new NamedIntViewModel(translator, VariableType.ListOfStrings, false));
        }

        public VariableEditViewModel(Translator translator, IDatabase db, Session session, Variable variable)
            : this(translator)
        {
            Method = "edit";
            Id = variable.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Variable.Edit.Field.Name", "Name field in the variable edit dialog", "Name ({0})", variable.Name.Value);
            InitialValue = variable.InitialValue.Value;
            Types.Add(new NamedIntViewModel(translator, VariableType.Boolean, variable.Type.Value == VariableType.Boolean));
            Types.Add(new NamedIntViewModel(translator, VariableType.Integer, variable.Type.Value == VariableType.Integer));
            Types.Add(new NamedIntViewModel(translator, VariableType.String, variable.Type.Value == VariableType.String));
            Types.Add(new NamedIntViewModel(translator, VariableType.ListOfBooleans, variable.Type.Value == VariableType.ListOfBooleans));
            Types.Add(new NamedIntViewModel(translator, VariableType.ListOfIntegers, variable.Type.Value == VariableType.ListOfIntegers));
            Types.Add(new NamedIntViewModel(translator, VariableType.ListOfStrings, variable.Type.Value == VariableType.ListOfStrings));
        }
    }

    public class VariableViewModel : MasterViewModel
    {
        public string Id;

        public VariableViewModel(Translator translator, Session session, Questionaire questionaire)
            : base(translator,
            translator.Get("Variable.List.Title", "Title of the variable list page", "Variables"),
            session)
        {
            Id = questionaire.Id.Value.ToString();
        }
    }

    public class VariableListItemViewModel
    {
        public string Id;
        public string Name;
        public string Type;
        public string PhraseDeleteConfirmationQuestion;

        public VariableListItemViewModel(Translator translator, Session session, Variable variable)
        {
            Id = variable.Id.Value.ToString();
            Name = variable.Name.Value[translator.Language];
            Type = variable.Type.Value.Translate(translator);
            PhraseDeleteConfirmationQuestion = translator.Get("Variable.List.Delete.Confirm.Variable", "Delete variable confirmation variable", "Do you really wish to delete variable {0}?", variable.GetText(translator));
        }
    }

    public class VariableListViewModel
    {
        public string Id;
        public string Name;
        public string PhraseHeaderQuestionaire;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public string PhraseHeaderQuestions;
        public List<VariableListItemViewModel> List;
        public string Editable;
        public bool AddAccess;

        public VariableListViewModel(Translator translator, IDatabase database, Session session, Questionaire questionaire)
        {
            Id = questionaire.Id.Value.ToString();
            Name = questionaire.Name.Value[translator.Language];
            PhraseHeaderQuestionaire = translator.Get("Variable.List.Header.Questionaire", "Header part 'Questionaire' in the variable list", "Questionaire");
            PhraseDeleteConfirmationTitle = translator.Get("Variable.List.Delete.Confirm.Title", "Delete variable confirmation title", "Delete?");
            PhraseDeleteConfirmationInfo = string.Empty;
            PhraseHeaderQuestions = translator.Get("Variable.List.Header.Questions", "Header part 'Questions' in the variable list", "Questions");
            List = new List<VariableListItemViewModel>(
                questionaire.Variables
                .Select(v => new VariableListItemViewModel(translator, session, v))
                .OrderBy(v => v.Name));
            AddAccess = session.HasAccess(questionaire.Owner.Value, PartAccess.Structure, AccessRight.Write);
            Editable = AddAccess ? "editable" : "accessdenied";
        }
    }

    public class VariableEdit : CensusModule
    {
        public VariableEdit()
        {
            this.RequiresAuthentication();

            Get("/variable/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);

                if (questionaire != null)
                {
                    if (HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/variable.sshtml",
                            new VariableViewModel(Translator, CurrentSession, questionaire)];
                    }
                }

                return null;
            });
            Get("/variable/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);

                if (questionaire != null)
                {
                    if (HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/variablelist.sshtml",
                            new VariableListViewModel(Translator, Database, CurrentSession, questionaire)];
                    }
                }

                return null;
            });
            Get("/variable/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var variable = Database.Query<Variable>(idString);

                if (variable != null)
                {
                    if (HasAccess(variable.Questionaire.Value.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/variableedit.sshtml",
                        new VariableEditViewModel(Translator, Database, CurrentSession, variable)];
                    }
                }

                return null;
            });
            Post("/variable/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<VariableEditViewModel>(ReadBody());
                var variable = Database.Query<Variable>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(variable))
                {
                    if (status.HasAccess(variable.Questionaire.Value.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", variable.Name, model.Name);
                        status.AssignEnumIntString("Type", variable.Type, model.Type);
                        status.AssignStringFree("InitialValue", variable.InitialValue, model.InitialValue);
                        Variables.CheckValue(status, variable.Type.Value, variable.InitialValue.Value, "InitialValue");

                        if (status.IsSuccess)
                        {
                            Database.Save(variable);
                            Notice("{0} changed variable {1}", CurrentSession.User.UserName.Value, variable);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/variable/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);

                if (questionaire != null)
                {
                    if (HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/variableedit.sshtml",
                            new VariableEditViewModel(Translator, Database, CurrentSession, questionaire)];
                    }
                }

                return null;
            });
            Post("/variable/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var questionaire = Database.Query<Questionaire>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(questionaire))
                {
                    if (status.HasAccess(questionaire.Owner.Value, PartAccess.Questionaire, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<VariableEditViewModel>(ReadBody());
                        var variable = new Variable(Guid.NewGuid());
                        status.AssignMultiLanguageRequired("Name", variable.Name, model.Name);
                        status.AssignEnumIntString("Type", variable.Type, model.Type);
                        status.AssignStringFree("InitialValue", variable.InitialValue, model.InitialValue);
                        Variables.CheckValue(status, variable.Type.Value, variable.InitialValue.Value, "InitialValue");

                        variable.Questionaire.Value = questionaire;

                        if (status.IsSuccess)
                        {
                            Database.Save(variable);
                            Notice("{0} added variable {1}", CurrentSession.User.UserName.Value, variable);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/variable/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var variable = Database.Query<Variable>(idString);

                if (variable != null)
                {
                    if (HasAccess(variable.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            variable.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted variable {1}", CurrentSession.User.UserName.Value, variable);
                        }
                    }
                }

                return string.Empty;
            });
        }
    }
}

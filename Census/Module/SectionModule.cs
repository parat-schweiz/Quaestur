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
        public List<NamedIntViewModel> ConditionTypes;
        public string ConditionType;
        public List<NamedIdViewModel> ConditionVariables;
        public string ConditionVariable;
        public string ConditionValue;
        public string PhraseFieldConditionType;
        public string PhraseFieldConditionVariable;
        public string PhraseFieldConditionValue;

        public SectionEditViewModel()
        {
        }

        public SectionEditViewModel(Translator translator, IDatabase database, ConditionType conditionType)
            : base(translator, translator.Get("Section.Edit.Title", "Title of the section edit dialog", "Edit section"), "sectionEditDialog")
        {
            PhraseFieldConditionType = translator.Get("Section.Edit.Field.ConditionType", "Condition type field in the option edit dialog", "Condition type").EscapeHtml();
            PhraseFieldConditionVariable = translator.Get("Section.Edit.Field.ConditionVariable", "Condition variable field in the option edit dialog", "Condition variable").EscapeHtml();
            PhraseFieldConditionValue = translator.Get("Section.Edit.Field.ConditionValue", "Condition value field in the option edit dialog", "Condition value").EscapeHtml();
            ConditionTypes = new List<NamedIntViewModel>();
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.None, Census.ConditionType.None == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.Equal, Census.ConditionType.Equal == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.NotEqual, Census.ConditionType.NotEqual == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.Greater, Census.ConditionType.Greater == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.GreaterOrEqual, Census.ConditionType.GreaterOrEqual == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.Lesser, Census.ConditionType.Lesser == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.LesserOrEqual, Census.ConditionType.LesserOrEqual == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.Contains, Census.ConditionType.Contains == conditionType));
            ConditionTypes.Add(new NamedIntViewModel(translator, Census.ConditionType.DoesNotContain, Census.ConditionType.DoesNotContain == conditionType));
        }

        private NamedIdViewModel CreateNoneValue(Translator translator, bool selected)
        {
            return new NamedIdViewModel(
                translator.Get("Section.Edit.Field.ConditionValue.None", "None value in fields on the section edit dialog", "None"),
                false, selected);
        }

        public SectionEditViewModel(Translator translator, IDatabase database, Session session, Questionaire questionaire)
            : this(translator, database, Census.ConditionType.None)
        {
            Method = "add";
            Id = questionaire.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("Section.Edit.Field.Name", "Name field in the section edit dialog", "Name ({0})", new MultiLanguageString());
            ConditionVariables = new List<NamedIdViewModel>();
            ConditionVariables.Add(CreateNoneValue(translator, true));
            ConditionVariables.AddRange(questionaire.Variables
                .Select(v => new NamedIdViewModel(translator, v, false)));
            ConditionValue = string.Empty;
        }

        public SectionEditViewModel(Translator translator, IDatabase database, Session session, Section section)
            : this(translator, database, section.ConditionType.Value)
        {
            Method = "edit";
            Id = section.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Section.Edit.Field.Name", "Name field in the section edit dialog", "Name ({0})", section.Name.Value);
            ConditionVariables = new List<NamedIdViewModel>();
            ConditionVariables.Add(CreateNoneValue(translator, section.ConditionVariable.Value == null));
            ConditionVariables.AddRange(section.Questionaire.Value.Variables
                .Select(v => new NamedIdViewModel(translator, v, section.ConditionVariable.Value == v)));
            ConditionValue = section.ConditionValue.Value;
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
        public string PhraseDeleteConfirmationQuestion;

        public SectionListItemViewModel(Translator translator, Session session, Section section)
        {
            Id = section.Id.Value.ToString();
            Name = section.Name.Value[translator.Language];
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
        public string Editable;
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
                .OrderBy(s => s.Ordering.Value)
                .Select(s => new SectionListItemViewModel(translator, session, s)));
            AddAccess = session.HasAccess(questionaire.Owner.Value, PartAccess.Structure, AccessRight.Write);
            Editable = AddAccess ? "editable" : "accessdenied";
        }
    }

    public class SectionEdit : CensusModule
    {
        private void CheckCondition(PostStatus status, Section section)
        {
            if (section.ConditionType.Value != ConditionType.None &&
                section.ConditionVariable.Value == null)
            {
                status.SetValidationError("ConditionVariable", "Section.Edit.Validation.VariableMissing", "Variable is missing when modification is set in the section edit dialog", "Variable must be set");
            }

            if (section.ConditionVariable.Value != null)
            {
                switch (section.ConditionType.Value)
                {
                    case ConditionType.None:
                        break;
                    case ConditionType.Equal:
                    case ConditionType.NotEqual:
                        if (section.ConditionVariable.Value.Type.Value != VariableType.Boolean &&
                            section.ConditionVariable.Value.Type.Value != VariableType.Double &&
                            section.ConditionVariable.Value.Type.Value != VariableType.Integer &&
                            section.ConditionVariable.Value.Type.Value != VariableType.String)
                        {
                            status.SetValidationError("ConditionVariable", "Section.Edit.Validation.VariableMustBeScalar", "Variable not scalar when logic modification is set in the section edit dialog", "Variable must be of some scalar type");
                        }
                        break;
                    case ConditionType.Greater:
                    case ConditionType.GreaterOrEqual:
                    case ConditionType.Lesser:
                    case ConditionType.LesserOrEqual:
                        if (section.ConditionVariable.Value.Type.Value != VariableType.Double &&
                            section.ConditionVariable.Value.Type.Value != VariableType.Integer)
                        {
                            status.SetValidationError("ConditionVariable", "Section.Edit.Validation.VariableMustBeNumber", "Variable not number when logic modification is set in the section edit dialog", "Variable must be of some number type");
                        }
                        break;
                    case ConditionType.Contains:
                    case ConditionType.DoesNotContain:
                        if (section.ConditionVariable.Value.Type.Value != VariableType.ListOfBooleans &&
                            section.ConditionVariable.Value.Type.Value != VariableType.ListOfDouble &&
                            section.ConditionVariable.Value.Type.Value != VariableType.ListOfIntegers &&
                            section.ConditionVariable.Value.Type.Value != VariableType.ListOfStrings)
                        {
                            status.SetValidationError("ConditionVariable", "Section.Edit.Validation.VariableMustBeList", "Variable not list when logic modification is set in the section edit dialog", "Variable must be of some list type");
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                }

                Variables.CheckValue(status, section.ConditionVariable.Value.Type.Value, section.ConditionValue.Value, "ConditionValue");
            }
        }

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
                        status.AssignEnumIntString("ConditionType", section.ConditionType, model.ConditionType);
                        status.AssignObjectIdString("ConditionVariable", section.ConditionVariable, model.ConditionVariable);
                        status.AssignStringFree("ConditionValue", section.ConditionValue, model.ConditionValue);
                        CheckCondition(status, section);

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
                        status.AssignEnumIntString("ConditionType", section.ConditionType, model.ConditionType);
                        status.AssignObjectIdString("ConditionVariable", section.ConditionVariable, model.ConditionVariable);
                        status.AssignStringFree("ConditionValue", section.ConditionValue, model.ConditionValue);
                        CheckCondition(status, section);

                        section.Ordering.Value = questionaire.Sections.MaxOrDefault(q => q.Ordering.Value, 0) + 1;
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
            Post("/section/switch", parameters =>
            {
                var model = JsonConvert.DeserializeObject<SwitchViewModel>(ReadBody());
                var status = CreateStatus();

                using (var transaction = Database.BeginTransaction())
                {
                    var source = Database.Query<Section>(model.SourceId);
                    var target = Database.Query<Section>(model.TargetId);

                    if (status.ObjectNotNull(source) &&
                        status.ObjectNotNull(target) &&
                        status.HasAccess(source.Owner, PartAccess.Questionaire, AccessRight.Write) &&
                        status.HasAccess(target.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        if (source.Questionaire.Value == target.Questionaire.Value)
                        {
                            var sourcePrecedence = source.Ordering.Value;
                            var targetPrecedence = target.Ordering.Value;
                            source.Ordering.Value = targetPrecedence;
                            target.Ordering.Value = sourcePrecedence;

                            if (source.Dirty || target.Dirty)
                            {
                                Database.Save(source);
                                Database.Save(target);
                                transaction.Commit();
                                Notice("{0} switched {1} and {2}", CurrentSession.User.UserName.Value, source, target);
                            }
                        }
                        else
                        {
                            status.SetErrorNotFound(); 
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

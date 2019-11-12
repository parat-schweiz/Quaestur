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
    public class OptionEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Text;
        public List<NamedIntViewModel> CheckedModifications;
        public string CheckedModification;
        public List<NamedIdViewModel> CheckedVariables;
        public string CheckedVariable;
        public string CheckedValue;
        public List<NamedIntViewModel> UncheckedModifications;
        public string UncheckedModification;
        public List<NamedIdViewModel> UncheckedVariables;
        public string UncheckedVariable;
        public string UncheckedValue;
        public string PhraseFieldCheckedModification;
        public string PhraseFieldCheckedVariable;
        public string PhraseFieldCheckedValue;
        public string PhraseFieldUncheckedModification;
        public string PhraseFieldUncheckedVariable;
        public string PhraseFieldUncheckedValue;

        public OptionEditViewModel()
        {
        }

        public OptionEditViewModel(Translator translator)
            : base(translator, translator.Get("Option.Edit.Title", "Title of the option edit dialog", "Edit option"), "optionEditDialog")
        {
            PhraseFieldCheckedModification = translator.Get("Option.Edit.Field.CheckedModification", "Checked modification field in the option edit dialog", "Checked modification").EscapeHtml();
            PhraseFieldCheckedVariable = translator.Get("Option.Edit.Field.CheckedVariable", "Checked variable field in the option edit dialog", "Checked variable").EscapeHtml();
            PhraseFieldCheckedValue = translator.Get("Option.Edit.Field.CheckedValue", "Checked value field in the option edit dialog", "Checked value").EscapeHtml();
            PhraseFieldUncheckedModification = translator.Get("Option.Edit.Field.UncheckedModification", "Unchecked modification field in the option edit dialog", "Unchecked modification").EscapeHtml();
            PhraseFieldUncheckedVariable = translator.Get("Option.Edit.Field.UncheckedVariable", "Unchecked variable field in the option edit dialog", "Unchecked variable").EscapeHtml();
            PhraseFieldUncheckedValue = translator.Get("Option.Edit.Field.UncheckedValue", "Unchecked value field in the option edit dialog", "Unchecked value").EscapeHtml();
            CheckedModifications = new List<NamedIntViewModel>();
            UncheckedModifications = new List<NamedIntViewModel>();
        }

        private void AddModifications(Translator translator, List<NamedIntViewModel> list, Func<VariableModification, bool> selected)
        {
            list.Add(new NamedIntViewModel(translator, VariableModification.None, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Add, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.And, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Append, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Divide, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Multiply, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Or, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Remove, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Set, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Subtract, selected));
            list.Add(new NamedIntViewModel(translator, VariableModification.Xor, selected));
        }

        public OptionEditViewModel(Translator translator, IDatabase db, Session session, Question question)
            : this(translator)
        {
            Method = "add";
            Id = question.Id.Value.ToString();
            Text = translator.CreateLanguagesMultiItem("Option.Edit.Field.Text", "Text field in the option edit dialog", "Text ({0})", new MultiLanguageString());
            CheckedValue = "0";
            UncheckedValue = "0";
            AddModifications(translator, CheckedModifications, v => v == VariableModification.None);
            AddModifications(translator, UncheckedModifications, v => v == VariableModification.None);
            CheckedVariables = new List<NamedIdViewModel>(
                question.Section.Value.Questionaire.Value.Variables
                .Select(v => new NamedIdViewModel(translator, v, false))
                .OrderBy(v => v.Name));
            UncheckedVariables = new List<NamedIdViewModel>(
                question.Section.Value.Questionaire.Value.Variables
                .Select(v => new NamedIdViewModel(translator, v, false))
                .OrderBy(v => v.Name));
        }

        public OptionEditViewModel(Translator translator, IDatabase db, Session session, Option option)
            : this(translator)
        {
            Method = "edit";
            Id = option.Id.ToString();
            Text = translator.CreateLanguagesMultiItem("Option.Edit.Field.Text", "Text field in the option edit dialog", "Text ({0})", option.Text.Value);
            AddModifications(translator, CheckedModifications, v => v == option.CheckedModification.Value);
            AddModifications(translator, UncheckedModifications, v => v == option.UncheckedModification.Value);
            CheckedVariables = new List<NamedIdViewModel>(
                option.Question.Value.Section.Value.Questionaire.Value.Variables
                .Select(v => new NamedIdViewModel(translator, v, v == option.CheckedVariable.Value))
                .OrderBy(v => v.Name));
            UncheckedVariables = new List<NamedIdViewModel>(
                option.Question.Value.Section.Value.Questionaire.Value.Variables
                .Select(v => new NamedIdViewModel(translator, v, v == option.UncheckedVariable.Value))
                .OrderBy(v => v.Name));
        }
    }

    public class OptionViewModel : MasterViewModel
    {
        public string Id;

        public OptionViewModel(Translator translator, Session session, Question question)
            : base(translator,
            translator.Get("Option.List.Title", "Title of the option list page", "Options"),
            session)
        {
            Id = question.Id.Value.ToString();
        }
    }

    public class OptionListItemViewModel
    {
        public string Id;
        public string Text;
        public string PhraseDeleteConfirmationOption;
        public string PhraseHeaderOptions;

        public OptionListItemViewModel(Translator translator, Session session, Option option)
        {
            Id = option.Id.Value.ToString();
            Text = option.Text.Value[translator.Language];
            PhraseDeleteConfirmationOption = translator.Get("Option.List.Delete.Confirm.Option", "Delete option confirmation option", "Do you really wish to delete option {0}?", option.GetText(translator));
        }
    }

    public class OptionListViewModel
    {
        public string ParentId;
        public string Id;
        public string Text;
        public string PhraseHeaderQuestion;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<OptionListItemViewModel> List;
        public string Editable;
        public bool AddAccess;

        public OptionListViewModel(Translator translator, IDatabase database, Session session, Question question)
        {
            ParentId = question.Section.Value.Id.Value.ToString();
            Id = question.Id.Value.ToString();
            Text = question.Text.Value[translator.Language];
            PhraseHeaderQuestion = translator.Get("Option.List.Header.Question", "Header part 'Question' in the option list", "Question");
            PhraseDeleteConfirmationTitle = translator.Get("Option.List.Delete.Confirm.Title", "Delete option confirmation title", "Delete?");
            PhraseDeleteConfirmationInfo = string.Empty;
            List = new List<OptionListItemViewModel>(
                question.Options
                .OrderBy(o => o.Ordering.Value)
                .Select(o => new OptionListItemViewModel(translator, session, o)));
            AddAccess = session.HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Write);
            Editable = AddAccess ? "editable" : "accessdenied";
        }
    }

    public class OptionEdit : CensusModule
    {
        public OptionEdit()
        {
            this.RequiresAuthentication();

            Get("/option/{id}", parameters =>
            {
                string idString = parameters.id;
                var question = Database.Query<Question>(idString);

                if (question != null)
                {
                    if (HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/option.sshtml",
                            new OptionViewModel(Translator, CurrentSession, question)];
                    }
                }

                return null;
            });
            Get("/option/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var question = Database.Query<Question>(idString);

                if (question != null)
                {
                    if (HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Read))
                    {
                        return View["View/optionlist.sshtml",
                            new OptionListViewModel(Translator, Database, CurrentSession, question)];
                    }
                }

                return null;
            });
            Get("/option/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var option = Database.Query<Option>(idString);

                if (option != null)
                {
                    if (HasAccess(option.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/optionedit.sshtml",
                        new OptionEditViewModel(Translator, Database, CurrentSession, option)];
                    }
                }

                return null;
            });
            Post("/option/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<OptionEditViewModel>(ReadBody());
                var option = Database.Query<Option>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(option))
                {
                    if (status.HasAccess(option.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", option.Text, model.Text);
                        status.AssignInt32String("CheckedValue", option.CheckedValue, model.CheckedValue);
                        status.AssignInt32String("UncheckedValue", option.UncheckedValue, model.UncheckedValue);

                        if (status.IsSuccess)
                        {
                            Database.Save(option);
                            Notice("{0} changed option {1}", CurrentSession.User.UserName.Value, option);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/option/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var question = Database.Query<Question>(idString);

                if (question != null)
                {
                    if (HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        return View["View/optionedit.sshtml",
                            new OptionEditViewModel(Translator, Database, CurrentSession, question)];
                    }
                }

                return null;
            });
            Post("/option/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var question = Database.Query<Question>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(question))
                {
                    if (status.HasAccess(question.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<OptionEditViewModel>(ReadBody());
                        var option = new Option(Guid.NewGuid());
                        status.AssignMultiLanguageRequired("Name", option.Text, model.Text);
                        status.AssignInt32String("CheckedValue", option.CheckedValue, model.CheckedValue);
                        status.AssignInt32String("UncheckedValue", option.UncheckedValue, model.UncheckedValue);

                        option.Ordering.Value = question.Options.MaxOrDefault(o => o.Ordering.Value, 0) + 1;
                        option.Question.Value = question;

                        if (status.IsSuccess)
                        {
                            Database.Save(option);
                            Notice("{0} added option {1}", CurrentSession.User.UserName.Value, option);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/option/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var option = Database.Query<Option>(idString);

                if (option != null)
                {
                    if (HasAccess(option.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            option.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted option {1}", CurrentSession.User.UserName.Value, option);
                        }
                    }
                }

                return string.Empty;
            });
            Post("/option/switch", parameters =>
            {
                var model = JsonConvert.DeserializeObject<SwitchViewModel>(ReadBody());
                var status = CreateStatus();

                using (var transaction = Database.BeginTransaction())
                {
                    var source = Database.Query<Option>(model.SourceId);
                    var target = Database.Query<Option>(model.TargetId);

                    if (status.ObjectNotNull(source) &&
                        status.ObjectNotNull(target) &&
                        status.HasAccess(source.Owner, PartAccess.Questionaire, AccessRight.Write) &&
                        status.HasAccess(target.Owner, PartAccess.Questionaire, AccessRight.Write))
                    {
                        if (source.Question.Value == target.Question.Value)
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

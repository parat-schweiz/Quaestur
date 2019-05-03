using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class StateEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;

        public StateEditViewModel()
        { 
        }

        public StateEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("State.Edit.Title", "Title of the edit state dialog", "Edit state"), 
                   "stateEditDialog")
        {
        }

        public StateEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("State.Edit.Field.Name", "Field 'Name' in the edit state dialog", "Name ({0})", new MultiLanguageString());
        }

        public StateEditViewModel(Translator translator, IDatabase db, State state)
            : this(translator)
        {
            Method = "edit";
            Id = state.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("State.Edit.Field.Name", "Field 'Name' in the edit state dialog", "Name ({0})", state.Name.Value);
        }
    }

    public class StateViewModel : MasterViewModel
    {
        public StateViewModel(Translator translator, Session session)
            : base(translator, 
                   translator.Get("State.List.Title", "Title of the states list page", "States"), 
                   session)
        { 
        }
    }

    public class StateListItemViewModel
    {
        public string Id;
        public string Name;
        public string PhraseDeleteConfirmationQuestion;

        public StateListItemViewModel(Translator translator, State state)
        {
            Id = state.Id.Value.ToString();
            Name = state.Name.Value[translator.Language].EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("State.List.Delete.Confirm.Question", "Delete state confirmation question", "Do you really wish to delete state {0}?", state.GetText(translator)).EscapeHtml();
        }
    }

    public class StateListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<StateListItemViewModel> List;

        public StateListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderName = translator.Get("State.List.Header.Name", "Column 'Name' in the state list page", "Name").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("State.List.Delete.Confirm.Title", "Delete state confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("State.List.Delete.Confirm.Info", "Delete state confirmation info", "This will remove that state from all postal addresses.").EscapeHtml();
            List = new List<StateListItemViewModel>(
                database.Query<State>()
                .Select(c => new StateListItemViewModel(translator, c))
                .OrderBy(c => c.Name));
        }
    }

    public class StateEdit : PublicusModule
    {
        public StateEdit()
        {
            this.RequiresAuthentication();

            Get["/state"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/state.sshtml",
                        new StateViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            };
            Get["/state/list"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/statelist.sshtml",
                        new StateListViewModel(Translator, Database)];
                }
                return null;
            };
            Get["/state/edit/{id}"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var state = Database.Query<State>(idString);

                    if (state != null)
                    {
                        return View["View/stateedit.sshtml",
                            new StateEditViewModel(Translator, Database, state)];
                    }
                }
                return null;
            };
            Post["/state/edit/{id}"] = parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<StateEditViewModel>(ReadBody());
                    var state = Database.Query<State>(idString);

                    if (status.ObjectNotNull(state))
                    {
                        status.AssignMultiLanguageRequired("Name", state.Name, model.Name);

                        if (status.IsSuccess)
                        {
                            Database.Save(state);
                            Notice("{0} changed state {1}", CurrentSession.User.UserName.Value, state);
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/state/add"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/stateedit.sshtml",
                        new StateEditViewModel(Translator, Database)];
                }
                return null;
            };
            Post["/state/add/new"] = parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<StateEditViewModel>(ReadBody());
                    var state = new State(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", state.Name, model.Name);

                    if (status.IsSuccess)
                    {
                        Database.Save(state);
                        Notice("{0} added state {1}", CurrentSession.User.UserName.Value, state);
                    }
                }

                return status.CreateJsonData();
            };
            Get["/state/delete/{id}"] = parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var state = Database.Query<State>(idString);

                    if (status.ObjectNotNull(state))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            state.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted state {1}", CurrentSession.User.UserName.Value, state);
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

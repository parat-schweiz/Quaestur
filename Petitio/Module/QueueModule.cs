using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Petitio
{
    public class QueueEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Parent;
        public List<NamedIdViewModel> Parents;
        public string PhraseFieldParent;

        public QueueEditViewModel()
        {
        }

        public QueueEditViewModel(Translator translator)
            : base(translator, translator.Get("Queue.Edit.Title", "Title of the queue edit dialog", "Edit queue"), "queueEditDialog")
        {
            PhraseFieldParent = translator.Get("Queue.Edit.Field.Parent", "Parent field in the queue edit dialog", "Parent").EscapeHtml();
        }

        public QueueEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Queue.Edit.Field.Name", "Name field in the queue edit dialog", "Name ({0})", new MultiLanguageString());
            Parent = string.Empty;
            Parents = new List<NamedIdViewModel>(
                db.Query<Queue>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
            Parents.Add(new NamedIdViewModel(
                translator.Get("Queue.Edit.Field.Parent.None", "No value in the field 'Parent' in the queue edit dialog", "<None>"),
                false, true));
        }

        public QueueEditViewModel(Translator translator, IDatabase db, Queue queue)
            : this(translator)
        {
            Method = "edit";
            Id = queue.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Queue.Edit.Field.Name", "Name field in the queue edit dialog", "Name ({0})", queue.Name.Value);
            Parent =
                queue.Parent.Value != null ?
                queue.Parent.Value.Id.Value.ToString() :
                string.Empty;
            Parents = new List<NamedIdViewModel>(
                db.Query<Queue>()
                .Where(o => !queue.Subordinates.Contains(o))
                .Where(o => queue != o)
                .Select(o => new NamedIdViewModel(translator, o, o == queue.Parent.Value))
                .OrderBy(o => o.Name));
            Parents.Add(new NamedIdViewModel(
                translator.Get("Queue.Edit.Field.Parent.None", "No value in the field 'Parent' in the queue edit dialog", "<None>"),
                false, queue.Parent.Value == null));
        }
    }

    public class QueueViewModel : MasterViewModel
    {
        public QueueViewModel(Translator translator, Session session)
            : base(translator,
            translator.Get("Queue.List.Title", "Title of the queue list page", "Queues"),
            session)
        {
        }
    }

    public class QueueListItemViewModel
    {
        public string Id;
        public string Name;
        public string Indent;
        public string Width;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public QueueListItemViewModel(Translator translator, Session session, Queue queue, int indent)
        {
            Id = queue.Id.Value.ToString();
            Name = queue.Name.Value[translator.Language].EscapeHtml();
            Indent = indent.ToString() + "%";
            Width = (70 - indent).ToString() + "%";
            Editable =
                session.HasAccess(queue, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Queue.List.Delete.Confirm.Question", "Delete queue confirmation question", "Do you really wish to delete queue {0}?", queue.GetText(translator)).EscapeHtml();
        }
    }

    public class QueueListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseHeaderGroups;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<QueueListItemViewModel> List;
        public bool AddAccess;

        private void AddRecursive(Translator translator, Session session, Queue queue, int indent)
        {
            AddAccess = session.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write);
            int addIndent = 0;

            if (session.HasAccess(queue, PartAccess.Structure, AccessRight.Read))
            {
                List.Add(new QueueListItemViewModel(translator, session, queue, indent));
                addIndent = 5;
            }

            foreach (var child in queue.Children)
            {
                AddRecursive(translator, session, child, indent + addIndent);
            }
        }

        public QueueListViewModel(Translator translator, Session session, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Queue.List.Header.Name", "Column 'Name' in the queue list", "Name").EscapeHtml();
            PhraseHeaderGroups = translator.Get("Queue.List.Header.Groups", "Column 'Groups' in the queue list", "Groups").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Queue.List.Delete.Confirm.Title", "Delete queue confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Queue.List.Delete.Confirm.Info", "Delete queue confirmation info", "This will also delete all subscriptions, groups, roles, permissions and mailings under that queue.").EscapeHtml();
            List = new List<QueueListItemViewModel>();
            var queues = database.Query<Queue>();

            foreach (var queue in queues
                .Where(o => o.Parent.Value == null)
                .OrderBy(o => o.Name.Value[translator.Language]))
            {
                AddRecursive(translator, session, queue, 0);
            }
        }
    }

    public class QueueEdit : PetitioModule
    {
        public QueueEdit()
        {
            this.RequiresAuthentication();

            Get("/queue", parameters =>
            {
                return View["View/queue.sshtml",
                    new QueueViewModel(Translator, CurrentSession)];
            });
            Get("/queue/list", parameters =>
            {
                return View["View/queuelist.sshtml",
                    new QueueListViewModel(Translator, CurrentSession, Database)];
            });
            Get("/queue/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var queue = Database.Query<Queue>(idString);

                if (queue != null)
                {
                    if (HasAccess(queue, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/queueedit.sshtml",
                            new QueueEditViewModel(Translator, Database, queue)];
                    }
                }

                return null;
            });
            Post("/queue/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<QueueEditViewModel>(ReadBody());
                var queue = Database.Query<Queue>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(queue))
                {
                    if (status.HasAccess(queue, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", queue.Name, model.Name);
                        status.AssignObjectIdString("Parent", queue.Parent, model.Parent);

                        if (status.IsSuccess)
                        {
                            Database.Save(queue);
                            Notice("{0} changed queue {1}", CurrentSession.User.UserName.Value, queue);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/queue/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    return View["View/queueedit.sshtml",
                        new QueueEditViewModel(Translator, Database)];
                }
                return null;
            });
            Post("/queue/add/new", parameters =>
            {
                var model = JsonConvert.DeserializeObject<QueueEditViewModel>(ReadBody());
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var queue = new Queue(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", queue.Name, model.Name);
                    status.AssignObjectIdString("Parent", queue.Parent, model.Parent);

                    if (status.IsSuccess)
                    {
                        Database.Save(queue);
                        Notice("{0} added queue {1}", CurrentSession.User.UserName.Value, queue);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/queue/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var queue = Database.Query<Queue>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(queue))
                {
                    if (status.HasAccess(queue, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            queue.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted queue {1}", CurrentSession.User.UserName.Value, queue);
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

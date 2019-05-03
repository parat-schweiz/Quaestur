using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class FeedEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Parent;
        public List<NamedIdViewModel> Parents;
        public string PhraseFieldParent;

        public FeedEditViewModel()
        { 
        }

        public FeedEditViewModel(Translator translator)
            : base(translator, translator.Get("Feed.Edit.Title", "Title of the feed edit dialog", "Edit feed"), "feedEditDialog")
        {
            PhraseFieldParent = translator.Get("Feed.Edit.Field.Parent", "Parent field in the feed edit dialog", "Parent").EscapeHtml();
        }

        public FeedEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Feed.Edit.Field.Name", "Name field in the feed edit dialog", "Name ({0})", new MultiLanguageString());
            Parent = string.Empty;
            Parents = new List<NamedIdViewModel>(
                db.Query<Feed>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
            Parents.Add(new NamedIdViewModel(
                translator.Get("Feed.Edit.Field.Parent.None", "No value in the field 'Parent' in the feed edit dialog", "<None>"),
                false, true));
        }

        public FeedEditViewModel(Translator translator, IDatabase db, Feed feed)
            : this(translator)
        {
            Method = "edit";
            Id = feed.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Feed.Edit.Field.Name", "Name field in the feed edit dialog", "Name ({0})", feed.Name.Value);
            Parent =
                feed.Parent.Value != null ?
                feed.Parent.Value.Id.Value.ToString() :
                string.Empty;
            Parents = new List<NamedIdViewModel>(
                db.Query<Feed>()
                .Where(o => !feed.Subordinates.Contains(o))
                .Where(o => feed != o)
                .Select(o => new NamedIdViewModel(translator, o, o == feed.Parent.Value))
                .OrderBy(o => o.Name));
            Parents.Add(new NamedIdViewModel(
                translator.Get("Feed.Edit.Field.Parent.None", "No value in the field 'Parent' in the feed edit dialog", "<None>"),
                false, feed.Parent.Value == null));
        }
    }

    public class FeedViewModel : MasterViewModel
    {
        public FeedViewModel(Translator translator, Session session)
            : base(translator, 
            translator.Get("Feed.List.Title", "Title of the feed list page", "Feeds"), 
            session)
        { 
        }
    }

    public class FeedListItemViewModel
    {
        public string Id;
        public string Name;
        public string Indent;
        public string Width;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public FeedListItemViewModel(Translator translator, Session session, Feed feed, int indent)
        {
            Id = feed.Id.Value.ToString();
            Name = feed.Name.Value[translator.Language].EscapeHtml();
            Indent = indent.ToString() + "%";
            Width = (70 - indent).ToString() + "%";
            Editable =
                session.HasAccess(feed, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Feed.List.Delete.Confirm.Question", "Delete feed confirmation question", "Do you really wish to delete feed {0}?", feed.GetText(translator)).EscapeHtml();
        }
    }

    public class FeedListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseHeaderGroups;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<FeedListItemViewModel> List;
        public bool AddAccess;

        private void AddRecursive(Translator translator, Session session, Feed feed, int indent)
        {
            AddAccess = session.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write);
            int addIndent = 0;

            if (session.HasAccess(feed, PartAccess.Structure, AccessRight.Read))
            {
                List.Add(new FeedListItemViewModel(translator, session, feed, indent));
                addIndent = 5;
            }

            foreach (var child in feed.Children)
            {
                AddRecursive(translator, session, child, indent + addIndent);
            }
        }

        public FeedListViewModel(Translator translator, Session session, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Feed.List.Header.Name", "Column 'Name' in the feed list", "Name").EscapeHtml();
            PhraseHeaderGroups = translator.Get("Feed.List.Header.Groups", "Column 'Groups' in the feed list", "Groups").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Feed.List.Delete.Confirm.Title", "Delete feed confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Feed.List.Delete.Confirm.Info", "Delete feed confirmation info", "This will also delete all subscriptions, groups, roles, permissions and mailings under that feed.").EscapeHtml();
            List = new List<FeedListItemViewModel>();
            var feeds = database.Query<Feed>();

            foreach (var feed in feeds
                .Where(o => o.Parent.Value == null)
                .OrderBy(o => o.Name.Value[translator.Language]))
            {
                AddRecursive(translator, session, feed, 0);
            }
        }
    }

    public class FeedEdit : PublicusModule
    {
        public FeedEdit()
        {
            this.RequiresAuthentication();

            Get["/feed"] = parameters =>
            {
                return View["View/feed.sshtml",
                    new FeedViewModel(Translator, CurrentSession)];
            };
            Get["/feed/list"] = parameters =>
            {
                return View["View/feedlist.sshtml",
                    new FeedListViewModel(Translator, CurrentSession, Database)];
            };
            Get["/feed/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var feed = Database.Query<Feed>(idString);

                if (feed != null)
                {
                    if (HasAccess(feed, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/feededit.sshtml",
                            new FeedEditViewModel(Translator, Database, feed)];
                    }
                }

                return null;
            };
            Post["/feed/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<FeedEditViewModel>(ReadBody());
                var feed = Database.Query<Feed>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(feed))
                {
                    if (status.HasAccess(feed, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", feed.Name, model.Name);
                        status.AssignObjectIdString("Parent", feed.Parent, model.Parent);

                        if (status.IsSuccess)
                        {
                            Database.Save(feed);
                            Notice("{0} changed feed {1}", CurrentSession.User.UserName.Value, feed);
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/feed/add"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    return View["View/feededit.sshtml",
                        new FeedEditViewModel(Translator, Database)];
                }
                return null;
            };
            Post["/feed/add/new"] = parameters =>
            {
                var model = JsonConvert.DeserializeObject<FeedEditViewModel>(ReadBody());
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var feed = new Feed(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", feed.Name, model.Name);
                    status.AssignObjectIdString("Parent", feed.Parent, model.Parent);

                    if (status.IsSuccess)
                    {
                        Database.Save(feed);
                        Notice("{0} added feed {1}", CurrentSession.User.UserName.Value, feed);
                    }
                }

                return status.CreateJsonData();
            };
            Get["/feed/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var feed = Database.Query<Feed>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(feed))
                {
                    if (status.HasAccess(feed, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            feed.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted feed {1}", CurrentSession.User.UserName.Value, feed);
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class TagEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string[] Usage;
        public string[] Mode;
        public List<NamedIntViewModel> Usages;
        public List<NamedIntViewModel> Modes;
        public string PhraseFieldUsage;
        public string PhraseFieldMode;

        public TagEditViewModel()
        { 
        }

        public TagEditViewModel(Translator translator)
            : base(translator, translator.Get("Tag.Edit.Title", "Title of the tag edit dialog", "Edit tag"), "tagEditDialog")
        {
            PhraseFieldUsage = translator.Get("Tag.Edit.Field.Usage", "Usage field in the tag edit dialog", "Usage").EscapeHtml();
            PhraseFieldMode = translator.Get("Tag.Edit.Field.Mode", "Mode field in the tag edit dialog", "Mode").EscapeHtml();
        }

        public TagEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Tag.Edit.Field.Name", "Name field in the tag edit dialog", "Name ({0})", new MultiLanguageString());
            Usages = new List<NamedIntViewModel>();
            Usages.Add(new NamedIntViewModel(translator, TagUsage.Mailing, false));
            Modes = new List<NamedIntViewModel>();
            Modes.Add(new NamedIntViewModel(translator, TagMode.Default, false));
            Modes.Add(new NamedIntViewModel(translator, TagMode.Manual, false));
            Modes.Add(new NamedIntViewModel(translator, TagMode.Self, false));
        }

        public TagEditViewModel(Translator translator, IDatabase db, Tag tag)
            : this(translator)
        {
            Method = "edit";
            Id = tag.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Tag.Edit.Field.Name", "Name field in the tag edit dialog", "Name ({0})", tag.Name.Value);
            Usages = new List<NamedIntViewModel>();
            Usages.Add(new NamedIntViewModel(translator, TagUsage.Mailing, tag.Usage.Value.HasFlag(TagUsage.Mailing)));
            Modes = new List<NamedIntViewModel>();
            Modes.Add(new NamedIntViewModel(translator, TagMode.Default, tag.Mode.Value.HasFlag(TagMode.Default)));
            Modes.Add(new NamedIntViewModel(translator, TagMode.Manual, tag.Mode.Value.HasFlag(TagMode.Manual)));
            Modes.Add(new NamedIntViewModel(translator, TagMode.Self, tag.Mode.Value.HasFlag(TagMode.Self)));
        }
    }

    public class TagViewModel : MasterViewModel
    {
        public TagViewModel(Translator translator, Session session)
            : base(translator, translator.Get("Tag.List.Title", "Title of the tag list page", "Tags"), 
            session)
        { 
        }
    }

    public class TagListItemViewModel
    {
        public string Id;
        public string Name;
        public string Usage;
        public string Mode;
        public string PhraseDeleteConfirmationQuestion;

        private static string GetText(Translator translator, TagUsage usage)
        {
            var list = new List<string>();

            if (usage.HasFlag(TagUsage.Mailing))
            {
                list.Add(TagUsage.Mailing.Translate(translator));
            }

            return string.Join(", ", list);
        }

        private static string GetText(Translator translator, TagMode mode)
        {
            var list = new List<string>();

            if (mode.HasFlag(TagMode.Default))
            {
                list.Add(TagMode.Default.Translate(translator));
            }

            if (mode.HasFlag(TagMode.Manual))
            {
                list.Add(TagMode.Manual.Translate(translator));
            }

            if (mode.HasFlag(TagMode.Self))
            {
                list.Add(TagMode.Self.Translate(translator));
            }

            return string.Join(", ", list);
        }

        public TagListItemViewModel(Translator translator, Tag tag)
        {
            Id = tag.Id.Value.ToString();
            Name = tag.Name.Value[translator.Language].EscapeHtml();
            Usage = GetText(translator, tag.Usage.Value).EscapeHtml();
            Mode = GetText(translator, tag.Mode.Value).EscapeHtml();
            PhraseDeleteConfirmationQuestion = translator.Get("Tag.List.Delete.Confirm.Question", "Delete tag confirmation question", "Do you really wish to delete tag {0}?", tag.GetText(translator)).EscapeHtml();
        }
    }

    public class TagListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseHeaderUsage;
        public string PhraseHeaderMode;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<TagListItemViewModel> List;

        public TagListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Tag.List.Header.Name", "Column 'Name' in the tag list", "Name").EscapeHtml();
            PhraseHeaderUsage = translator.Get("Tag.List.Header.Usage", "Column 'Usage' in the tag list", "Usage").EscapeHtml();
            PhraseHeaderMode = translator.Get("Tag.List.Header.Mode", "Column 'Mode' in the tag list", "Mode").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Tag.List.Delete.Confirm.Title", "Delete tag confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Tag.List.Delete.Confirm.Info", "Delete tag confirmation info", "This will remove that tag from all contacts.").EscapeHtml();
            List = new List<TagListItemViewModel>(
                database.Query<Tag>()
                .Select(t => new TagListItemViewModel(translator, t))
                .OrderBy(t => t.Name));
        }
    }

    public class TagEdit : PublicusModule
    {
        public TagEdit()
        {
            this.RequiresAuthentication();

            Get["/tag"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/tag.sshtml",
                        new TagViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            };
            Get["/tag/list"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/taglist.sshtml",
                    new TagListViewModel(Translator, Database)];
                }
                return null;
            };
            Get["/tag/edit/{id}"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var tag = Database.Query<Tag>(idString);

                    if (tag != null)
                    {
                        return View["View/tagedit.sshtml",
                            new TagEditViewModel(Translator, Database, tag)];
                    }
                }
                return null;
            };
            Post["/tag/edit/{id}"] = parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<TagEditViewModel>(ReadBody());
                    var tag = Database.Query<Tag>(idString);

                    if (status.ObjectNotNull(tag))
                    {
                        status.AssignMultiLanguageRequired("Name", tag.Name, model.Name);
                        status.AssignFlagIntsString("Usage", tag.Usage, model.Usage);
                        status.AssignFlagIntsString("Mode", tag.Mode, model.Mode);

                        if (status.IsSuccess)
                        {
                            Database.Save(tag);
                            Notice("{0} changed tag {1}", CurrentSession.User.UserName.Value, tag);
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/tag/add"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/tagedit.sshtml",
                        new TagEditViewModel(Translator, Database)];
                }
                return null;
            };
            Post["/tag/add/new"] = parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<TagEditViewModel>(ReadBody());
                    var tag = new Tag(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", tag.Name, model.Name);
                    status.AssignFlagIntsString("Usage", tag.Usage, model.Usage);
                    status.AssignFlagIntsString("Mode", tag.Mode, model.Mode);

                    if (status.IsSuccess)
                    {
                        Database.Save(tag);
                        Notice("{0} added tag {1}", CurrentSession.User.UserName.Value, tag);
                    }
                }

                return status.CreateJsonData();
            };
            Get["/tag/delete/{id}"] = parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var tag = Database.Query<Tag>(idString);

                    if (status.ObjectNotNull(tag))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            tag.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted tag {1}", CurrentSession.User.UserName.Value, tag);
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

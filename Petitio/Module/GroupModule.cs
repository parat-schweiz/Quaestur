﻿using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Petitio
{
    public class GroupEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public List<MultiItemViewModel> MailName;
        public List<MultiItemViewModel> MailAddress;
        public string GpgKeyId;
        public string GpgKeyPassphrase;
        public string PhraseFieldGpgKeyId;
        public string PhraseFieldGpgKeyPassphrase;
        public bool CryptoAccess;

        public GroupEditViewModel()
        {
        }

        public GroupEditViewModel(Translator translator)
            : base(translator, translator.Get("Group.Edit.Title", "Title of the group edit dialog", "Edit group"), "groupEditDialog")
        {
            PhraseFieldGpgKeyId = translator.Get("Group.Edit.Field.GpgKeyId", "GpgKeyId field in the group edit dialog", "Public Key ID").EscapeHtml();
            PhraseFieldGpgKeyPassphrase = translator.Get("Group.Edit.Field.GpgKeyPassphrase", "GpgKeyPassphrase field in the group edit dialog", "Private Key Passphrase").EscapeHtml();
        }

        public GroupEditViewModel(Translator translator, IDatabase db, Session session, Queue queue)
            : this(translator)
        {
            Method = "add";
            Id = queue.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("Group.Edit.Field.Name", "Name field in the group edit dialog", "Name ({0})", new MultiLanguageString());
            MailName = translator.CreateLanguagesMultiItem("Group.Edit.Field.MailName", "MailName field in the group edit dialog", "Name in E-Mail ({0})", new MultiLanguageString());
            MailAddress = translator.CreateLanguagesMultiItem("Group.Edit.Field.MailAddress", "MailAddress field in the group edit dialog", "E-Mail address ({0})", new MultiLanguageString());
            CryptoAccess = session.HasAccess(queue, PartAccess.Crypto, AccessRight.Write);
            GpgKeyId = string.Empty;
            GpgKeyPassphrase = string.Empty;
        }

        public GroupEditViewModel(Translator translator, IDatabase db, Session session, Group group)
            : this(translator)
        {
            Method = "edit";
            Id = group.Id.ToString();
            Name = translator.CreateLanguagesMultiItem("Group.Edit.Field.Name", "Name field in the group edit dialog", "Name ({0})", group.Name.Value);
            MailName = translator.CreateLanguagesMultiItem("Group.Edit.Field.MailName", "MailName field in the group edit dialog", "Name in E-Mail ({0})", group.MailName.Value);
            MailAddress = translator.CreateLanguagesMultiItem("Group.Edit.Field.MailAddress", "MailAddress field in the group edit dialog", "E-Mail address ({0})", group.MailAddress.Value);
            CryptoAccess = session.HasAccess(group, PartAccess.Crypto, AccessRight.Write);
            if (CryptoAccess)
            {
                GpgKeyId = group.GpgKeyId.Value;
                GpgKeyPassphrase = PostStatus.UnchangedGpgPassphraseValue;
            }
            else
            {
                GpgKeyId = string.Empty;
                GpgKeyPassphrase = string.Empty;
            }
        }
    }

    public class GroupViewModel : MasterViewModel
    {
        public string Id;

        public GroupViewModel(Translator translator, Session session, Queue queue)
            : base(translator,
            translator.Get("Group.List.Title", "Title of the group list page", "Groups"),
            session)
        {
            Id = queue.Id.Value.ToString();
        }
    }

    public class GroupListItemViewModel
    {
        public string Id;
        public string Name;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public GroupListItemViewModel(Translator translator, Session session, Group group)
        {
            Id = group.Id.Value.ToString();
            Name = group.Name.Value[translator.Language];
            Editable =
                session.HasAccess(group.Queue.Value, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Group.List.Delete.Confirm.Question", "Delete group confirmation question", "Do you really wish to delete group {0}?", group.GetText(translator));
        }
    }

    public class GroupListViewModel
    {
        public string Id;
        public string Name;
        public string PhraseHeaderQueue;
        public string PhraseHeaderRoles;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<GroupListItemViewModel> List;
        public bool AddAccess;

        public GroupListViewModel(Translator translator, IDatabase database, Session session, Queue queue)
        {
            Id = queue.Id.Value.ToString();
            Name = queue.Name.Value[translator.Language];
            PhraseHeaderQueue = translator.Get("Group.List.Header.Queue", "Header part 'Queue' in the group list", "Queue");
            PhraseHeaderRoles = translator.Get("Group.List.Header.Roles", "Link 'Roles' caption in the group list", "Roles");
            PhraseDeleteConfirmationTitle = translator.Get("Group.List.Delete.Confirm.Title", "Delete group confirmation title", "Delete?");
            PhraseDeleteConfirmationInfo = translator.Get("Group.List.Delete.Confirm.Info", "Delete group confirmation info", "This will also delete all roles and permissions under that group.");
            List = new List<GroupListItemViewModel>(
                queue.Groups
                .Where(g => session.HasAccess(g, PartAccess.Structure, AccessRight.Read))
                .Select(g => new GroupListItemViewModel(translator, session, g))
                .OrderBy(g => g.Name));
            AddAccess = session.HasAccess(queue, PartAccess.Structure, AccessRight.Write);
        }
    }

    public class GroupEdit : PetitioModule
    {
        public GroupEdit()
        {
            this.RequiresAuthentication();

            Get("/group/{id}", parameters =>
            {
                string idString = parameters.id;
                var queue = Database.Query<Queue>(idString);

                if (queue != null)
                {
                    if (HasAccess(queue, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/group.sshtml",
                            new GroupViewModel(Translator, CurrentSession, queue)];
                    }
                }

                return null;
            });
            Get("/group/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var queue = Database.Query<Queue>(idString);

                if (queue != null)
                {
                    if (HasAccess(queue, PartAccess.Structure, AccessRight.Read))
                    {
                        return View["View/grouplist.sshtml",
                            new GroupListViewModel(Translator, Database, CurrentSession, queue)];
                    }
                }

                return null;
            });
            Get("/group/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var group = Database.Query<Group>(idString);

                if (group != null)
                {
                    if (HasAccess(group, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/groupedit.sshtml",
                        new GroupEditViewModel(Translator, Database, CurrentSession, group)];
                    }
                }

                return null;
            });
            Post("/group/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<GroupEditViewModel>(ReadBody());
                var group = Database.Query<Group>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(group))
                {
                    if (status.HasAccess(group, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", group.Name, model.Name);
                        status.AssignMultiLanguageFree("MailName", group.MailName, model.MailName);
                        status.AssignMultiLanguageFree("MailAddress", group.MailAddress, model.MailAddress);

                        if (HasAccess(group, PartAccess.Crypto, AccessRight.Write))
                        {
                            status.AssignStringFree("GpgKeyId", group.GpgKeyId, model.GpgKeyId);
                            status.AssignGpgPassphrase("GpgKeyPassphrase", group.GpgKeyPassphrase, model.GpgKeyPassphrase);
                        }

                        if (status.IsSuccess)
                        {
                            Database.Save(group);
                            Notice("{0} changed group {1}", CurrentSession.User.UserName.Value, group);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/group/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var queue = Database.Query<Queue>(idString);

                if (queue != null)
                {
                    if (HasAccess(queue, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/groupedit.sshtml",
                            new GroupEditViewModel(Translator, Database, CurrentSession, queue)];
                    }
                }

                return null;
            });
            Post("/group/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var queue = Database.Query<Queue>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(queue))
                {
                    if (status.HasAccess(queue, PartAccess.Structure, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<GroupEditViewModel>(ReadBody());
                        var group = new Group(Guid.NewGuid());
                        status.AssignMultiLanguageRequired("Name", group.Name, model.Name);
                        status.AssignMultiLanguageFree("MailName", group.MailName, model.MailName);
                        status.AssignMultiLanguageFree("MailAddress", group.MailAddress, model.MailAddress);

                        if (HasAccess(group, PartAccess.Crypto, AccessRight.Write))
                        {
                            status.AssignStringFree("GpgKeyId", group.GpgKeyId, model.GpgKeyId);
                            status.AssignGpgPassphrase("GpgKeyPassphrase", group.GpgKeyPassphrase, model.GpgKeyPassphrase);
                        }

                        group.Queue.Value = queue;

                        if (status.IsSuccess)
                        {
                            Database.Save(group);
                            Notice("{0} added group {1}", CurrentSession.User.UserName.Value, group);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/group/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var group = Database.Query<Group>(idString);

                if (group != null)
                {
                    if (HasAccess(group, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            group.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted group {1}", CurrentSession.User.UserName.Value, group);
                        }
                    }
                }

                return null;
            });
        }
    }
}

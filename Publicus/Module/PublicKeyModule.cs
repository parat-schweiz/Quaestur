using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using SiteLibrary;

namespace Publicus
{
    public class GpgUidItem
    {
        public string Trust;
        public string MailAddress;

        public GpgUidItem(string trust, string mailAddress)
        {
            Trust = trust.EscapeHtml();
            MailAddress = mailAddress.EscapeHtml();
        }
    }

    public class PublicKeyEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Type;
        public string KeyId;
        public string FileName;
        public string FilePath;
        public string FileSize;
        public string FileData;
        public List<NamedIntViewModel> Types;
        public string PhraseFieldType;
        public string PhraseFieldKeyId;
        public string PhraseFieldUid;
        public string PhraseFieldKeyFile;
        public List<GpgUidItem> Uids;

        public PublicKeyEditViewModel()
        { 
        }

        public PublicKeyEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("PublicKey.Edit.Title", "Title of the edit publicKey dialog", "Edit public key"), 
                   "publicKeyEditDialog")
        {
            PhraseFieldType = translator.Get("PublicKey.Edit.Field.Type", "Field 'Type' in the edit public key dialog", "Type").EscapeHtml();
            PhraseFieldKeyId = translator.Get("PublicKey.Edit.Field.KeyId", "Field 'KeyId' in the edit public key dialog", "Key ID").EscapeHtml();
            PhraseFieldUid = translator.Get("PublicKey.Edit.Field.Uid", "Field 'User ID' in the edit public key dialog", "User ID").EscapeHtml();
            PhraseFieldKeyFile = translator.Get("PublicKey.Edit.Field.KeyFile", "Field 'KeyFile' in the edit public key dialog", "Key file").EscapeHtml();
            Type = string.Empty;
            FileData = string.Empty;
        }

        public PublicKeyEditViewModel(Translator translator, IDatabase db, Session session, Contact contact)
            : this(translator)
        {
            Method = "add";
            Id = contact.Id.ToString();
            KeyId = string.Empty;
            FileName = string.Empty;
            FilePath = string.Empty;
            FileSize = string.Empty;
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, PublicKeyType.OpenPGP, false));
            Uids = new List<GpgUidItem>();
        }

        public PublicKeyEditViewModel(Translator translator, IDatabase db, Session session, PublicKey publicKey)
            : this(translator)
        {
            Method = "edit";
            Id = publicKey.Id.ToString();
            KeyId = publicKey.KeyId.Value.EscapeHtml();
            FileName = publicKey.ShortKeyId.EscapeHtml() + ".asc";
            FilePath = "/publickey/download/" + publicKey.Id.ToString();
            FileSize = publicKey.Data.Value.Length.SizeFormat();
            Types = new List<NamedIntViewModel>();
            Types.Add(new NamedIntViewModel(translator, PublicKeyType.OpenPGP, publicKey.Type.Value == PublicKeyType.OpenPGP));

            Uids = new List<GpgUidItem>(Global.Gpg
                .ImportKeys(publicKey.Data.Value)
                .SelectMany(k => k.Uids)
                .Select(u => new GpgUidItem(u.Trust.ToString(), u.Mail))
                .OrderBy(u => u.MailAddress));
        }
    }

    public class PublicKeyModule : PublicusModule
    {
        public PublicKeyModule()
        {
            this.RequiresAuthentication();

            Get("/publickey/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var publicKey = Database.Query<PublicKey>(idString);

                if (publicKey != null)
                {
                    if (HasAccess(publicKey.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        return View["View/publicKeyedit.sshtml",
                            new PublicKeyEditViewModel(Translator, Database, CurrentSession, publicKey)];
                    }
                }

                return null;
            });
            Post("/publickey/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PublicKeyEditViewModel>(ReadBody());
                var publicKey = Database.Query<PublicKey>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(publicKey))
                {
                    if (status.HasAccess(publicKey.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        status.AssignEnumIntString("Type", publicKey.Type, model.Type);
                        AssignKeyFileData(model, status, publicKey, false);

                        if (status.IsSuccess)
                        {
                            Database.Save(publicKey);
                            Journal(publicKey.Contact.Value,
                                "PublicKey.Journal.Edit",
                                "Journal entry edited publicKey",
                                "Changed publicKey {0}",
                                t => publicKey.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/publickey/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Write))
                    {
                        return View["View/publicKeyedit.sshtml",
                            new PublicKeyEditViewModel(Translator, Database, CurrentSession, contact)];
                    }
                }

                return null;
            });
            Post("/publickey/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PublicKeyEditViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    if (status.HasAccess(contact, PartAccess.Contact, AccessRight.Write))
                    {
                        var publicKey = new PublicKey(Guid.NewGuid());
                        status.AssignEnumIntString("Type", publicKey.Type, model.Type);
                        AssignKeyFileData(model, status, publicKey, true);
                        publicKey.Contact.Value = contact;

                        if (status.IsSuccess)
                        {
                            Database.Save(publicKey);
                            Journal(publicKey.Contact,
                                "PublicKey.Journal.Add",
                                "Journal entry added public key",
                                "Added public key {0}",
                                t => publicKey.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/publickey/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var publicKey = Database.Query<PublicKey>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(publicKey))
                {
                    if (status.HasAccess(publicKey.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        Database.Delete(publicKey);
                        Journal(publicKey.Contact,
                            "PublicKey.Journal.Delete",
                            "Journal entry deleted public key",
                            "Deleted public key {0}",
                            t => publicKey.GetText(t));
                    }
                }

                return status.CreateJsonData();
            });
            Get("/publickey/download/{id}", parameters =>
            {
                string idString = parameters.id;
                var publicKey = Database.Query<PublicKey>(idString);

                if (publicKey != null)
                {
                    if (HasAccess(publicKey.Contact.Value, PartAccess.Contact, AccessRight.Read))
                    {
                        var stream = new MemoryStream(publicKey.Data);
                        var response = new StreamResponse(() => stream, "application/openpgp-publickey");
                        Journal(publicKey.Contact,
                            "PublicKey.Journal.Download",
                            "Journal entry downloaded publicKey",
                            "Downloaded publicKey {0}",
                            t => publicKey.GetText(t));
                        return response.AsAttachment(publicKey.ShortKeyId + ".asc");
                    }
                }

                return null;
            });
        }

        private void AssignKeyFileData(PublicKeyEditViewModel model, PostStatus status, PublicKey publicKey, bool noDataSetError)
        {
            if (!string.IsNullOrEmpty(model.FileData))
            {
                var keyFileData = GetDataUrlString(model.FileData);

                if (keyFileData != null)
                {
                    var keys = Global.Gpg.ImportKeys(keyFileData);

                    if (keys.Count() == 1)
                    {
                        var key = keys.Single();
                        publicKey.Data.Value = Global.Gpg.ExportKeyBinary(key.Id);
                        publicKey.KeyId.Value = key.Id;
                    }
                    else
                    {
                        status.SetValidationError("File", "PublicKey.Validation.GpgKey.Invalid", "Validation message when gpg key file is not valid", "Key file not vaild");
                    }
                }
                else
                {
                    status.SetValidationError("File", "PublicKey.Validation.GpgKey.Invalid", "Validation message when gpg key file is not valid", "Key file not vaild");
                }
            }
            else if (noDataSetError)
            {
                status.SetValidationError("File", "PublicKey.Validation.GpgKey.Missing", "Validation message when gpg key file is required but not uploaded", "Key file must be uploaded");
            }
        }
    }
}

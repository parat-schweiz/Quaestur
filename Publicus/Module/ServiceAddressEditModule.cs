using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ServiceAddressEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Address;
        public string Service;
        public string Category;
        public List<NamedIntViewModel> Categories;
        public string PhraseFieldAddress;
        public string PhraseFieldCategory;

        public ServiceAddressEditViewModel()
        { 
        }

        private static string Translate(Translator translator, ServiceType service)
        {
            switch (service)
            {
                case ServiceType.EMail:
                    return translator.Get("ServiceAddress,Edit.Title.EMail", "Title of the edit e-mail address dialog", "Edit E-Mail address").EscapeHtml();
                case ServiceType.Phone:
                    return translator.Get("ServiceAddress,Edit.Title.EMail", "Title of the edit phone number dialog", "Edit phone number").EscapeHtml();
                default:
                    throw new NotSupportedException();
            }
        }

        private static string GetDialogId(ServiceType service)
        {
            switch (service)
            {
                case ServiceType.EMail:
                    return "serviceAddressMailEditDialog";
                case ServiceType.Phone:
                    return "serviceAddressPhoneEditDialog";
                default:
                    throw new NotSupportedException();
            }
        }

        public ServiceAddressEditViewModel(Translator translator, ServiceType service)
            : base(translator, Translate(translator, service), GetDialogId(service))
        {
            switch (service)
            {
                case ServiceType.EMail:
                    PhraseFieldAddress = 
                        translator.Get("ServiceAddress.Edit.Field.Address.EMail", "Field 'E-Mail' in the edit e-mail address dialog", "E-Mail address").EscapeHtml();
                    break;
                case ServiceType.Phone:
                    PhraseFieldAddress = 
                        translator.Get("ServiceAddress.Edit.Field.Address.Phone", "Field 'Phone' in the edit e-mail address dialog", "Phone number").EscapeHtml();
                    break;
                default:
                    throw new NotSupportedException(); 
            }
            PhraseFieldCategory = translator.Get("ServiceAddress.Edit.Field.Category", "Field 'Category' in the edit e-mail address dialog", "Category").EscapeHtml();
            Categories = new List<NamedIntViewModel>();
        }

        public ServiceAddressEditViewModel(Translator translator, IDatabase db, Contact contact, ServiceType service)
            : this(translator, service)
        {
            Method = "add";
            Id = contact.Id.ToString();
            Address = string.Empty;
            Service = ((int)service).ToString();
            Category = string.Empty;
            Categories.Add(new NamedIntViewModel(translator, AddressCategory.Home, false));
            Categories.Add(new NamedIntViewModel(translator, AddressCategory.Mobile, false));
            Categories.Add(new NamedIntViewModel(translator, AddressCategory.Work, false));
        }

        public ServiceAddressEditViewModel(Translator translator, IDatabase db, ServiceAddress address)
            : this(translator, address.Service.Value)
        {
            Method = "edit";
            Id = address.Id.ToString();
            Address = address.Address.Value.EscapeHtml();
            Service = ((int)address.Service.Value).ToString();
            Category = ((int)address.Category.Value).ToString();
            Categories.Add(new NamedIntViewModel(translator, AddressCategory.Home, address.Category.Value == AddressCategory.Home));
            Categories.Add(new NamedIntViewModel(translator, AddressCategory.Mobile, address.Category.Value == AddressCategory.Mobile));
            Categories.Add(new NamedIntViewModel(translator, AddressCategory.Work, address.Category.Value == AddressCategory.Work));
        }
    }

    public class ServiceAddressEdit : PublicusModule
    {
        public ServiceAddressEdit()
        {
            this.RequiresAuthentication();

            Get["/serviceaddress/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var address = Database.Query<ServiceAddress>(idString);

                if (address != null)
                {
                    if (HasAccess(address.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        return View["View/serviceaddressedit.sshtml",
                            new ServiceAddressEditViewModel(Translator, Database, address)];
                    }
                }

                return null;
            };
            Post["/serviceaddress/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<ServiceAddressEditViewModel>(ReadBody());
                var address = Database.Query<ServiceAddress>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(address))
                {
                    if (status.HasAccess(address.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        status.AssignStringRequired("Address", address.Address, model.Address);
                        status.AssignEnumIntString("Category", address.Category, model.Category);

                        if (status.IsSuccess)
                        {
                            Database.Save(address);
                            switch (address.Service.Value)
                            {
                                case ServiceType.EMail:
                                    Journal(address.Contact,
                                        "ServiceAddress.EMail.Journal.Edit",
                                        "Journal entry changed mail address",
                                        "Changed mail address {0}",
                                        t => address.GetText(t));
                                    break;
                                case ServiceType.Phone:
                                    Journal(address.Contact,
                                        "ServiceAddress.Phone.Journal.Edit",
                                        "Journal entry changed phone number",
                                        "Changed phone number {0}",
                                        t => address.GetText(t));
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/serviceaddress/add/phone/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Write))
                    {
                        return View["View/serviceaddressedit.sshtml",
                            new ServiceAddressEditViewModel(Translator, Database, contact, ServiceType.Phone)];
                    }
                }

                return null;
            };
            Get["/serviceaddress/add/mail/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Write))
                    {
                        return View["View/serviceaddressedit.sshtml",
                            new ServiceAddressEditViewModel(Translator, Database, contact, ServiceType.EMail)];
                    }
                }

                return null;
            };
            Post["/serviceaddress/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<ServiceAddressEditViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    var address = new ServiceAddress(Guid.NewGuid());
                    status.AssignStringRequired("Address", address.Address, model.Address);
                    status.AssignEnumIntString("Category", address.Category, model.Category);
                    status.AssignEnumIntString("Service", address.Service, model.Service);
                    address.Precedence.Value = contact.PostalAddresses.MaxOrDefault(a => a.Precedence.Value, 0) + 1;
                    address.Contact.Value = contact;

                    if (status.IsSuccess)
                    {
                        Database.Save(address);
                        switch (address.Service.Value)
                        {
                            case ServiceType.EMail:
                                Journal(address.Contact,
                                    "ServiceAddress.EMail.Journal.Add",
                                    "Journal entry added mail address",
                                    "Added mail address {0}",
                                    t => address.GetText(t));
                                break;
                            case ServiceType.Phone:
                                Journal(address.Contact,
                                    "ServiceAddress.Phone.Journal.Add",
                                    "Journal entry added phone number",
                                    "Added phone number {0}",
                                    t => address.GetText(Translator));
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/serviceaddress/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var address = Database.Query<ServiceAddress>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(address))
                {
                    if (status.HasAccess(address.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        Database.Delete(address);
                        switch (address.Service.Value)
                        {
                            case ServiceType.EMail:
                                Journal(address.Contact, 
                                    "ServiceAddress.EMail.Journal.Delete",
                                    "Journal entry deleted mail address",
                                    "Deleted mail address {0}",
                                    t => address.GetText(Translator));
                                break;
                            case ServiceType.Phone:
                                Journal(address.Contact, 
                                    "ServiceAddress.Phone.Journal.Delete",
                                    "Journal entry deleted phone number",
                                    "Deleted phone number {0}",
                                    t => address.GetText(Translator));
                                break;
                            default:
                                throw new NotSupportedException();
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Post["/serviceaddress/switch"] = parameters =>
            {
                var model = JsonConvert.DeserializeObject<SwitchViewModel>(ReadBody());
                var source = Database.Query<ServiceAddress>(model.SourceId);
                var status = CreateStatus();

                if (status.ObjectNotNull(source) &&
                    Guid.TryParse(model.TargetId, out Guid targetId))
                {
                    if (status.HasAccess(source.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        var target = source.Contact.Value.ServiceAddresses
                            .FirstOrDefault(a => a.Id.Equals(targetId));

                        if (status.ObjectNotNull(target))
                        {
                            var sourcePrecedence = source.Precedence.Value;
                            var targetPrecedence = target.Precedence.Value;
                            source.Precedence.Value = targetPrecedence;
                            target.Precedence.Value = sourcePrecedence;

                            Database.Save(source.Contact);
                            switch (source.Service.Value)
                            {
                                case ServiceType.EMail:
                                    Journal(source.Contact, 
                                        "ServiceAddress.EMail.Journal.Switch",
                                        "Journal entry deleted mail address",
                                        "Switched mail addresses {0} and {1}",
                                        t => source.GetText(t),
                                        t => target.GetText(t));
                                    break;
                                case ServiceType.Phone:
                                    Journal(source.Contact, 
                                        "ServiceAddress.Phone.Journal.Delete",
                                        "Journal entry deleted phone Switch",
                                        "Switched phone numbers {0} and {1}",
                                        t => source.GetText(t),
                                        t => target.GetText(t));
                                    break;
                                default:
                                    throw new NotSupportedException();
                            }
                        }
                    }
                }
                else
                {
                    status.SetErrorNotFound(); 
                }

                return status.CreateJsonData();
            };
        }
    }
}

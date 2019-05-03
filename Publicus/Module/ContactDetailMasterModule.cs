using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactMasterViewModel
    {
        public string Id;
        public bool DemographyRead;
        public bool ContactRead;

        public ContactMasterViewModel(Contact contact, Session session)
        {
            Id = contact.Id.ToString();
            DemographyRead = session.HasAccess(contact, PartAccess.Demography, AccessRight.Read);
            ContactRead = session.HasAccess(contact, PartAccess.Demography, AccessRight.Read);
        }
    }

    public class ContactMasterSectionViewModel
    {
        public string Id;
        public string Title;

        public ContactMasterSectionViewModel(Contact contact, string title)
        {
            Id = contact.Id.ToString();
            Title = title;
        }
    }

    public class PostalAddressViewModel
    {
        public string Id;
        public string Text;
        public string PhraseDeleteConfirmationQuestion;

        public PostalAddressViewModel(Translator translator, PostalAddress address)
        {
            Id = address.Id.ToString();
            Text = string.Join("<br/>", address.Elements.Select(e => e.EscapeHtml()));
            PhraseDeleteConfirmationQuestion = translator.Get("Contact.Detail.Master.PostalAddresses.Delete.Confirm.Question", "Delete postal address confirmation question", "Do you really wish to delete postal address {0}?", address.GetText(translator)).EscapeHtml();
        }
    }

    public class ContactMasterPostalViewModel : ContactMasterSectionViewModel
    {
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public string Editable;
        public List<PostalAddressViewModel> List;

        public ContactMasterPostalViewModel(Translator translator, Session session, Contact contact)
            : base(contact, 
                   translator.Get("Contact.Detail.Master.PostalAddresses.Title", "Title of the section 'Postal addreses' on the master data tab in the contact detail page", "Postal addresses"))
        {
            List = new List<PostalAddressViewModel>(
                contact.PostalAddresses
                .OrderBy(a => a.Precedence.Value)
                .Select(a => new PostalAddressViewModel(translator, a)));
            Editable =
                session.HasAccess(contact, PartAccess.Contact, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationTitle = translator.Get("Contact.Detail.Master.PostalAddresses.Delete.Confirm.Title", "Delete postal address confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class ServiceAddressViewModel
    {
        public string Id;
        public string Text;
        public string PhraseDeleteConfirmationQuestion;

        public ServiceAddressViewModel(Translator translator, ServiceAddress address)
        {
            Id = address.Id.ToString();
            Text = address.Address.Value.EscapeHtml();

            switch (address.Service.Value)
            {
                case ServiceType.EMail:
                    PhraseDeleteConfirmationQuestion = translator.Get("Contact.Detail.Master.Mail.Delete.Confirm.Question", "Delete E-Mail address confirmation question", "Do you really wish to delete E-Mail address {0}?", address.GetText(translator)).EscapeHtml();
                    break;
                case ServiceType.Phone:
                    PhraseDeleteConfirmationQuestion = translator.Get("Contact.Detail.Master.Phone.Delete.Confirm.Question", "Delete phone number confirmation question", "Do you really wish to delete phone number {0}?", address.GetText(translator)).EscapeHtml();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class ContactMasterServiceAddressViewModel : ContactMasterSectionViewModel
    {
        private static string GetTitle(Translator translator, ServiceType service)
        {
            switch (service)
            {
                case ServiceType.EMail:
                    return translator.Get("Contact.Detail.Master.Mail.Title", "Title of the section 'E-Mail addresses' on the master data tab in the contact detail page", "E-Mail addresses").EscapeHtml();
                case ServiceType.Phone:
                    return translator.Get("Contact.Detail.Master.Phone.Title", "Title of the section 'Phone numbers' on the master data tab in the contact detail page", "Phone numbers").EscapeHtml();
                default:
                    throw new NotSupportedException(); 
            }
        }

        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public string Editable;
        public List<ServiceAddressViewModel> List;

        public ContactMasterServiceAddressViewModel(Translator translator, Session session, Contact contact, ServiceType service)
            : base(contact, GetTitle(translator, service))
        {
            List = new List<ServiceAddressViewModel>(
                contact.ServiceAddresses
                .Where(a => a.Service.Value == service)
                .OrderBy(a => a.Precedence.Value)
                .Select(a => new ServiceAddressViewModel(translator, a)));
            Editable =
                session.HasAccess(contact, PartAccess.Contact, AccessRight.Write) ?
                "editable" : "accessdenied";

            switch (service)
            {
                case ServiceType.EMail:
                    PhraseDeleteConfirmationTitle = translator.Get("Contact.Detail.Master.Mail.Delete.Confirm.Title", "Delete E-Mail address confirmation title", "Delete?").EscapeHtml();
                    break;
                case ServiceType.Phone:
                    PhraseDeleteConfirmationTitle = translator.Get("Contact.Detail.Master.Phone.Delete.Confirm.Title", "Delete phone number confirmation title", "Delete?").EscapeHtml();
                    break;
                default:
                    throw new NotSupportedException();
            }
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class ContactDetailMasterModule : PublicusModule
    {
        public ContactDetailMasterModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/master/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Anonymous, AccessRight.Read))
                    {
                        return View["View/contactdetail_master.sshtml",
                            new ContactMasterViewModel(contact, CurrentSession)];
                    }
                }

                return null;
            };
            Get["/contact/detail/master/postal/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/contactdetail_master_postal.sshtml",
                            new ContactMasterPostalViewModel(Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
            Get["/contact/detail/master/email/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/contactdetail_master_email.sshtml", 
                            new ContactMasterServiceAddressViewModel(Translator, CurrentSession, contact, ServiceType.EMail)];
                    }
                }

                return null;
            };
            Get["/contact/detail/master/phone/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/contactdetail_master_phone.sshtml", 
                            new ContactMasterServiceAddressViewModel(Translator, CurrentSession, contact, ServiceType.Phone)];
                    }
                }

                return null;
            };
        }
    }
}

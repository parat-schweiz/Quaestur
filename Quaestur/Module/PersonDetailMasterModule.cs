using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class PersonMasterViewModel
    {
        public string Id;
        public bool DemographyRead;
        public bool ContactRead;

        public PersonMasterViewModel(Person person, Session session)
        {
            Id = person.Id.ToString();
            DemographyRead = session.HasAccess(person, PartAccess.Demography, AccessRight.Read);
            ContactRead = session.HasAccess(person, PartAccess.Demography, AccessRight.Read);
        }
    }

    public class PersonMasterSectionViewModel
    {
        public string Id;
        public string Title;

        public PersonMasterSectionViewModel(Person person, string title)
        {
            Id = person.Id.ToString();
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
            PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.PostalAddresses.Delete.Confirm.Question", "Delete postal address confirmation question", "Do you really wish to delete postal address {0}?", address.GetText(translator)).EscapeHtml();
        }
    }

    public class PersonMasterPostalViewModel : PersonMasterSectionViewModel
    {
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public string Editable;
        public List<PostalAddressViewModel> List;

        public PersonMasterPostalViewModel(Translator translator, Session session, Person person)
            : base(person, 
                   translator.Get("Person.Detail.Master.PostalAddresses.Title", "Title of the section 'Postal addreses' on the master data tab in the person detail page", "Postal addresses"))
        {
            List = new List<PostalAddressViewModel>(
                person.PostalAddresses
                .OrderBy(a => a.Precedence.Value)
                .Select(a => new PostalAddressViewModel(translator, a)));
            Editable =
                session.HasAccess(person, PartAccess.Contact, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.PostalAddresses.Delete.Confirm.Title", "Delete postal address confirmation title", "Delete?").EscapeHtml();
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
                    PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.Mail.Delete.Confirm.Question", "Delete E-Mail address confirmation question", "Do you really wish to delete E-Mail address {0}?", address.GetText(translator)).EscapeHtml();
                    break;
                case ServiceType.Phone:
                    PhraseDeleteConfirmationQuestion = translator.Get("Person.Detail.Master.Phone.Delete.Confirm.Question", "Delete phone number confirmation question", "Do you really wish to delete phone number {0}?", address.GetText(translator)).EscapeHtml();
                    break;
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class PersonMasterServiceAddressViewModel : PersonMasterSectionViewModel
    {
        private static string GetTitle(Translator translator, ServiceType service)
        {
            switch (service)
            {
                case ServiceType.EMail:
                    return translator.Get("Person.Detail.Master.Mail.Title", "Title of the section 'E-Mail addresses' on the master data tab in the person detail page", "E-Mail addresses").EscapeHtml();
                case ServiceType.Phone:
                    return translator.Get("Person.Detail.Master.Phone.Title", "Title of the section 'Phone numbers' on the master data tab in the person detail page", "Phone numbers").EscapeHtml();
                default:
                    throw new NotSupportedException(); 
            }
        }

        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public string Editable;
        public List<ServiceAddressViewModel> List;

        public PersonMasterServiceAddressViewModel(Translator translator, Session session, Person person, ServiceType service)
            : base(person, GetTitle(translator, service))
        {
            List = new List<ServiceAddressViewModel>(
                person.ServiceAddresses
                .Where(a => a.Service.Value == service)
                .OrderBy(a => a.Precedence.Value)
                .Select(a => new ServiceAddressViewModel(translator, a)));
            Editable =
                session.HasAccess(person, PartAccess.Contact, AccessRight.Write) ?
                "editable" : "accessdenied";

            switch (service)
            {
                case ServiceType.EMail:
                    PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.Mail.Delete.Confirm.Title", "Delete E-Mail address confirmation title", "Delete?").EscapeHtml();
                    break;
                case ServiceType.Phone:
                    PhraseDeleteConfirmationTitle = translator.Get("Person.Detail.Master.Phone.Delete.Confirm.Title", "Delete phone number confirmation title", "Delete?").EscapeHtml();
                    break;
                default:
                    throw new NotSupportedException();
            }
            PhraseDeleteConfirmationInfo = string.Empty;
        }
    }

    public class PersonDetailMasterModule : QuaesturModule
    {
        public PersonDetailMasterModule()
        {
            RequireCompleteLogin();

            Get["/person/detail/master/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Anonymous, AccessRight.Read))
                    {
                        return View["View/persondetail_master.sshtml",
                            new PersonMasterViewModel(person, CurrentSession)];
                    }
                }

                return null;
            };
            Get["/person/detail/master/postal/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/persondetail_master_postal.sshtml",
                            new PersonMasterPostalViewModel(Translator, CurrentSession, person)];
                    }
                }

                return null;
            };
            Get["/person/detail/master/email/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/persondetail_master_email.sshtml", 
                            new PersonMasterServiceAddressViewModel(Translator, CurrentSession, person, ServiceType.EMail)];
                    }
                }

                return null;
            };
            Get["/person/detail/master/phone/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/persondetail_master_phone.sshtml", 
                            new PersonMasterServiceAddressViewModel(Translator, CurrentSession, person, ServiceType.Phone)];
                    }
                }

                return null;
            };
        }
    }
}

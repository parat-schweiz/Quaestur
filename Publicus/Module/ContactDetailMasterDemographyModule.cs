using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailDemographyItemViewModel
    {
        public string Phrase;
        public string Text;

        public ContactDetailDemographyItemViewModel(string phrase, string text)
        {
            Phrase = phrase.EscapeHtml();
            Text = text.EscapeHtml();
        }
    }

    public class ContactDetailDemographyViewModel
    {
        public string Title;
        public string Id;
        public string Editable;
        public List<ContactDetailDemographyItemViewModel> List;

        public ContactDetailDemographyViewModel(Translator translator, Session session, Contact contact)
        {
            Title = translator.Get("Contact.Detail.Demography.Title", "Title of the demography part of the contact detail page", "Demography").EscapeHtml();
            Id = contact.Id.Value.ToString();
            List = new List<ContactDetailDemographyItemViewModel>();
            List.Add(new ContactDetailDemographyItemViewModel(
                translator.Get("Contact.Detail.Demography.Birthdate", "Birthdate item in demography part of the contact detail page", "Birthdate"), 
                contact.BirthDate.Value.ToString("dd.MM.yyyy")));
            List.Add(new ContactDetailDemographyItemViewModel(
                translator.Get("Contact.Detail.Demography.Language", "Language item in demography part of the contact detail page", "Language"),
                contact.Language.Value.Translate(translator)));
            Editable =
                session.HasAccess(contact, PartAccess.Demography, AccessRight.Write) ?
                "editable" : "accessdenied";
        }
    }

    public class ContactDetailDemographyModule : PublicusModule
    {
        public ContactDetailDemographyModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/master/demography/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Demography, AccessRight.Read))
                    {
                        return View["View/contactdetail_master_demography.sshtml", 
                            new ContactDetailDemographyViewModel(Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

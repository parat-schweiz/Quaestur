using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactEditHeadViewModel : DialogViewModel
    {
        public string Id = string.Empty;
        public string Organization = string.Empty;
        public string Titles = string.Empty;
        public string FirstName = string.Empty;
        public string MiddleNames = string.Empty;
        public string LastName = string.Empty;

        public string PhraseFieldOrganization;
        public string PhraseFieldTitle;
        public string PhraseFieldFirstName;
        public string PhraseFieldMiddleNames;
        public string PhraseFieldLastName;

        public ContactEditHeadViewModel()
        {
        }

        public ContactEditHeadViewModel(Translator translator, Session session, Contact contact)
          : base(translator, 
                 translator.Get("Contact.Edit.Head.Title", "Title of the edit names dialog", "Edit names"), 
                 "contactEditHeadDialog")
        {
            PhraseFieldOrganization = translator.Get("Contact.Edit.Head.Field.Organization", "Field 'Organization' in the edit names dialog", "Organization").EscapeHtml();
            PhraseFieldTitle = translator.Get("Contact.Edit.Head.Field.Title", "Field 'Title' in the edit names dialog", "Title").EscapeHtml();
            PhraseFieldFirstName = translator.Get("Contact.Edit.Head.Field.FirstName", "Field 'First name' in the edit names dialog", "First name").EscapeHtml();
            PhraseFieldMiddleNames = translator.Get("Contact.Edit.Head.Field.MiddleNames", "Field 'Middle names' in the edit names dialog", "Middle names").EscapeHtml();
            PhraseFieldLastName = translator.Get("Contact.Edit.Head.Field.LastName", "Field 'Last name' in the edit names dialog", "Last name").EscapeHtml();
            Id = contact.Id.ToString();
            Organization = contact.Organization.Value.EscapeHtml();
            Titles = contact.Title.Value.EscapeHtml();
            FirstName = contact.FirstName.Value.EscapeHtml();
            MiddleNames = contact.MiddleNames.Value.EscapeHtml();
            LastName = contact.LastName.Value.EscapeHtml();
        }
    }

    public class ContactDetailEditHeadModule : PublicusModule
    {
        public ContactDetailEditHeadModule()
        {
            this.RequiresAuthentication();

            Get["/contact/edit/head/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Demography, AccessRight.Write))
                    {
                        return View["View/contactedit_head.sshtml", new ContactEditHeadViewModel(Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
            Post["/contact/edit/head/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<ContactEditHeadViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    if (status.HasAccess(contact, PartAccess.Demography, AccessRight.Write))
                    {
                        status.AssignStringFree("Organization", contact.Organization, model.Organization);
                        status.AssignStringFree("Title", contact.Title, model.Titles);
                        status.AssignStringFree("FirstName", contact.FirstName, model.FirstName);
                        status.AssignStringFree("MiddleNames", contact.MiddleNames, model.MiddleNames);
                        status.AssignStringFree("LastName", contact.LastName, model.LastName);

                        if (status.IsSuccess)
                        {
                            Database.Save(contact);
                            Journal(contact,
                                "Name.Journal.Edit",
                                "Journal entry edited names",
                                "Changed names {0}",
                                t => contact.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailJournalItemViewModel
    {
        public string Id;
        public string Moment;
        public string Subject;
        public string Text;

        public ContactDetailJournalItemViewModel(Translator translator, JournalEntry entry)
        {
            Id = entry.Id.Value.ToString();
            Moment = entry.Moment.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            Subject = entry.Subject.Value.EscapeHtml();
            Text = entry.Text.Value.EscapeHtml();
        }
    }

    public class ContactDetailJournalViewModel
    {
        public string Id;
        public List<ContactDetailJournalItemViewModel> List;
        public string PhraseHeaderMoment;
        public string PhraseHeaderSubject;
        public string PhraseHeaderText;

        public ContactDetailJournalViewModel(Translator translator, IDatabase database, Session session, Contact contact)
        {
            Id = contact.Id.Value.ToString();
            List = new List<ContactDetailJournalItemViewModel>(database
                .Query<JournalEntry>(DC.Equal("contactid", contact.Id.Value))
                .OrderByDescending(d => d.Moment.Value)
                .Select(d => new ContactDetailJournalItemViewModel(translator, d)));
            PhraseHeaderMoment = translator.Get("Contact.Detail.Journal.Header.Moment", "Column 'Moment' on the journal tab of the contact detail page", "When").EscapeHtml();
            PhraseHeaderSubject = translator.Get("Contact.Detail.Journal.Header.Subject", "Column 'Subject' on the journal tab of the contact detail page", "Who").EscapeHtml();
            PhraseHeaderText = translator.Get("Contact.Detail.Journal.Header.Text", "Column 'Text' on the journal tab of the contact detail page", "What").EscapeHtml();
        }
    }

    public class ContactDetailJournalModule : PublicusModule
    {
        public ContactDetailJournalModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/journal/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Journal, AccessRight.Read))
                    {
                        return View["View/contactdetail_journal.sshtml", 
                            new ContactDetailJournalViewModel(Translator, Database, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

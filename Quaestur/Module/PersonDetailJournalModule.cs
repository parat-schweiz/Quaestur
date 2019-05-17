using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDetailJournalItemViewModel
    {
        public string Id;
        public string Moment;
        public string Subject;
        public string Text;

        public PersonDetailJournalItemViewModel(Translator translator, JournalEntry entry)
        {
            Id = entry.Id.Value.ToString();
            Moment = entry.Moment.Value.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            Subject = entry.Subject.Value.EscapeHtml();
            Text = entry.Text.Value.EscapeHtml();
        }
    }

    public class PersonDetailJournalViewModel
    {
        public string Id;
        public List<PersonDetailJournalItemViewModel> List;
        public string PhraseHeaderMoment;
        public string PhraseHeaderSubject;
        public string PhraseHeaderText;

        public PersonDetailJournalViewModel(Translator translator, IDatabase database, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailJournalItemViewModel>(database
                .Query<JournalEntry>(DC.Equal("personid", person.Id.Value))
                .OrderByDescending(d => d.Moment.Value)
                .Select(d => new PersonDetailJournalItemViewModel(translator, d)));
            PhraseHeaderMoment = translator.Get("Person.Detail.Journal.Header.Moment", "Column 'Moment' on the journal tab of the person detail page", "When").EscapeHtml();
            PhraseHeaderSubject = translator.Get("Person.Detail.Journal.Header.Subject", "Column 'Subject' on the journal tab of the person detail page", "Who").EscapeHtml();
            PhraseHeaderText = translator.Get("Person.Detail.Journal.Header.Text", "Column 'Text' on the journal tab of the person detail page", "What").EscapeHtml();
        }
    }

    public class PersonDetailJournalModule : QuaesturModule
    {
        public PersonDetailJournalModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/journal/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Journal, AccessRight.Read))
                    {
                        return View["View/persondetail_journal.sshtml", 
                            new PersonDetailJournalViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return null;
            });
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Hospes
{
    public class PersonDetailDeleteItemViewModel
    {
        public string RowId;
        public string Phrase;
        public string PhraseConfirmationTitle;
        public string PhraseConfirmationQuestion;
        public string Path;

        public PersonDetailDeleteItemViewModel(
            string rowId, 
            string phrase, 
            string phraseConfirmationTitle,
            string phraseConfirmationQuestion,
            string path)
        {
            RowId = rowId;
            Phrase = phrase.EscapeHtml();
            PhraseConfirmationTitle = phraseConfirmationTitle.EscapeHtml();
            PhraseConfirmationQuestion = phraseConfirmationQuestion.EscapeHtml();
            Path = path;
        }
    }

    public class PersonDetailDeleteViewModel
    {
        public string Title;
        public string Id;
        public List<PersonDetailDeleteItemViewModel> List;

        public PersonDetailDeleteViewModel(Translator translator, Session session, Person person)
        {
            Title = translator.Get("Person.Detail.Delete.Title", "Title of the delete part of the person detail page", "Delete").EscapeHtml();
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailDeleteItemViewModel>();

            if (!person.Deleted &&
                session.HasAccess(person, PartAccess.Contact, AccessRight.Write) &&
                (person != session.User))
            {
                List.Add(new PersonDetailDeleteItemViewModel(
                    "personDeleteMark",
                    translator.Get("Person.Detail.Delete.Mark", "Mark as delete in the person master data tab in the person detail page", "Mark as deleted"),
                    translator.Get("Person.Detail.Delete.Mark.Confirmation.Title", "Confirmation title when mark as delete in the person master data tab in the person detail page", "Mark as delete?"),
                    translator.Get("Person.Detail.Delete.Mark.Confirmation.Question", "Confirmation question when mark as delete in the person master data tab in the person detail page", "Do you really wish to mark this person as deleted?"),
                    "/person/delete/mark/" + person.Id.Value.ToString()));
            }

            if (person.Deleted &&
                session.HasAccess(person, PartAccess.Deleted, AccessRight.Write) &&
                (person != session.User))
            {
                List.Add(new PersonDetailDeleteItemViewModel(
                    "personDeleteUnmark",
                    translator.Get("Person.Detail.Delete.Unmark", "Unmark as delete in the person master data tab in the person detail page", "Undelete"),
                    translator.Get("Person.Detail.Delete.Unmark.Confirmation.Title", "Confirmation title when unmark as delete in the person master data tab in the person detail page", "Undelete?"),
                    translator.Get("Person.Detail.Delete.Unmark.Confirmation.Question", "Confirmation question when unmark as delete in the person master data tab in the person detail page", "Do you really wish to undelete this person?"),
                    "/person/delete/unmark/" + person.Id.Value.ToString()));
            }

            if (person.Deleted &&
                session.HasAccess(person, PartAccess.Deleted, AccessRight.Write) &&
                (person != session.User))
            {
                List.Add(new PersonDetailDeleteItemViewModel(
                    "personDeleteHard",
                    translator.Get("Person.Detail.Delete.Hard", "Hard delete in the person master data tab in the person detail page", "Delete from database"),
                    translator.Get("Person.Detail.Delete.Hard.Confirmation.Title", "Confirmation title when hard delete in the person master data tab in the person detail page", "Delete from database?"),
                    translator.Get("Person.Detail.Delete.Hard.Confirmation.Question", "Confirmation question when hard delete in the person master data tab in the person detail page", "Do you wish to delete this person from the database? This action cannot be undone."),
                    "/person/delete/hard/" + person.Id.Value.ToString()));
            }
        }
    }

    public class PersonDetailDeleteModule : QuaesturModule
    {
        public PersonDetailDeleteModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/master/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Contact, AccessRight.Read))
                    {
                        return View["View/persondetail_master_delete.sshtml", 
                            new PersonDetailDeleteViewModel(Translator, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
        }
    }
}

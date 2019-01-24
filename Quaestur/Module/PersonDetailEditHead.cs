using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class PersonEditHeadViewModel : DialogViewModel
    {
        public string Id = string.Empty;
        public string Number = string.Empty;
        public string UserName = string.Empty;
        public string Titles = string.Empty;
        public string FirstName = string.Empty;
        public string MiddleNames = string.Empty;
        public string LastName = string.Empty;

        public bool HasAllAccess;

        public string PhraseFieldNumber;
        public string PhraseFieldUserName;
        public string PhraseFieldTitle;
        public string PhraseFieldFirstName;
        public string PhraseFieldMiddleNames;
        public string PhraseFieldLastName;

        public PersonEditHeadViewModel()
        {
        }

        public PersonEditHeadViewModel(Translator translator, Session session, Person person)
          : base(translator, 
                 translator.Get("Person.Edit.Head.Title", "Title of the edit names dialog", "Edit names"), 
                 "personEditHeadDialog")
        {
            PhraseFieldNumber = translator.Get("Person.Edit.Head.Field.Number", "Field 'Number' in the edit names dialog", "Number").EscapeHtml();
            PhraseFieldUserName = translator.Get("Person.Edit.Head.Field.Username", "Field 'Username' in the edit names dialog", "Username").EscapeHtml();
            PhraseFieldTitle = translator.Get("Person.Edit.Head.Field.Title", "Field 'Title' in the edit names dialog", "Title").EscapeHtml();
            PhraseFieldFirstName = translator.Get("Person.Edit.Head.Field.FirstName", "Field 'First name' in the edit names dialog", "First name").EscapeHtml();
            PhraseFieldMiddleNames = translator.Get("Person.Edit.Head.Field.MiddleNames", "Field 'Middle names' in the edit names dialog", "Middle names").EscapeHtml();
            PhraseFieldLastName = translator.Get("Person.Edit.Head.Field.LastName", "Field 'Last name' in the edit names dialog", "Last name").EscapeHtml();
            HasAllAccess = session.HasAllAccessOf(person);
            Id = person.Id.ToString();
            Number = person.Number.ToString();
            UserName = person.UserName.Value.EscapeHtml();
            Titles = person.Title.Value.EscapeHtml();
            FirstName = person.FirstName.Value.EscapeHtml();
            MiddleNames = person.MiddleNames.Value.EscapeHtml();
            LastName = person.LastName.Value.EscapeHtml();
        }
    }

    public class PersonDetailEditHeadModule : QuaesturModule
    {
        public PersonDetailEditHeadModule()
        {
            RequireCompleteLogin();

            Get["/person/edit/head/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Demography, AccessRight.Write))
                    {
                        return View["View/personedit_head.sshtml", new PersonEditHeadViewModel(Translator, CurrentSession, person)];
                    }
                }

                return null;
            };
            Post["/person/edit/head/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PersonEditHeadViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Demography, AccessRight.Write))
                    {
                        if (HasAllAccessOf(person))
                        {
                            status.AssignStringRequired("UserName", person.UserName, model.UserName);
                        }

                        status.AssignStringFree("Title", person.Title, model.Titles);
                        status.AssignStringFree("FirstName", person.FirstName, model.FirstName);
                        status.AssignStringFree("MiddleNames", person.MiddleNames, model.MiddleNames);
                        status.AssignStringFree("LastName", person.LastName, model.LastName);

                        if (status.IsSuccess)
                        {
                            Database.Save(person);
                            Journal(person,
                                "Name.Journal.Edit",
                                "Journal entry edited names",
                                "Changed names {0}",
                                t => person.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

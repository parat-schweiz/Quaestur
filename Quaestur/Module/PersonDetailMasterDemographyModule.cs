using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDetailDemographyItemViewModel
    {
        public string Phrase;
        public string Text;

        public PersonDetailDemographyItemViewModel(string phrase, string text)
        {
            Phrase = phrase.EscapeHtml();
            Text = text.EscapeHtml();
        }
    }

    public class PersonDetailDemographyViewModel
    {
        public string Title;
        public string Id;
        public string Editable;
        public List<PersonDetailDemographyItemViewModel> List;

        public PersonDetailDemographyViewModel(Translator translator, Session session, Person person)
        {
            Title = translator.Get("Person.Detail.Demography.Title", "Title of the demography part of the person detail page", "Demography").EscapeHtml();
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailDemographyItemViewModel>();
            List.Add(new PersonDetailDemographyItemViewModel(
                translator.Get("Person.Detail.Demography.Birthdate", "Birthdate item in demography part of the person detail page", "Birthdate"), 
                person.BirthDate.Value.ToString("dd.MM.yyyy")));
            List.Add(new PersonDetailDemographyItemViewModel(
                translator.Get("Person.Detail.Demography.Language", "Language item in demography part of the person detail page", "Language"),
                person.Language.Value.Translate(translator)));
            Editable =
                session.HasAccess(person, PartAccess.Demography, AccessRight.Write) ?
                "editable" : "accessdenied";
        }
    }

    public class PersonDetailDemographyModule : QuaesturModule
    {
        public PersonDetailDemographyModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/master/demography/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Demography, AccessRight.Read))
                    {
                        return View["View/persondetail_master_demography.sshtml", 
                            new PersonDetailDemographyViewModel(Translator, CurrentSession, person)];
                    }
                }

                return null;
            });
        }
    }
}

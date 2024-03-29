﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class DemographyEditViewModel : DialogViewModel
    {
        public string Id;
        public string Birthdate;
        public string Language;
        public List<NamedIntViewModel> Languages;
        public string PhraseFieldBirthdate;
        public string PhraseFieldLanguage;

        public DemographyEditViewModel()
        { 
        }

        public DemographyEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Demography.Edit.Title", "Title of the demography edit dialog", "Edit demography"),
                   "demographyEditDialog")
        {
            PhraseFieldBirthdate = translator.Get("Demography.Edit.Field.Birthdate", "Field 'Birthdate' in the edit demography address dialog", "Birthdate").EscapeHtml();
            PhraseFieldLanguage = translator.Get("Demography.Edit.Field.Language", "Field 'Language' in the edit demography address dialog", "Language").EscapeHtml();
        }

        public DemographyEditViewModel(Translator translator, IDatabase db, Person person)
            : this(translator)
        {
            Id = person.Id.ToString();
            Birthdate = person.BirthDate.Value.FormatSwissDateDay();
            Language = ((int)person.Language.Value).ToString();
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.English, person.Language.Value == SiteLibrary.Language.English));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.German, person.Language.Value == SiteLibrary.Language.German));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.French, person.Language.Value == SiteLibrary.Language.French));
            Languages.Add(new NamedIntViewModel(translator, SiteLibrary.Language.Italian, person.Language.Value == SiteLibrary.Language.Italian));
        }
    }

    public class DemographyEdit : QuaesturModule
    {
        public DemographyEdit()
        {
            RequireCompleteLogin();

            Get("/demography/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Demography, AccessRight.Write))
                    {
                        return View["View/demographyedit.sshtml",
                            new DemographyEditViewModel(Translator, Database, person)];
                    }
                }

                return string.Empty;
            });
            Post("/demography/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<DemographyEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Demography, AccessRight.Write))
                    {
                        status.AssignDateString("Birthdate", person.BirthDate, model.Birthdate);
                        status.AssignEnumIntString("Language", person.Language, model.Language);

                        if (status.IsSuccess)
                        {
                            Database.Save(person);
                            Journal(person,
                                "Person.Demography.Edit",
                                "Journal entry edited demography",
                                "Changed demography {0}",
                                t => person.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

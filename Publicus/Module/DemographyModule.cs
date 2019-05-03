using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
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

        public DemographyEditViewModel(Translator translator, IDatabase db, Contact contact)
            : this(translator)
        {
            Id = contact.Id.ToString();
            Birthdate = contact.BirthDate.Value.ToString("dd.MM.yyyy");
            Language = ((int)contact.Language.Value).ToString();
            Languages = new List<NamedIntViewModel>();
            Languages.Add(new NamedIntViewModel(translator, Publicus.Language.English, contact.Language.Value == Publicus.Language.English));
            Languages.Add(new NamedIntViewModel(translator, Publicus.Language.German, contact.Language.Value == Publicus.Language.German));
            Languages.Add(new NamedIntViewModel(translator, Publicus.Language.French, contact.Language.Value == Publicus.Language.French));
            Languages.Add(new NamedIntViewModel(translator, Publicus.Language.Italian, contact.Language.Value == Publicus.Language.Italian));
        }
    }

    public class DemographyEdit : PublicusModule
    {
        public DemographyEdit()
        {
            this.RequiresAuthentication();

            Get["/demography/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Demography, AccessRight.Write))
                    {
                        return View["View/demographyedit.sshtml",
                            new DemographyEditViewModel(Translator, Database, contact)];
                    }
                }

                return null;
            };
            Post["/demography/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<DemographyEditViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    if (status.HasAccess(contact, PartAccess.Demography, AccessRight.Write))
                    {
                        status.AssignDateString("Birthdate", contact.BirthDate, model.Birthdate);
                        status.AssignEnumIntString("Language", contact.Language, model.Language);

                        if (status.IsSuccess)
                        {
                            Database.Save(contact);
                            Journal(contact,
                                "Contact.Demography.Edit",
                                "Journal entry edited demography",
                                "Changed demography {0}",
                                t => contact.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

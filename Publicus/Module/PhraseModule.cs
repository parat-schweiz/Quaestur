using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class PhraseEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Key;
        public string Hint;
        public string Technical;
        public string English;
        public string German;
        public string French;
        public string Italian;
        public string PhraseFieldKey;
        public string PhraseFieldHint;
        public string PhraseFieldTechnical;
        public string PhraseFieldEnglish;
        public string PhraseFieldGerman;
        public string PhraseFieldFrench;
        public string PhraseFieldItalian;

        public PhraseEditViewModel()
        { 
        }

        public PhraseEditViewModel(Translator translator)
            : base(translator, translator.Get("Phrase.Edit.Title", "Title of the phrase edit dialog", "Edit phrase"), "phraseEditDialog")
        {
            PhraseFieldKey = translator.Get("Phrase.Edit.Field.Key", "Key field in the phrase edit dialog", "Key").EscapeHtml();
            PhraseFieldHint = translator.Get("Phrase.Edit.Field.Hint", "Hint field in the phrase edit dialog", "Hint").EscapeHtml();
            PhraseFieldTechnical = translator.Get("Phrase.Edit.Field.Technical", "Technical field in the phrase edit dialog", "Technical").EscapeHtml();
            PhraseFieldEnglish = translator.Get("Phrase.Edit.Field.English", "English field in the phrase edit dialog", "English").EscapeHtml();
            PhraseFieldGerman = translator.Get("Phrase.Edit.Field.German", "German field in the phrase edit dialog", "German").EscapeHtml();
            PhraseFieldFrench = translator.Get("Phrase.Edit.Field.French", "French field in the phrase edit dialog", "French").EscapeHtml();
            PhraseFieldItalian = translator.Get("Phrase.Edit.Field.Italian", "Italian field in the phrase edit dialog", "Italian").EscapeHtml();
        }

        public PhraseEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Key = string.Empty;
            Hint = string.Empty;
            Technical = string.Empty;
            English = string.Empty;
            German = string.Empty;
            French = string.Empty;
            Italian = string.Empty;
        }

        public PhraseEditViewModel(Translator translator, IDatabase db, Phrase phrase)
            : this(translator)
        {
            Method = "edit";
            Id = phrase.Id.ToString();
            Key = phrase.Key.Value.EscapeHtml();
            Hint = phrase.Hint.Value.EscapeHtml();
            Technical = phrase.Technical.Value.EscapeHtml();
            English = phrase.Translations
                .Where(t => t.Language.Value == Language.English)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? string.Empty;
            German = phrase.Translations
                .Where(t => t.Language.Value == Language.German)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? string.Empty;
            French = phrase.Translations
                .Where(t => t.Language.Value == Language.French)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? string.Empty;
            Italian = phrase.Translations
                .Where(t => t.Language.Value == Language.Italian)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? string.Empty;
        }
    }

    public class PhraseViewModel : MasterViewModel
    {
        public PhraseViewModel(Translator translator, Session session)
            : base(translator, translator.Get("Phrase.List.Title", "Title of the phrase list page", "Countries"), 
            session)
        { 
        }
    }

    public class PhraseListItemViewModel
    {
        public string Id;
        public string Key;
        public string English;
        public string German;
        public string French;
        public string Italian;

        public PhraseListItemViewModel(Translator translator, Phrase phrase)
        {
            Id = phrase.Id.Value.ToString();
            Key = phrase.Key.Value.EscapeHtml();
            English = phrase.Translations
                .Where(t => t.Language.Value == Language.English)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? phrase.Technical.Value.EscapeHtml(); ;
            German = phrase.Translations
                .Where(t => t.Language.Value == Language.German)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? string.Empty;
            French = phrase.Translations
                .Where(t => t.Language.Value == Language.French)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? string.Empty;
            Italian = phrase.Translations
                .Where(t => t.Language.Value == Language.Italian)
                .Select(t => t.Text.Value.EscapeHtml())
                .FirstOrDefault() ?? string.Empty;
        }
    }

    public class PhraseListViewModel
    {
        public string PhraseHeaderKey;
        public string PhraseHeaderEnglish;
        public string PhraseHeaderGerman;
        public string PhraseHeaderFrench;
        public string PhraseHeaderItalian;
        public List<PhraseListItemViewModel> List;

        public PhraseListViewModel(Translator translator, IDatabase database)
        {
            PhraseHeaderKey = translator.Get("Phrase.List.Header.Key", "Column 'Key' in the phrase list", "Key");
            PhraseHeaderEnglish = translator.Get("Phrase.List.Header.English", "Column 'English' in the phrase list", "English");
            PhraseHeaderGerman = translator.Get("Phrase.List.Header.German", "Column 'German' in the phrase list", "German");
            PhraseHeaderFrench = translator.Get("Phrase.List.Header.French", "Column 'French' in the phrase list", "French");
            PhraseHeaderItalian = translator.Get("Phrase.List.Header.Italian", "Column 'Italian' in the phrase list", "Italian");
            List = new List<PhraseListItemViewModel>(
                database.Query<Phrase>()
                .OrderBy(p => p.Key.Value)
                .Select(c => new PhraseListItemViewModel(translator, c)));
        }
    }

    public class PhraseEdit : PublicusModule
    {
        private void AssignLanguageText(Phrase phrase, Language language, string value)
        {
            var translation = phrase.Translations.FirstOrDefault(t => t.Language.Value == language);

            if (string.IsNullOrEmpty(value))
            {
                if (translation != null)
                {
                    translation.Delete(Database);
                    phrase.Translations.Remove(translation); 
                }
            }
            else
            {
                if (translation != null)
                {
                    translation.Text.Value = value;
                    Database.Save(translation);
                }
                else
                {
                    translation = new PhraseTranslation(Guid.NewGuid());
                    translation.Language.Value = language;
                    translation.Text.Value = value;
                    translation.Phrase.Value = phrase;
                    Database.Save(translation);
                }
            }
        }

        public PhraseEdit()
        {
            this.RequiresAuthentication();

            Get["/phrase"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/phrase.sshtml",
                        new PhraseViewModel(Translator, CurrentSession)];
                }
                return AccessDenied();
            };
            Get["/phrase/list"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    return View["View/phraselist.sshtml",
                        new PhraseListViewModel(Translator, Database)];
                }
                return null;
            };
            Get["/phrase/edit/{id}"] = parameters =>
            {
                if (HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var phrase = Database.Query<Phrase>(idString);

                    if (phrase != null)
                    {
                        return View["View/phraseedit.sshtml",
                            new PhraseEditViewModel(Translator, Database, phrase)];
                    }
                }
                return null;
            };
            Post["/phrase/edit/{id}"] = parameters =>
            {
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.CustomDefinitions, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var model = JsonConvert.DeserializeObject<PhraseEditViewModel>(ReadBody());
                    var phrase = Database.Query<Phrase>(idString);

                    if (status.ObjectNotNull(phrase))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            AssignLanguageText(phrase, Language.English, model.English);
                            AssignLanguageText(phrase, Language.German, model.German);
                            AssignLanguageText(phrase, Language.French, model.French);
                            AssignLanguageText(phrase, Language.Italian, model.Italian);
                            Database.Save(phrase);
                            transaction.Commit();
                            Notice("{0} changed phrase {1}", CurrentSession.User.UserName.Value, phrase);
                        }
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

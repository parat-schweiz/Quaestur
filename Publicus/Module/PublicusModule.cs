using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nancy;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nancy.Security;

namespace Publicus
{
    public class Translator
    {
        public Translation Translation { get; private set; }
        public Language Language { get; private set; }

        public Translator(Translation translation, Language language)
        {
            Translation = translation;
            Language = language;
        }

        public string Get(string key, string hint, string technical, params object[] parameters)
        {
            return Translation.Get(Language, key, hint, technical, parameters); 
        }

        public string Get(string key, string hint, string technical, IEnumerable<object> parameters)
        {
            return Translation.Get(Language, key, hint, technical, parameters);
        }

        public List<MultiItemViewModel> CreateLanguagesMultiItem(string key, string hint, string technical, MultiLanguageString values)
        {
            var result = new List<MultiItemViewModel>();
            result.Add(new MultiItemViewModel(
                ((int)Language.English).ToString(),
                Get(key, hint, technical, Language.English.Translate(this)),
                values.GetValueOrEmpty(Language.English)));
            result.Add(new MultiItemViewModel(
                ((int)Language.German).ToString(),
                Get(key, hint, technical, Language.German.Translate(this)),
                values.GetValueOrEmpty(Language.German)));
            result.Add(new MultiItemViewModel(
                ((int)Language.French).ToString(),
                Get(key, hint, technical, Language.French.Translate(this)),
                values.GetValueOrEmpty(Language.French)));
            result.Add(new MultiItemViewModel(
                ((int)Language.Italian).ToString(),
                Get(key, hint, technical, Language.Italian.Translate(this)),
                values.GetValueOrEmpty(Language.Italian)));
            return result;
        }
    }

    public class MultiItemViewModel
    {
        public string Key;
        public string Phrase;
        public string Value;

        public MultiItemViewModel()
        { 
        }

        public MultiItemViewModel(string key, string phrase, string value)
        {
            Key = key.EscapeHtml();
            Phrase = phrase.EscapeHtml();
            Value = value.EscapeHtml();
        }
    }

    public class PublicusModule : NancyModule, IDisposable
    {
        protected IDatabase Database { get; private set; }
        protected Translation Translation { get; private set; }

        public PublicusModule()
        { 
            Database = Global.CreateDatabase();
            Translation = new Translation(Database);
        }

        protected string ReadBody()
        {
            using (var reader = new System.IO.StreamReader(Context.Request.Body))
            {
                return reader.ReadToEnd();
            }
        }

        protected byte[] GetDataUrlString(string stringValue)
        {
            if (!string.IsNullOrEmpty(stringValue))
            {
                var parts = stringValue.Split(new string[] { "data:", ";base64," }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 2)
                {
                    try
                    {
                        return Convert.FromBase64String(parts[1]);
                    }
                    catch
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void Notice(string text, params object[] parameters)
        {
            Global.Log.Notice(text, parameters);
        }

        public void Info(string text, params object[] parameters)
        {
            Global.Log.Info(text, parameters);
        }

        public void Warning(string text, params object[] parameters)
        {
            Global.Log.Warning(text, parameters);
        }

        public Session CurrentSession
        {
            get 
            {
                return Context.CurrentUser as Session;
            } 
        }

        public bool HasContactNewAccess()
        {
            return CurrentSession.HasContactNewAccess();
        }

        public bool HasAnyFeedAccess(PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasSystemWideAccess(partAccess, right);
            }
        }

        public bool HasSystemWideAccess(PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasSystemWideAccess(partAccess, right);
            }
        }

        public bool HasAccess(Contact contact, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(contact, partAccess, right); 
            }
        }

        public bool HasAccess(Feed feed, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(feed, partAccess, right);
            }
        }

        public bool HasAccess(Group group, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(group, partAccess, right);
            }
        }

        public Negotiator AccessDenied()
        {
            return View["View/info.sshtml", new AccessDeniedViewModel(Translator)];
        }

        private static Language? ConvertLocale(string locale)
        {
            if (locale.StartsWith("de", StringComparison.InvariantCulture))
            {
                return Language.German;
            }
            else if (locale.StartsWith("fr", StringComparison.InvariantCulture))
            {
                return Language.French;
            }
            else if (locale.StartsWith("it", StringComparison.InvariantCulture))
            {
                return Language.Italian;
            }
            else if (locale.StartsWith("en", StringComparison.InvariantCulture))
            {
                return Language.English;
            }
            else
            {
                return null; 
            }
        }

        public Translator Translator
        {
            get
            {
                return new Translator(Translation, CurrentLanguage);
            }
        }

        public Translator GetTranslator(Language language)
        {
            return new Translator(Translation, language);
        }

        public string Translate(string key, string hint, string technical, params object[] parameters)
        {
            return Translation.Get(CurrentLanguage, key, hint, technical, parameters);
        }

        protected void Journal(string subject, Contact contact, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translator = GetTranslator(contact.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = subject;
            entry.Contact.Value = contact;
            Database.Save(entry);

            var technicalTranslator = GetTranslator(Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Contact.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        protected void Journal(Contact contact, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            Journal(CurrentSession.User.UserName.Value, contact, key, hint, technical, parameters);
        }

        protected void Journal(Contact subject, Contact contact, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            Journal(subject.ShortHand, contact, key, hint, technical, parameters);
        }

        public void Dispose()
        {
            if (Translation != null)
            {
                Translation = null;
            }

            if (Database != null)
            {
                Database.Dispose();
                Database = null; 
            }
        }

        public Language CurrentLanguage
        {
            get
            {
                if (CurrentSession != null)
                {
                    return CurrentSession.User.Language.Value;
                }
                else
                {
                    return BrowserLanguage;
                }
            }
        }

        public Language BrowserLanguage
        {
            get
            {
                var language = Request.Headers.AcceptLanguage
                    .Select(l => new Tuple<Language?, decimal>(ConvertLocale(l.Item1), l.Item2))
                    .Where(l => l.Item1 != null)
                    .OrderByDescending(l => l.Item2)
                    .Select(l => l.Item1)
                    .FirstOrDefault();

                if (language.HasValue)
                {
                    return language.Value;
                }
                else
                {
                    return Language.English;
                }
            }
        }

        protected PostStatus CreateStatus()
        {
            return new PostStatus(Database, Translator, CurrentSession);
        }
    }
}

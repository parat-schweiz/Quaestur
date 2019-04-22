using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nancy;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nancy.Security;

namespace Quaestur
{
    public class Translator
    {
        public Translation Translation { get; private set; }
        public Language Language { get; private set; }

        public CultureInfo Culture
        {
            get
            {
                switch (Language)
                {
                    case Language.German:
                        return new CultureInfo("de-CH");
                    case Language.French:
                        return new CultureInfo("fr-CH");
                    case Language.English:
                        return new CultureInfo("en-US");
                    case Language.Italian:
                        return new CultureInfo("it-IT");
                    default:
                        return new CultureInfo("en-US");
                }
            }
        }

        public string FormatShortDate(DateTime date)
        {
            return date.ToString("d", Culture);
        }

        public string FormatLongDate(DateTime date)
        {
            switch (Language)
            {
                case Language.German:
                    return date.ToString("d. MMMM yyyy");
                case Language.French:
                    return date.ToString("d MMMM yyyy");
                case Language.English:
                    return date.ToString("MMMM dd yyyy");
                case Language.Italian:
                    return date.ToString("d MMMM yyyy");
                default:
                    throw new NotSupportedException();
            }
        }

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

        public List<MultiItemViewModel> CreateLanguagesMultiItem(string key, string hint, string technical, MultiLanguageString values, EscapeMode valueEscapeMode = EscapeMode.Html)
        {
            var result = new List<MultiItemViewModel>();
            result.Add(new MultiItemViewModel(
                ((int)Language.English).ToString(),
                Get(key, hint, technical, Language.English.Translate(this)),
                values.GetValueOrEmpty(Language.English),
                valueEscapeMode));
            result.Add(new MultiItemViewModel(
                ((int)Language.German).ToString(),
                Get(key, hint, technical, Language.German.Translate(this)),
                values.GetValueOrEmpty(Language.German),
                valueEscapeMode));
            result.Add(new MultiItemViewModel(
                ((int)Language.French).ToString(),
                Get(key, hint, technical, Language.French.Translate(this)),
                values.GetValueOrEmpty(Language.French),
                valueEscapeMode));
            result.Add(new MultiItemViewModel(
                ((int)Language.Italian).ToString(),
                Get(key, hint, technical, Language.Italian.Translate(this)),
                values.GetValueOrEmpty(Language.Italian),
                valueEscapeMode));
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

        public MultiItemViewModel(string key, string phrase, string value, EscapeMode valueEscapeMode)
        {
            Key = key.EscapeHtml();
            Phrase = phrase.EscapeHtml();
            Value = value.Escape(valueEscapeMode);
        }
    }

    public class QuaesturModule : NancyModule, IDisposable
    {
        protected IDatabase Database { get; private set; }
        protected Translation Translation { get; private set; }

        public QuaesturModule()
        { 
            Database = Global.CreateDatabase();
            Translation = new Translation(Database);
        }

        protected void RequireCompleteLogin()
        {
            this.RequiresAuthentication();
            this.RequiresClaims(Quaestur.Session.CompleteAuthClaim); 
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

        public bool HasPersonNewAccess()
        {
            return CurrentSession.HasPersonNewAccess();
        }

        public bool HasAllAccessOf(Person person)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAllAccessOf(person);
            }
        }

        public bool HasAnyOrganizationAccess(PartAccess partAccess, AccessRight right)
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

        public bool HasAccess(Person person, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(person, partAccess, right); 
            }
        }

        public bool HasAccess(Organization organization, PartAccess partAccess, AccessRight right)
        {
            var session = CurrentSession;

            if (session == null)
            {
                return false;
            }
            else
            {
                return session.HasAccess(organization, partAccess, right);
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

        protected void Journal(string subject, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translator = GetTranslator(person.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = subject;
            entry.Person.Value = person;
            Database.Save(entry);

            var technicalTranslator = GetTranslator(Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        protected void Journal(Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            Journal(CurrentSession.User, person, key, hint, technical, parameters);
        }

        protected void Journal(Person subject, Person person, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            Journal(subject.ShortHand, person, key, hint, technical, parameters);
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

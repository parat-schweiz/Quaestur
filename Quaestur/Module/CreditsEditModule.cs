using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class CreditsEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string MomentDate;
        public string MomentTime;
        public string Amount;
        public string Reason;
        public string Url;
        public string PhraseFieldMomentDate;
        public string PhraseFieldMomentTime;
        public string PhraseFieldAmount;
        public string PhraseFieldReason;
        public string PhraseFieldUrl;

        public CreditsEditViewModel()
        { 
        }

        public CreditsEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Credits.Edit.Title", "Title of the edit credits dialog", "Edit credits"), 
                   "creditsEditDialog")
        {
            PhraseFieldMomentDate = translator.Get("Credits.Edit.Field.Moment.Date", "Field 'Moment Date' in the edit credits dialog", "Date").EscapeHtml();
            PhraseFieldMomentTime = translator.Get("Credits.Edit.Field.Moment.Time", "Field 'Moment Time' in the edit credits dialog", "Time").EscapeHtml();
            PhraseFieldAmount = translator.Get("Credits.Edit.Field.Amount", "Field 'Amount' in the edit credits dialog", "Amount").EscapeHtml();
            PhraseFieldReason = translator.Get("Credits.Edit.Field.Reason", "Field 'Reason' in the edit credits dialog", "Reason").EscapeHtml();
            PhraseFieldUrl = translator.Get("Credits.Edit.Field.Url", "Field 'Url' in the edit credits dialog", "Url").EscapeHtml();
        }

        public CreditsEditViewModel(Translator translator, Session session, IDatabase db, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            MomentDate = string.Empty;
            MomentTime = string.Empty;
            Amount = string.Empty;
            Reason = string.Empty;
            Url = string.Empty;
        }

        public CreditsEditViewModel(Translator translator, Session session, IDatabase db, Credits credits)
            : this(translator)
        {
            Method = "edit";
            Id = credits.Id.ToString();
            MomentDate = credits.Moment.Value.ToLocalTime().FormatSwissDateDay();
            MomentTime = credits.Moment.Value.ToLocalTime().ToString("HH:mm:ss");
            Amount = credits.Amount.Value.ToString();
            Reason = credits.Reason.Value;
            Url = credits.Url.Value;
        }
    }

    public class CreditsEdit : QuaesturModule
    {
        public CreditsEdit()
        {
            RequireCompleteLogin();

            Get("/credits/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var credits = Database.Query<Credits>(idString);

                if (credits != null)
                {
                    if (HasAccess(credits.Owner.Value, PartAccess.Credits, AccessRight.Write))
                    {
                        return View["View/creditsedit.sshtml",
                            new CreditsEditViewModel(Translator, CurrentSession, Database, credits)];
                    }
                }

                return string.Empty;
            });
            Post("/credits/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<CreditsEditViewModel>(ReadBody());
                var credits = Database.Query<Credits>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(credits))
                {
                    if (status.HasAccess(credits.Owner.Value, PartAccess.Credits, AccessRight.Write))
                    {
                        status.AssignDateString("MomentDate", credits.Moment, model.MomentDate);
                        status.AddAssignTimeString("MomentTime", credits.Moment, model.MomentTime);
                        status.AssignInt32String("Amount", credits.Amount, model.Amount);
                        status.AssignStringRequired("Reason", credits.Reason, model.Reason);
                        status.AssignStringFree("Url", credits.Url, model.Url);

                        if (!string.IsNullOrEmpty(credits.Url.Value) &&
                            !Uri.TryCreate(credits.Url.Value, UriKind.Absolute, out Uri dummy))
                        {
                            status.SetValidationError("Url", "Credits.Edit.Url.Invalid", "When the Url in the credits edit dialog is not valid", "Invalid Url");
                        }

                        credits.Moment.Value = credits.Moment.Value.ToUniversalTime();

                        if (status.IsSuccess)
                        {
                            Database.Save(credits);
                            Journal(credits.Owner.Value,
                                "Credits.Journal.Edit",
                                "Journal entry edited credits",
                                "Change credits {0}",
                                t => credits.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/credits/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Credits, AccessRight.Write))
                    {
                        return View["View/creditsedit.sshtml",
                            new CreditsEditViewModel(Translator, CurrentSession, Database, person)];
                    }
                }

                return string.Empty;
            });
            Post("/credits/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<CreditsEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Credits, AccessRight.Write))
                    {
                        var credits = new Credits(Guid.NewGuid());
                        status.AssignDateString("MomentDate", credits.Moment, model.MomentDate);
                        status.AddAssignTimeString("MomentTime", credits.Moment, model.MomentTime);
                        status.AssignInt32String("Amount", credits.Amount, model.Amount);
                        status.AssignStringRequired("Reason", credits.Reason, model.Reason);
                        status.AssignStringFree("Url", credits.Url, model.Url);
                        credits.Owner.Value = person;
                        credits.Moment.Value = credits.Moment.Value.ToUniversalTime();

                        if (status.IsSuccess)
                        {
                            Database.Save(credits);
                            Journal(credits.Owner.Value,
                                "Credits.Journal.Add",
                                "Journal entry addded credits",
                                "Added credits {0}",
                                t => credits.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/credits/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var credits = Database.Query<Credits>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(credits))
                {
                    if (status.HasAccess(credits.Owner.Value, PartAccess.Credits, AccessRight.Write))
                    {
                        credits.Delete(Database);
                        Journal(credits.Owner.Value,
                            "Credits.Journal.Delete",
                            "Journal entry removed credits",
                            "Removed credits {0}",
                            t => credits.GetText(t));
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

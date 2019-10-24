using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class PointsEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Budget;
        public string MomentDate;
        public string MomentTime;
        public string Amount;
        public string Reason;
        public string Url;
        public List<NamedIdViewModel> Budgets;
        public string PhraseFieldBudget;
        public string PhraseFieldMomentDate;
        public string PhraseFieldMomentTime;
        public string PhraseFieldAmount;
        public string PhraseFieldReason;
        public string PhraseFieldUrl;

        public PointsEditViewModel()
        { 
        }

        public PointsEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Points.Edit.Title", "Title of the edit points dialog", "Edit points"), 
                   "pointsEditDialog")
        {
            PhraseFieldBudget = translator.Get("Points.Edit.Field.Budget", "Field 'Budget' in the edit points dialog", "Budget").EscapeHtml();
            PhraseFieldMomentDate = translator.Get("Points.Edit.Field.Moment.Date", "Field 'Moment Date' in the edit points dialog", "Date").EscapeHtml();
            PhraseFieldMomentTime = translator.Get("Points.Edit.Field.Moment.Time", "Field 'Moment Time' in the edit points dialog", "Time").EscapeHtml();
            PhraseFieldAmount = translator.Get("Points.Edit.Field.Amount", "Field 'Amount' in the edit points dialog", "Amount").EscapeHtml();
            PhraseFieldReason = translator.Get("Points.Edit.Field.Reason", "Field 'Reason' in the edit points dialog", "Reason").EscapeHtml();
            PhraseFieldUrl = translator.Get("Points.Edit.Field.Url", "Field 'Url' in the edit points dialog", "Url").EscapeHtml();
            Budgets = new List<NamedIdViewModel>();
        }

        public PointsEditViewModel(Translator translator, Session session, IDatabase db, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            Budget = string.Empty;
            MomentDate = string.Empty;
            MomentTime = string.Empty;
            Amount = string.Empty;
            Reason = string.Empty;
            Url = string.Empty;
            Budgets.AddRange(
                db.Query<PointBudget>()
                .Where(b => DateTime.UtcNow.Date >= b.Period.Value.StartDate.Value.Date &&
                            DateTime.UtcNow.Date <= b.Period.Value.EndDate.Value.Date.AddDays(30) &&
                            session.HasAccess(b.Owner.Value, PartAccess.Points, AccessRight.Write))
                .Select(b => new NamedIdViewModel(translator, b, false))
                .OrderBy(b => b.Name));
        }

        public PointsEditViewModel(Translator translator, Session session, IDatabase db, Points points)
            : this(translator)
        {
            Method = "edit";
            Id = points.Id.ToString();
            Budget = string.Empty;
            MomentDate = points.Moment.Value.ToLocalTime().ToString("dd.MM.yyyy");
            MomentTime = points.Moment.Value.ToLocalTime().ToString("HH:mm:ss");
            Amount = points.Amount.Value.ToString();
            Reason = points.Reason.Value;
            Url = points.Url.Value;
            Budgets.AddRange(
                db.Query<PointBudget>()
                .Where(b => DateTime.UtcNow.Date >= b.Period.Value.StartDate.Value.Date &&
                            DateTime.UtcNow.Date <= b.Period.Value.EndDate.Value.Date.AddDays(30) &&
                            session.HasAccess(b.Owner.Value, PartAccess.Points, AccessRight.Write))
                .Select(b => new NamedIdViewModel(translator, b, b == points.Budget.Value))
                .OrderBy(b => b.Name));
        }
    }

    public class PointsEdit : QuaesturModule
    {
        public PointsEdit()
        {
            RequireCompleteLogin();

            Get("/points/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var points = Database.Query<Points>(idString);

                if (points != null)
                {
                    if (HasAccess(points.Owner.Value, PartAccess.Points, AccessRight.Write))
                    {
                        return View["View/pointsedit.sshtml",
                            new PointsEditViewModel(Translator, CurrentSession, Database, points)];
                    }
                }

                return string.Empty;
            });
            Post("/points/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PointsEditViewModel>(ReadBody());
                var points = Database.Query<Points>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(points))
                {
                    if (status.HasAccess(points.Owner.Value, PartAccess.Points, AccessRight.Write))
                    {
                        status.AssignObjectIdString("Budget", points.Budget, model.Budget);
                        status.AssignDateString("MomentDate", points.Moment, model.MomentDate);
                        status.AddAssignTimeString("MomentTime", points.Moment, model.MomentTime);
                        status.AssignInt32String("Amount", points.Amount, model.Amount);
                        status.AssignStringRequired("Reason", points.Reason, model.Reason);
                        status.AssignStringFree("Url", points.Url, model.Url);

                        if (!string.IsNullOrEmpty(points.Url.Value) &&
                            !Regex.IsMatch(points.Url.Value, "^https(s)?://([a-zA-Z0-9\\-\\_]{2,256}\\.)+[a-zA-Z0-9\\-\\_]{2,256}(/[a-zA-Z0-9\\-\\_\\.\\?\\&\\=\\+\\*\\(\\)\\[\\]/,;'#@$]*)?$"))
                        {
                            status.SetValidationError("Url", "Points.Edit.Url.Invalid", "When the Url in the points edit dialog is not valid", "Invalid Url");
                        }

                        if (status.HasAccess(points.Budget.Value.Owner.Value, PartAccess.Points, AccessRight.Write))
                        {
                            points.Moment.Value = points.Moment.Value.ToUniversalTime();

                            if (status.IsSuccess)
                            {
                                Database.Save(points);
                                Journal(points.Owner.Value,
                                    "Points.Journal.Edit",
                                    "Journal entry edited points",
                                    "Change points {0}",
                                    t => points.GetText(t));
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/points/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Points, AccessRight.Write))
                    {
                        return View["View/pointsedit.sshtml",
                            new PointsEditViewModel(Translator, CurrentSession, Database, person)];
                    }
                }

                return string.Empty;
            });
            Post("/points/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PointsEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Points, AccessRight.Write))
                    {
                        var points = new Points(Guid.NewGuid());
                        status.AssignObjectIdString("Budget", points.Budget, model.Budget);
                        status.AssignDateString("MomentDate", points.Moment, model.MomentDate);
                        status.AddAssignTimeString("MomentTime", points.Moment, model.MomentTime);
                        status.AssignInt32String("Amount", points.Amount, model.Amount);
                        status.AssignStringRequired("Reason", points.Reason, model.Reason);
                        status.AssignStringFree("Url", points.Url, model.Url);
                        points.Owner.Value = person;

                        if (status.HasAccess(points.Budget.Value.Owner.Value, PartAccess.Points, AccessRight.Write))
                        {
                            points.Moment.Value = points.Moment.Value.ToUniversalTime();

                            if (status.IsSuccess)
                            {
                                Database.Save(points);
                                Journal(points.Owner.Value,
                                    "Points.Journal.Add",
                                    "Journal entry addded points",
                                    "Added points {0}",
                                    t => points.GetText(t));
                            }
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/points/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var points = Database.Query<Points>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(points))
                {
                    if (status.HasAccess(points.Owner.Value, PartAccess.Points, AccessRight.Write))
                    {
                        points.Delete(Database);
                        Journal(points.Owner.Value,
                            "Points.Journal.Delete",
                            "Journal entry removed points",
                            "Removed points {0}",
                            t => points.GetText(t));
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
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
        public string Moment;
        public string Amount;
        public string Reason;
        public List<NamedIdViewModel> Budgets;
        public string PhraseFieldBudget;
        public string PhraseFieldMoment;
        public string PhraseFieldAmount;
        public string PhraseFieldReason;

        public PointsEditViewModel()
        { 
        }

        public PointsEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Points.Edit.Title", "Title of the edit points dialog", "Edit points"), 
                   "pointsEditDialog")
        {
            PhraseFieldBudget = translator.Get("Points.Edit.Field.Budget", "Field 'Budget' in the edit points dialog", "Budget").EscapeHtml();
            PhraseFieldMoment = translator.Get("Points.Edit.Field.Moment", "Field 'Moment' in the edit points dialog", "Moment").EscapeHtml();
            PhraseFieldAmount = translator.Get("Points.Edit.Field.Amount", "Field 'Amount' in the edit points dialog", "Amount").EscapeHtml();
            PhraseFieldReason = translator.Get("Points.Edit.Field.Reason", "Field 'Reason' in the edit points dialog", "Reason").EscapeHtml();
            Budgets = new List<NamedIdViewModel>();
        }

        public PointsEditViewModel(Translator translator, Session session, IDatabase db, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            Budget = string.Empty;
            Moment = string.Empty;
            Amount = string.Empty;
            Reason = string.Empty;
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
            Moment = points.Moment.Value.ToString("dd.MM.yyyy");
            Amount = points.Amount.Value.ToString();
            Reason = points.Reason.Value;
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
                        status.AssignDateString("Moment", points.Moment, model.Moment);
                        status.AssignInt32String("Amount", points.Amount, model.Amount);
                        status.AssignStringRequired("Reason", points.Reason, model.Reason);

                        if (status.HasAccess(points.Budget.Value.Owner.Value, PartAccess.Points, AccessRight.Write))
                        {
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
                        status.AssignDateString("Moment", points.Moment, model.Moment);
                        status.AssignInt32String("Amount", points.Amount, model.Amount);
                        status.AssignStringRequired("Reason", points.Reason, model.Reason);
                        points.Owner.Value = person;

                        if (status.HasAccess(points.Budget.Value.Owner.Value, PartAccess.Points, AccessRight.Write))
                        {
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

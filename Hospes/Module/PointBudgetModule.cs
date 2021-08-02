using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using MimeKit;
using SiteLibrary;

namespace Hospes
{
    public class PointBudgetItemViewModel
    {
        public string Id;
        public string Indent;
        public string Width;
        public string Label;
        public string Percentage;
        public string TotalPoints;
        public string CurrentPoints;
        public string Type;
        public string Editable;
        public string Deletable;
        public string PhraseDeleteConfirmationQuestion;

        public PointBudgetItemViewModel(Translator translator, Session session, long totalPoints, Group group, IEnumerable<PointBudget> budgets)
        {
            Id = string.Empty;
            Indent = "0%";
            Width = "50%";
            Label = group.Name.Value[translator.Language];
            var totalShare = budgets.Sum(b => b.Share.Value);
            Percentage = string.Format("{0:0.00}", Math.Round(totalShare, 2)) + "%";
            TotalPoints = ((long)Math.Floor(totalPoints * totalShare / 100m)).ToString();
            CurrentPoints = budgets.Sum(b => b.CurrentPoints.Value).ToString();
            Type = string.Empty;
            Editable = string.Empty;
            Deletable = string.Empty;
            PhraseDeleteConfirmationQuestion = string.Empty;
        }

        public PointBudgetItemViewModel(Translator translator, Session session, long totalPoints, PointBudget budget)
        {
            Id = budget.Id.ToString();
            Indent = "10%";
            Width = "40%";
            Label = budget.Label.Value[translator.Language];
            Percentage = string.Format("{0:0.00}", Math.Round(budget.Share.Value, 2)) + "%";
            TotalPoints = ((long)Math.Floor(totalPoints * budget.Share.Value / 100m)).ToString();
            CurrentPoints = budget.CurrentPoints.Value.ToString();
            Type = "budget";
            Editable = session.HasAccess(budget.Owner.Value, PartAccess.PointBudget, AccessRight.Write) ? "editable" : string.Empty;
            Deletable = session.HasAccess(budget.Owner.Value, PartAccess.PointBudget, AccessRight.Write) ? "fas fa-trash-alt" : string.Empty;
            PhraseDeleteConfirmationQuestion = translator.Get("PointBudget.List.Delete.Confirm.Question", "Delete points budget confirmation question", "Do you really wish to delete point budget {0}?", budget.GetText(translator)).EscapeHtml();
        }

        public PointBudgetItemViewModel(Translator translator, Session session, long totalPoints, PointTransfer transfer)
        {
            Id = transfer.Id.ToString();
            Indent = "0%";
            Width = "50%";
            Label = transfer.Sink.Value.Organization.Value.Name.Value[translator.Language];
            Percentage = string.Format("{0:0.00}", Math.Round(transfer.Share.Value, 2)) + "%";
            TotalPoints = ((long)Math.Floor(totalPoints * transfer.Share.Value / 100m)).ToString();
            CurrentPoints = string.Empty;
            Type = "transfer";
            Editable = session.HasAccess(transfer.Source.Value.Organization.Value, PartAccess.PointBudget, AccessRight.Write) ? "editable" : string.Empty;
            Deletable = session.HasAccess(transfer.Source.Value.Organization.Value, PartAccess.PointBudget, AccessRight.Write) ? "fas fa-trash-alt" : string.Empty;
            PhraseDeleteConfirmationQuestion = translator.Get("PointTransfer.List.Delete.Confirm.Question", "Delete points transfer confirmation question", "Do you really wish to delete point transfer {0}?", transfer.GetText(translator)).EscapeHtml();
        }
    }

    public class PointBudgetListViewModel : MasterViewModel
    {
        public string Id;
        public List<PointBudgetItemViewModel> List;
        public string Text;
        public string PhraseHeaderPercentage;
        public string PhraseHeaderTotalPoints;
        public string PhraseHeaderCurrentPoints;
        public string Editable;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;

        public PointBudgetListViewModel(IDatabase database, Translator translator, Session session, BudgetPeriod period)
            : base(translator,
            translator.Get("PointBudget.List.Title", "Title of the point budget list page", "Points budget"),
            session)
        {
            Id = period.Id.ToString();
            Text = period.GetText(translator);
            Editable = session.HasAccess(period.Organization.Value, PartAccess.PointBudget, AccessRight.Write) ? "editable" : string.Empty;
            List = new List<PointBudgetItemViewModel>();

            foreach (var group in period.Organization.Value.Groups
                .OrderBy(g => g.Name.Value[translator.Language]))
            {
                var budgets = database.Query<PointBudget>(DC.Equal("periodid", period.Id.Value).And(DC.Equal("ownerid", group.Id.Value)));

                if (budgets.Any())
                {
                    List.Add(new PointBudgetItemViewModel(translator, session, period.TotalPoints.Value, group, budgets));

                    foreach (var budget in budgets.OrderBy(b => b.Label.Value[translator.Language]))
                    {
                        List.Add(new PointBudgetItemViewModel(translator, session, period.TotalPoints.Value, budget));
                    }
                }
            }

            var transfers = database.Query<PointTransfer>(DC.Equal("sourceid", period.Id.Value));

            foreach (var transfer in transfers
                .OrderBy(t => t.Sink.Value.Organization.Value.Name.Value[translator.Language]))
            {
                List.Add(new PointBudgetItemViewModel(translator, session, period.TotalPoints.Value, transfer));
            }

            PhraseHeaderPercentage = translator.Get("PointBudget.List.Header.Percentage", "Percentage header in the point budget list", "Share");
            PhraseHeaderTotalPoints = translator.Get("PointBudget.List.Header.TotalPoints", "Total points header in the point budget list", "Total");
            PhraseHeaderCurrentPoints = translator.Get("PointBudget.List.Header.CurrentPoints", "Current points header in the point budget list", "Used");
            PhraseDeleteConfirmationTitle = translator.Get("PointBudget.List.Delete.Confirm.Title", "Delete point budget confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("PointBudget.List.Delete.Confirm.Info", "Delete point budget confirmation info", "This will remove that point budget and all associated point assignments.").EscapeHtml();
        }
    }

    public class PointBudgetViewModel : MasterViewModel
    {
        public string DefaultId;
        public List<NamedIdViewModel> BudgetPeriods;

        public PointBudgetViewModel(IDatabase database, Translator translator, Session session, BudgetPeriod defaultPeriod)
            : base(translator,
            translator.Get("PointBudget.List.Title", "Title of the point budget list page", "Points budget"),
            session)
        {
            DefaultId = defaultPeriod.Id.ToString();
            BudgetPeriods = new List<NamedIdViewModel>(
                database.Query<BudgetPeriod>()
                .Where(p => session.HasAccess(p.Organization.Value, PartAccess.PointBudget, AccessRight.Read))
                .OrderByDescending(p => p.Organization.Value.Subordinates.Count())
                .ThenBy(p => p.GetText(translator))
                .Select(p => new NamedIdViewModel(translator, p, defaultPeriod == p)));
        }
    }

    public class PointBudgetEditDialogViewModel : DialogViewModel
    {
        public List<MultiItemViewModel> Label;
        public List<NamedIdViewModel> Owners;
        public string Method;
        public string Id;
        public string Share;
        public string Owner;
        public string PhraseFieldOwner;
        public string PhraseFieldShare;

        public PointBudgetEditDialogViewModel()
        {
        }

        public PointBudgetEditDialogViewModel(Translator translator)
            : base(translator,
            translator.Get("PointBudget.Edit.Title", "Title of the point budget edit dialog", "Edit points budget"),
            "editDialog")
        {
        }

        public PointBudgetEditDialogViewModel(IDatabase database, Translator translator, Session session, BudgetPeriod period)
            : this(translator)
        {
            Method = "add";
            Id = period.Id.ToString();
            Label = translator.CreateLanguagesMultiItem("PointBudget.Edit.Field.Label", "Field 'Label' in the edit point budget dialog", "Label ({0})", new MultiLanguageString());
            Owners = new List<NamedIdViewModel>(
                period.Organization.Value.Groups
                .Select(g => new NamedIdViewModel(translator, g, false)));
            Share = string.Empty;
            PhraseFieldOwner = translator.Get("PointBudget.Edit.Field.Owner", "Owner field in the point budget edit", "Owner");
            PhraseFieldShare = translator.Get("PointBudget.Edit.Field.Share", "Share field in the point budget edit", "Share");
        }

        public PointBudgetEditDialogViewModel(IDatabase database, Translator translator, Session session, PointBudget budget)
            : this(translator)
        {
            Method = "edit";
            Id = budget.Id.ToString();
            Label = translator.CreateLanguagesMultiItem("PointBudget.Edit.Field.Label", "Field 'Label' in the edit point budget dialog", "Label ({0})", budget.Label.Value);
            Owners = new List<NamedIdViewModel>(
                budget.Period.Value.Organization.Value.Groups
                .Select(g => new NamedIdViewModel(translator, g, budget.Owner.Value == g)));
            Share = budget.Share.Value.ToString();
            PhraseFieldOwner = translator.Get("PointBudget.Edit.Field.Owner", "Owner field in the point budget edit", "Owner");
            PhraseFieldShare = translator.Get("PointBudget.Edit.Field.Share", "Share field in the point budget edit", "Share");
        }
    }

    public class PointTransferEditDialogViewModel : DialogViewModel
    {
        public List<NamedIdViewModel> Sinks;
        public string Method;
        public string Id;
        public string Share;
        public string Sink;
        public string PhraseFieldSink;
        public string PhraseFieldShare;

        public PointTransferEditDialogViewModel()
        { 
        }

        public PointTransferEditDialogViewModel(Translator translator)
            : base(translator,
            translator.Get("PointTransfer.Edit.Title", "Title of the point transfer edit dialog", "Edit points transfer"),
            "editDialog")
        {
            PhraseFieldSink = translator.Get("PointTransfer.Edit.Field.Sink", "Sink field in the point transfer edit", "Target");
            PhraseFieldShare = translator.Get("PointTransfer.Edit.Field.Share", "Share field in the point transfer edit", "Share");
        }

        public PointTransferEditDialogViewModel(IDatabase database, Translator translator, Session session, BudgetPeriod source)
            : this(translator)
        {
            Method = "add";
            Id = source.Id.ToString();
            Sinks = new List<NamedIdViewModel>(database
                .Query<BudgetPeriod>()
                .Where(p => p.Organization.Value != source.Organization.Value &&
                            Dates.ComputeOverlap(p.StartDate.Value, p.EndDate.Value, source.StartDate.Value, source.EndDate.Value).TotalDays >= 1d)
                .OrderByDescending(p => p.Organization.Value.Subordinates.Count())
                .ThenBy(p => p.GetText(translator))
                .Select(p => new NamedIdViewModel(translator, p, false)));
            Share = string.Empty;
        }

        public PointTransferEditDialogViewModel(IDatabase database, Translator translator, Session session, PointTransfer transfer)
            : this(translator)
        {
            Method = "edit";
            Id = transfer.Id.ToString();
            var source = transfer.Source.Value;
            Sinks = new List<NamedIdViewModel>(database
                .Query<BudgetPeriod>()
                .Where(p => p.Organization.Value != source.Organization.Value &&
                            Dates.ComputeOverlap(p.StartDate.Value, p.EndDate.Value, source.StartDate.Value, source.EndDate.Value).TotalDays >= 1d)
                .OrderByDescending(p => p.Organization.Value.Subordinates.Count())
                .ThenBy(p => p.GetText(translator))
                .Select(p => new NamedIdViewModel(translator, p, transfer.Sink.Value == p)));
            Share = transfer.Share.Value.ToString();
        }
    }

    public class PointBudgetModule : QuaesturModule
    {
        public PointBudgetModule()
        {
            RequireCompleteLogin();

            Get("/points/budget", parameters =>
            {
                var organization = Database.Query<Organization>()
                    .Where(o => CurrentSession.HasAccess(o, PartAccess.PointBudget, AccessRight.Read))
                    .OrderByDescending(o => o.Subordinates.Count())
                    .FirstOrDefault();

                if (organization != null)
                {
                    var period = Database.Query<BudgetPeriod>(DC.Equal("organizationid", organization.Id.Value))
                        .OrderBy(p => Math.Abs(DateTime.UtcNow.Date.Subtract(p.StartDate.Value).TotalDays) +
                                      Math.Abs(DateTime.UtcNow.Date.Subtract(p.EndDate.Value).TotalDays))
                        .FirstOrDefault();

                    if (period != null)
                    {
                        return View["View/pointbudget.sshtml",
                            new PointBudgetViewModel(Database, Translator, CurrentSession, period)];
                    }
                    else
                    {
                        return AccessDenied();
                    }
                }
                else
                {
                    return AccessDenied();
                }
            });
            Get("/points/budget/list/{id}", parameters =>
            {
                string idString = parameters.id;
                var period = Database.Query<BudgetPeriod>(idString);

                if (period != null)
                {
                    if (CurrentSession.HasAccess(period.Organization.Value, PartAccess.PointBudget, AccessRight.Read))
                    {
                        return View["View/pointbudgetlist.sshtml",
                            new PointBudgetListViewModel(Database, Translator, CurrentSession, period)];
                    }
                }

                return string.Empty;
            });
            Get("/points/budget/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var period = Database.Query<BudgetPeriod>(idString);

                if (period != null)
                {
                    if (CurrentSession.HasAccess(period.Organization.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        return View["View/pointbudgetedit.sshtml",
                            new PointBudgetEditDialogViewModel(Database, Translator, CurrentSession, period)];
                    }
                }

                return string.Empty;
            });
            Post("/points/budget/add/{id}", parameters =>
            {
                var status = CreateStatus();
                string idString = parameters.id;
                var period = Database.Query<BudgetPeriod>(idString);

                if (status.ObjectNotNull(period))
                {
                    if (status.HasAccess(period.Organization.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<PointBudgetEditDialogViewModel>(ReadBody());
                        var budget = new PointBudget(Guid.NewGuid());
                        budget.Period.Value = period;
                        status.AssignMultiLanguageRequired("Label", budget.Label, model.Label);
                        status.AssignObjectIdString("Owner", budget.Owner, model.Owner);
                        status.AssignDecimalString("Share", budget.Share, model.Share);

                        if (status.IsSuccess)
                        {
                            Database.Save(budget);
                            Notice("{0} added budget {1} to {2}", CurrentSession.User.ShortHand, budget.GetText(Translator), period.GetText(Translator));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/points/budget/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var budget = Database.Query<PointBudget>(idString);

                if (budget != null)
                {
                    if (CurrentSession.HasAccess(budget.Owner.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        return View["View/pointbudgetedit.sshtml",
                            new PointBudgetEditDialogViewModel(Database, Translator, CurrentSession, budget)];
                    }
                }

                return string.Empty;
            });
            Post("/points/budget/edit/{id}", parameters =>
            {
                var status = CreateStatus();
                string idString = parameters.id;
                var budget = Database.Query<PointBudget>(idString);

                if (status.ObjectNotNull(budget))
                {
                    if (status.HasAccess(budget.Owner.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<PointBudgetEditDialogViewModel>(ReadBody());
                        status.AssignMultiLanguageRequired("Label", budget.Label, model.Label);
                        status.AssignObjectIdString("Owner", budget.Owner, model.Owner);
                        status.AssignDecimalString("Share", budget.Share, model.Share);

                        if (status.IsSuccess)
                        {
                            Database.Save(budget);
                            Notice("{0} updated budget {1} in {2}", CurrentSession.User.ShortHand, budget.GetText(Translator), budget.GetText(Translator));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/points/budget/delete/{id}", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var budget = Database.Query<PointBudget>(idString);

                if (status.ObjectNotNull(budget))
                {
                    if (status.HasAccess(budget.Owner.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            budget.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted budget {1} in {2}", CurrentSession.User.ShortHand, budget.GetText(Translator), budget.GetText(Translator));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/points/transfer/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var period = Database.Query<BudgetPeriod>(idString);

                if (period != null)
                {
                    if (CurrentSession.HasAccess(period.Organization.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        return View["View/pointtransferedit.sshtml",
                            new PointTransferEditDialogViewModel(Database, Translator, CurrentSession, period)];
                    }
                }

                return string.Empty;
            });
            Post("/points/transfer/add/{id}", parameters =>
            {
                var status = CreateStatus();
                string idString = parameters.id;
                var period = Database.Query<BudgetPeriod>(idString);

                if (status.ObjectNotNull(period))
                {
                    if (status.HasAccess(period.Organization.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<PointTransferEditDialogViewModel>(ReadBody());
                        var transfer = new PointTransfer(Guid.NewGuid());
                        transfer.Source.Value = period;
                        status.AssignObjectIdString("Sink", transfer.Sink, model.Sink);
                        status.AssignDecimalString("Share", transfer.Share, model.Share);

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(transfer);
                                transfer.Sink.Value.UpdateTotalPoints(Database);
                                Database.Save(transfer.Sink.Value);
                                transaction.Commit();
                            }
                            Notice("{0} added budget {1} to {2}", CurrentSession.User.ShortHand, transfer.GetText(Translator), period.GetText(Translator));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/points/transfer/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var transfer = Database.Query<PointTransfer>(idString);

                if (transfer != null)
                {
                    if (CurrentSession.HasAccess(transfer.Source.Value.Organization.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        return View["View/pointtransferedit.sshtml",
                            new PointTransferEditDialogViewModel(Database, Translator, CurrentSession, transfer)];
                    }
                }

                return string.Empty;
            });
            Post("/points/transfer/edit/{id}", parameters =>
            {
                var status = CreateStatus();
                string idString = parameters.id;
                var transfer = Database.Query<PointTransfer>(idString);

                if (status.ObjectNotNull(transfer))
                {
                    if (status.HasAccess(transfer.Source.Value.Organization.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        var model = JsonConvert.DeserializeObject<PointTransferEditDialogViewModel>(ReadBody());
                        status.AssignObjectIdString("Sink", transfer.Sink, model.Sink);
                        status.AssignDecimalString("Share", transfer.Share, model.Share);

                        if (status.IsSuccess)
                        {
                            using (var transaction = Database.BeginTransaction())
                            {
                                Database.Save(transfer);
                                transfer.Sink.Value.UpdateTotalPoints(Database);
                                Database.Save(transfer.Sink.Value);
                                transaction.Commit();
                            }
                            Notice("{0} updated budget {1} in {2}", CurrentSession.User.ShortHand, transfer.GetText(Translator), transfer.GetText(Translator));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/points/transfer/delete/{id}", parameters =>
            {
                var status = CreateStatus();

                string idString = parameters.id;
                var transfer = Database.Query<PointTransfer>(idString);

                if (status.ObjectNotNull(transfer))
                {
                    if (status.HasAccess(transfer.Source.Value.Organization.Value, PartAccess.PointBudget, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            transfer.Delete(Database);
                            transfer.Sink.Value.UpdateTotalPoints(Database);
                            Database.Save(transfer.Sink.Value);
                            transaction.Commit();
                            Notice("{0} deleted budget {1} in {2}", CurrentSession.User.ShortHand, transfer.GetText(Translator), transfer.GetText(Translator));
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Newtonsoft.Json;
using BaseLibrary;
using SiteLibrary;

namespace Hospes
{
    public class PointsTallyEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string FromDate;
        public string UntilDate;
        public string CreatedDate;
        public string Considered;
        public string ForwardBalance;
        public string FileName;
        public string FileSize;
        public string FileData;
        public string PhraseFieldCreatedDate;
        public string PhraseFieldFromDate;
        public string PhraseFieldUntilDate;
        public string PhraseFieldConsidered;
        public string PhraseFieldForwardBalance;
        public string PhraseFieldDocument;

        public PointsTallyEditViewModel()
        { 
        }

        public PointsTallyEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("PointsTally.Edit.Title", "Title of the edit pointsTally dialog", "Edit points tally"), 
                   "pointsTallyEditDialog")
        {
            PhraseFieldCreatedDate = translator.Get("PointsTally.Edit.Field.CreatedDate", "Field 'CreatedDate' in the edit pointsTally dialog", "Created date").EscapeHtml();
            PhraseFieldFromDate = translator.Get("PointsTally.Edit.Field.FromDate", "Field 'From date' in the edit pointsTally dialog", "From date").EscapeHtml();
            PhraseFieldUntilDate = translator.Get("PointsTally.Edit.Field.UntilDate", "Field 'Until date' in the edit pointsTally dialog", "Until date").EscapeHtml();
            PhraseFieldConsidered = translator.Get("PointsTally.Edit.Field.Considered", "Field 'Considered' in the edit pointsTally dialog", "Considered").EscapeHtml();
            PhraseFieldForwardBalance = translator.Get("PointsTally.Edit.Field.ForwardBalance", "Field 'Forward balance' in the edit pointsTally dialog", "Forward balance").EscapeHtml();
            PhraseFieldDocument = translator.Get("PointsTally.Edit.Field.Document", "Field 'Document' in the edit pointsTally dialog", "Document").EscapeHtml();
        }

        public PointsTallyEditViewModel(Translator translator, IDatabase db, Session session, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            FromDate = string.Empty;
            UntilDate = string.Empty;
            CreatedDate = string.Empty;
            Considered = string.Empty;
            ForwardBalance = string.Empty;
            FileName = string.Empty;
            FileSize = string.Empty;
        }

        public PointsTallyEditViewModel(Translator translator, IDatabase db, Session session, PointsTally pointsTally)
            : this(translator)
        {
            Method = "edit";
            Id = pointsTally.Id.ToString();
            FromDate = pointsTally.FromDate.Value.FormatSwissDateDay();
            UntilDate = pointsTally.UntilDate.Value.FormatSwissDateDay();
            CreatedDate = pointsTally.CreatedDate.Value.FormatSwissDateDay();
            Considered = pointsTally.Considered.Value.ToString();
            ForwardBalance = pointsTally.ForwardBalance.Value.ToString();
            FileName = pointsTally.FileName(translator).EscapeHtml();
            FileSize = "(" + pointsTally.DocumentData.Value.Length.SizeFormat() + ")";
        }
    }

    public class PointsTallyEditModule : QuaesturModule
    {
        public PointsTallyEditModule()
        {
            RequireCompleteLogin();

            Get("/pointstally/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var pointsTally = Database.Query<PointsTally>(idString);

                if (pointsTally != null)
                {
                    if (HasAccess(pointsTally.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        return View["View/pointstallyedit.sshtml",
                            new PointsTallyEditViewModel(Translator, Database, CurrentSession, pointsTally)];
                    }
                }

                return string.Empty;
            });
            Post("/pointstally/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PointsTallyEditViewModel>(ReadBody());
                var pointsTally = Database.Query<PointsTally>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(pointsTally))
                {
                    if (status.HasAccess(pointsTally.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        status.AssignDateString("FromDate", pointsTally.FromDate, model.FromDate);
                        status.AssignDateString("UntilDate", pointsTally.UntilDate, model.UntilDate);
                        status.AssignDateString("CreatedDate", pointsTally.CreatedDate, model.CreatedDate);
                        status.AssignInt64String("Considered", pointsTally.Considered, model.Considered);
                        status.AssignInt64String("ForwardBalance", pointsTally.ForwardBalance, model.ForwardBalance);
                        status.AssingDataUrlString("DocumentData", pointsTally.DocumentData, null, model.FileData, false);

                        if (status.IsSuccess)
                        {
                            Database.Save(pointsTally);
                            Journal(pointsTally.Person.Value,
                                "PointsTally.Journal.Edit",
                                "Journal entry edited points tally",
                                "Changed points tally {0}",
                                t => pointsTally.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/pointstally/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Billing, AccessRight.Write))
                    {
                        return View["View/pointstallyedit.sshtml",
                            new PointsTallyEditViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
            Post("/pointstally/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PointsTallyEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Billing, AccessRight.Write))
                    {
                        var pointsTally = new PointsTally(Guid.NewGuid());
                        status.AssignDateString("FromDate", pointsTally.FromDate, model.FromDate);
                        status.AssignDateString("UntilDate", pointsTally.UntilDate, model.UntilDate);
                        status.AssignDateString("CreatedDate", pointsTally.CreatedDate, model.CreatedDate);
                        status.AssignInt64String("Considered", pointsTally.Considered, model.Considered);
                        status.AssignInt64String("ForwardBalance", pointsTally.ForwardBalance, model.ForwardBalance);
                        status.AssingDataUrlString("DocumentData", pointsTally.DocumentData, null, model.FileData, false);
                        pointsTally.Person.Value = person;

                        if (status.IsSuccess)
                        {
                            Database.Save(pointsTally);
                            Journal(pointsTally.Person.Value,
                                "PointsTally.Journal.Add",
                                "Journal entry added points tally",
                                "Added points tally {0}",
                                t => pointsTally.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/pointstally/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var pointsTally = Database.Query<PointsTally>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(pointsTally))
                {
                    if (status.HasAccess(pointsTally.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        pointsTally.Delete(Database);
                        Journal(pointsTally.Person.Value,
                            "PointsTally.Journal.Delete",
                            "Journal entry deleted points tally",
                            "Deleted points tally {0}",
                            t => pointsTally.GetText(t));
                    }
                }

                return status.CreateJsonData();
            });
            Get("/pointstally/download/{id}", parameters =>
            {
                string idString = parameters.id;
                var pointsTally = Database.Query<PointsTally>(idString);

                if (pointsTally != null)
                {
                    if (HasAccess(pointsTally.Person.Value, PartAccess.Billing, AccessRight.Read))
                    {
                        var stream = new MemoryStream(pointsTally.DocumentData);
                        var response = new StreamResponse(() => stream, "application/pdf");
                        Journal(pointsTally.Person.Value,
                            "PointsTally.Journal.Download",
                            "Journal entry downloaded points tally",
                            "Downloaded points tally {0}",
                            t => pointsTally.GetText(t));
                        return response.AsAttachment(pointsTally.FileName(Translator));
                    }
                }

                return string.Empty;
            });
        }
    }
}

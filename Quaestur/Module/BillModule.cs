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

namespace Quaestur
{
    public class BillEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Number;
        public string Membership;
        public string Status;
        public string FromDate;
        public string UntilDate;
        public string Amount;
        public string CreatedDate;
        public string PayedDate;
        public string FileName;
        public string FileSize;
        public string FileData;
        public List<NamedIdViewModel> Memberships;
        public List<NamedIntViewModel> Statuses;
        public string PhraseFieldNumber;
        public string PhraseFieldMembership;
        public string PhraseFieldStatus;
        public string PhraseFieldCreatedDate;
        public string PhraseFieldAmount;
        public string PhraseFieldFromDate;
        public string PhraseFieldUntilDate;
        public string PhraseFieldPayedDate;
        public string PhraseFieldDocument;

        public BillEditViewModel()
        { 
        }

        public BillEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Bill.Edit.Title", "Title of the edit bill dialog", "Edit bill"), 
                   "billEditDialog")
        {
            PhraseFieldNumber = translator.Get("Bill.Edit.Field.Number", "Field 'Number' in the edit bill dialog", "Number").EscapeHtml();
            PhraseFieldMembership = translator.Get("Bill.Edit.Field.Membership", "Field 'Membership' in the edit bill dialog", "Membership").EscapeHtml();
            PhraseFieldStatus = translator.Get("Bill.Edit.Field.Status", "Field 'Status' in the edit bill dialog", "Status").EscapeHtml();
            PhraseFieldCreatedDate = translator.Get("Bill.Edit.Field.CreatedDate", "Field 'CreatedDate' in the edit bill dialog", "Created date").EscapeHtml();
            PhraseFieldFromDate = translator.Get("Bill.Edit.Field.FromDate", "Field 'From date' in the edit bill dialog", "From date").EscapeHtml();
            PhraseFieldUntilDate = translator.Get("Bill.Edit.Field.UntilDate", "Field 'Until date' in the edit bill dialog", "Until date").EscapeHtml();
            PhraseFieldPayedDate = translator.Get("Bill.Edit.Field.PayedDate", "Field 'Payed date' in the edit bill dialog", "Payed date").EscapeHtml();
            PhraseFieldDocument = translator.Get("Bill.Edit.Field.Document", "Field 'Document' in the edit bill dialog", "Document").EscapeHtml();
            PhraseFieldAmount = translator.Get("Bill.Edit.Field.Amount", "Field 'Amount' in the edit bill dialog", "Amount").EscapeHtml();
        }

        public BillEditViewModel(Translator translator, IDatabase db, Session session, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            Number = string.Empty;
            Membership = string.Empty;
            Status = string.Empty;
            FromDate = string.Empty;
            UntilDate = string.Empty;
            Amount = string.Empty;
            CreatedDate = string.Empty;
            PayedDate = string.Empty;
            FileName = string.Empty;
            FileSize = string.Empty;
            Statuses = new List<NamedIntViewModel>();
            Statuses.Add(new NamedIntViewModel(translator, BillStatus.New, false));
            Statuses.Add(new NamedIntViewModel(translator, BillStatus.Payed, false));
            Statuses.Add(new NamedIntViewModel(translator, BillStatus.Canceled, false));
            Memberships = new List<NamedIdViewModel>(person.Memberships
                .Select(m => new NamedIdViewModel(translator, m, false))
                .OrderBy(m => m.Name));
        }

        public BillEditViewModel(Translator translator, IDatabase db, Session session, Bill bill)
            : this(translator)
        {
            Method = "edit";
            Id = bill.Id.ToString();
            Number = bill.Number.Value.EscapeHtml();
            Membership = bill.Membership.Value.Id.Value.ToString();
            Status = ((int)bill.Status.Value).ToString();
            FromDate = bill.FromDate.Value.ToString("dd.MM.yyyy");
            UntilDate = bill.UntilDate.Value.ToString("dd.MM.yyyy");
            Amount = bill.Amount.Value.ToString();
            CreatedDate = bill.CreatedDate.Value.ToString("dd.MM.yyyy");
            PayedDate = bill.PayedDate.Value.HasValue ? bill.PayedDate.Value.Value.ToString("dd.MM.yyyy") : string.Empty;
            FileName = bill.Number.Value.EscapeHtml() + ".pdf";
            FileSize = "(" + bill.DocumentData.Value.Length.SizeFormat() + ")";
            Statuses = new List<NamedIntViewModel>();
            Statuses.Add(new NamedIntViewModel(translator, BillStatus.New, bill.Status.Value == BillStatus.New));
            Statuses.Add(new NamedIntViewModel(translator, BillStatus.Payed, bill.Status.Value == BillStatus.Payed));
            Statuses.Add(new NamedIntViewModel(translator, BillStatus.Canceled, bill.Status.Value == BillStatus.Canceled));
            Memberships = new List<NamedIdViewModel>(bill.Membership.Value.Person.Value.Memberships
                .Select(m => new NamedIdViewModel(translator, m, bill.Membership.Value == m))
                .OrderBy(m => m.Name));
        }
    }

    public class BillModule : QuaesturModule
    {
        public BillModule()
        {
            RequireCompleteLogin();

            Get("/bill/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var bill = Database.Query<Bill>(idString);

                if (bill != null)
                {
                    if (HasAccess(bill.Membership.Value.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        return View["View/billedit.sshtml",
                            new BillEditViewModel(Translator, Database, CurrentSession, bill)];
                    }
                }

                return string.Empty;
            });
            Post("/bill/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<BillEditViewModel>(ReadBody());
                var bill = Database.Query<Bill>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(bill))
                {
                    if (status.HasAccess(bill.Membership.Value.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        status.AssignStringRequired("Number", bill.Number, model.Number);
                        status.AssignEnumIntString("Status", bill.Status, model.Status);
                        status.AssignDateString("FromDate", bill.FromDate, model.FromDate);
                        status.AssignDateString("UntilDate", bill.UntilDate, model.UntilDate);
                        status.AssignDateString("CreatedDate", bill.CreatedDate, model.CreatedDate);
                        status.AssignDateString("PayedDate", bill.PayedDate, model.PayedDate);
                        status.AssingDataUrlString("DocumentData", bill.DocumentData, null, model.FileData, false);
                        status.AssignDecimalString("Amount", bill.Amount, model.Amount);
                        status.AssignObjectIdString("Membership", bill.Membership, model.Membership);

                        if (status.IsSuccess)
                        {
                            bill.Membership.Value.UpdateVotingRight(Database);
                            Database.Save(bill);
                            Journal(bill.Membership.Value.Person.Value,
                                "Bill.Journal.Edit",
                                "Journal entry edited bill",
                                "Changed bill {0}",
                                t => bill.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/bill/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Billing, AccessRight.Write))
                    {
                        return View["View/billedit.sshtml",
                            new BillEditViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
            Post("/bill/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<BillEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Billing, AccessRight.Write))
                    {
                        var bill = new Bill(Guid.NewGuid());
                        status.AssignStringRequired("Number", bill.Number, model.Number);
                        status.AssignEnumIntString("Status", bill.Status, model.Status);
                        status.AssignDateString("FromDate", bill.FromDate, model.FromDate);
                        status.AssignDateString("UntilDate", bill.UntilDate, model.UntilDate);
                        status.AssignDateString("CreatedDate", bill.CreatedDate, model.CreatedDate);
                        status.AssignDateString("PayedDate", bill.PayedDate, model.PayedDate);
                        status.AssingDataUrlString("Document", bill.DocumentData, null, model.FileData, true);
                        status.AssignDecimalString("Amount", bill.Amount, model.Amount);
                        status.AssignObjectIdString("Membership", bill.Membership, model.Membership);

                        if (status.IsSuccess)
                        {
                            bill.Membership.Value.UpdateVotingRight(Database);
                            Database.Save(bill);
                            Journal(bill.Membership.Value.Person.Value,
                                "Bill.Journal.Add",
                                "Journal entry added bill",
                                "Added bill {0}",
                                t => bill.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/bill/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var bill = Database.Query<Bill>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(bill))
                {
                    if (status.HasAccess(bill.Membership.Value.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            bill.Delete(Database);

                            Journal(bill.Membership.Value.Person.Value,
                                "Bill.Journal.Delete",
                                "Journal entry deleted bill",
                                "Deleted bill {0}",
                                t => bill.GetText(t));

                            transaction.Commit();
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/bill/download/{id}", parameters =>
            {
                string idString = parameters.id;
                var bill = Database.Query<Bill>(idString);

                if (bill != null)
                {
                    if (HasAccess(bill.Membership.Value.Person.Value, PartAccess.Billing, AccessRight.Read))
                    {
                        var stream = new MemoryStream(bill.DocumentData);
                        var response = new StreamResponse(() => stream, "application/pdf");
                        Journal(bill.Membership.Value.Person.Value,
                            "Bill.Journal.Download",
                            "Journal entry downloaded bill",
                            "Downloaded bill {0}",
                            t => bill.GetText(t));
                        return response.AsAttachment(bill.Number.Value + ".pdf");
                    }
                }

                return string.Empty;
            });
        }
    }
}

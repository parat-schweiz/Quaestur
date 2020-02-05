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
    public class PrepaymentEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Moment;
        public string Amount;
        public string Reason;
        public string PhraseFieldMoment;
        public string PhraseFieldAmount;
        public string PhraseFieldReason;

        public PrepaymentEditViewModel()
        { 
        }

        public PrepaymentEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Prepayment.Edit.Title", "Title of the edit prepayment dialog", "Edit prepayment"), 
                   "prepaymentEditDialog")
        {
            PhraseFieldMoment = translator.Get("Prepayment.Edit.Field.Moment", "Field 'Moment' in the edit prepayment dialog", "Date").EscapeHtml();
            PhraseFieldAmount = translator.Get("Prepayment.Edit.Field.Amount", "Field 'Amount' in the edit prepayment dialog", "Amount").EscapeHtml();
            PhraseFieldReason = translator.Get("Prepayment.Edit.Field.Reason", "Field 'Reason' in the edit prepayment dialog", "Reason").EscapeHtml();
        }

        public PrepaymentEditViewModel(Translator translator, IDatabase db, Session session, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            Moment = string.Empty;
            Amount = string.Empty;
            Reason = string.Empty;
        }

        public PrepaymentEditViewModel(Translator translator, IDatabase db, Session session, Prepayment prepayment)
            : this(translator)
        {
            Method = "edit";
            Id = prepayment.Id.ToString();
            Moment = prepayment.Moment.Value.FormatSwissDateDay();
            Amount = prepayment.Amount.Value.FormatMoney();
            Reason = prepayment.Reason.Value.EscapeHtml();
        }
    }

    public class PrepaymentModule : QuaesturModule
    {
        public PrepaymentModule()
        {
            RequireCompleteLogin();

            Get("/prepayment/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var prepayment = Database.Query<Prepayment>(idString);

                if (prepayment != null)
                {
                    if (HasAccess(prepayment.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        return View["View/prepaymentedit.sshtml",
                            new PrepaymentEditViewModel(Translator, Database, CurrentSession, prepayment)];
                    }
                }

                return string.Empty;
            });
            Post("/prepayment/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PrepaymentEditViewModel>(ReadBody());
                var prepayment = Database.Query<Prepayment>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(prepayment))
                {
                    if (status.HasAccess(prepayment.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        status.AssignStringRequired("Reason", prepayment.Reason, model.Reason);
                        status.AssignDecimalString("Amount", prepayment.Amount, model.Amount);
                        status.AssignDateString("Moment", prepayment.Moment, model.Moment);

                        if (status.IsSuccess)
                        {
                            Database.Save(prepayment);
                            Journal(prepayment.Person.Value,
                                "Prepayment.Journal.Edit",
                                "Journal entry edited prepayment",
                                "Changed prepayment {0}",
                                t => prepayment.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/prepayment/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Billing, AccessRight.Write))
                    {
                        return View["View/prepaymentedit.sshtml",
                            new PrepaymentEditViewModel(Translator, Database, CurrentSession, person)];
                    }
                }

                return string.Empty;
            });
            Post("/prepayment/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PrepaymentEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Billing, AccessRight.Write))
                    {
                        var prepayment = new Prepayment(Guid.NewGuid());
                        prepayment.Person.Value = person;
                        status.AssignStringRequired("Reason", prepayment.Reason, model.Reason);
                        status.AssignDecimalString("Amount", prepayment.Amount, model.Amount);
                        status.AssignDateString("Moment", prepayment.Moment, model.Moment);

                        if (status.IsSuccess)
                        {
                            Database.Save(prepayment);
                            Journal(prepayment.Person.Value,
                                "Prepayment.Journal.Add",
                                "Journal entry added prepayment",
                                "Added prepayment {0}",
                                t => prepayment.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/prepayment/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var prepayment = Database.Query<Prepayment>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(prepayment))
                {
                    if (status.HasAccess(prepayment.Person.Value, PartAccess.Billing, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            prepayment.Delete(Database);

                            Journal(prepayment.Person.Value,
                                "Prepayment.Journal.Delete",
                                "Journal entry deleted prepayment",
                                "Deleted prepayment {0}",
                                t => prepayment.GetText(t));

                            transaction.Commit();
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

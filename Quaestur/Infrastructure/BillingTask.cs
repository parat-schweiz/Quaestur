using System;
using System.Linq;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;
using MimeKit;

namespace Quaestur
{
    public class BillingTask : ITask
    {
        private DateTime _lastSending;

        public BillingTask()
        {
            _lastSending = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                Global.Log.Notice("Running billing task");

                RunAll(database);

                Global.Log.Notice("Billing task complete");
            }
        }

        private static void Journal(IDatabase db, Membership membership, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, membership.Person.Value.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.Billing.Process", "Billing process naming", "Billing process");
            entry.Person.Value = membership.Person.Value;
            db.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        private void RunAll(IDatabase database)
        {
            var translation = new Translation(database);
            var memberships = database.Query<Membership>().ToList();

            foreach (var membership in memberships
                .Where(m => m.Type.Value.Collection.Value == CollectionModel.Direct &&
                            !m.Person.Value.Deleted.Value))
            {
                var translator = new Translator(translation, membership.Person.Value.Language.Value);
                var model = membership.Type.Value.CreatePaymentModel(database);
                var advancePeriod = model != null ? model.GetBillAdvancePeriod() : 30;

                var bills = database.Query<Bill>(DC.Equal("membershipid", membership.Id.Value)).ToList();

                if (bills.Count > 0)
                {
                    if (DateTime.Now.Date >= bills.Max(b => b.UntilDate.Value).AddDays(-advancePeriod).Date)
                    {
                        CreateBill(database, translation, membership);
                    }
                }
                else
                {
                    CreateBill(database, translation, membership);
                }
            }
        }

        public static bool CreateBill(IDatabase database, Translation translation, Membership membership)
        {
            try
            {
                Translator translator = new Translator(translation, membership.Person.Value.Language.Value);
                var billDocument = new BillDocument(translator, database, membership);

                if (billDocument.Create())
                {
                    using (var transaction = database.BeginTransaction())
                    {
                        database.Save(billDocument.Bill);
                        membership.UpdateVotingRight(database);
                        database.Save(membership);
                        Journal(
                            database,
                            membership,
                            "Document.Bill.Created",
                            "Bill created message",
                            "Created bill {0} for {1} in {2}",
                            t => billDocument.Bill.Number.Value,
                            t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                            t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);

                        transaction.Commit();
                    }

                    return true;
                }
                else if (billDocument.RequiresPersonalPaymentUpdate)
                {
                    Journal(
                        database,
                        membership,
                        "Document.Bill.RequiresPersonalPaymentUpdate",
                        "Cannot create bill because personal payment parameter update required",
                        "Cannot create bill {0} for {1} in {2} because an update of the personal payment parameter is required",
                        t => billDocument.Bill.Number.Value,
                        t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                        t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);
                    return false;
                }
                else if (billDocument.RequiresNewPointsTally)
                {
                    Journal(
                        database,
                        membership,
                        "Document.Bill.RequiresNewPointsTally",
                        "Cannot create bill because new points tally required",
                        "Cannot create bill {0} for {1} in {2} because a new points tally is required",
                        t => billDocument.Bill.Number.Value,
                        t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                        t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);
                    return false;
                }
                else
                {
                    Journal(
                        database,
                        membership,
                        "Document.Bill.Failed",
                        "Bill creation failed message",
                        "Creation of bill {0} for {1} in {2} failed",
                        t => billDocument.Bill.Number.Value,
                        t => billDocument.Bill.Membership.Value.Person.Value.ShortHand,
                        t => billDocument.Bill.Membership.Value.Organization.Value.Name.Value[t.Language]);
                    var texDocument = new TextAttachement(billDocument.TexDocument, "document.tex");
                    var errorDocument = new TextAttachement(billDocument.ErrorText, "error.txt");
                    Global.Mail.SendAdminEncrypted(
                        "LaTeX Error", "Could not compile bill document",
                        texDocument, errorDocument);
                    return false;
                }
            }
            catch (Exception e)
            {
                Global.Log.Error("Bill creation failed");
                Global.Log.Error(e.ToString());
                return false;
            }
        }
    }
}

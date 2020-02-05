using System;
using System.Linq;
using SiteLibrary;

namespace Quaestur
{
    public static class Model
    {
        public static int CurrentVersion = 28;

        public static void Install(IDatabase database)
        {
            CreateAllTables(database);
            Migrate(database);
            CheckPaymentParameters(database);
        }

        private static void CreateAllTables(IDatabase database)
        {
            Global.Log.Notice("Checking tables...");

            database.CreateTable<Meta>();
            database.CreateTable<Country>();
            database.CreateTable<State>();
            database.CreateTable<Person>();
            database.CreateTable<PostalAddress>();
            database.CreateTable<PublicKey>();
            database.CreateTable<ServiceAddress>();
            database.CreateTable<Organization>();
            database.CreateTable<Group>();
            database.CreateTable<Role>();
            database.CreateTable<Permission>();
            database.CreateTable<RoleAssignment>();
            database.CreateTable<MembershipType>();
            database.CreateTable<PaymentParameter>();
            database.CreateTable<Membership>();
            database.CreateTable<Tag>();
            database.CreateTable<TagAssignment>();
            database.CreateTable<MailingElement>();
            database.CreateTable<Mailing>();
            database.CreateTable<Sending>();
            database.CreateTable<Document>();
            database.CreateTable<Bill>();
            database.CreateTable<Prepayment>();
            database.CreateTable<BillSendingTemplate>();
            database.CreateTable<Export>();
            database.CreateTable<JournalEntry>();
            database.CreateTable<Phrase>();
            database.CreateTable<PhraseTranslation>();
            database.CreateTable<SystemWideSettings>();
            database.CreateTable<Oauth2Client>();
            database.CreateTable<Oauth2Session>();
            database.CreateTable<Oauth2Authorization>();
            database.CreateTable<SearchSettings>();
            database.CreateTable<BallotTemplate>();
            database.CreateTable<Ballot>();
            database.CreateTable<BallotPaper>();
            database.CreateTable<LoginLink>();
            database.CreateTable<PersonalPaymentParameter>();
            database.CreateTable<BudgetPeriod>();
            database.CreateTable<PointBudget>();
            database.CreateTable<Points>();
            database.CreateTable<PointsTally>();
            database.CreateTable<MailTemplate>();
            database.CreateTable<MailTemplateAssignment>();
            database.CreateTable<LatexTemplate>();
            database.CreateTable<LatexTemplateAssignment>();
            database.CreateTable<PointTransfer>();
            database.CreateTable<ApiClient>();
            database.CreateTable<ApiPermission>();
            database.CreateTable<Sequence>();
            database.CreateTable<ReservedUserName>();
            database.CreateTable<SystemWideFile>();

            Global.Log.Notice("Tables ok.");
        }

        private static void Migrate(IDatabase database)
        {
            Global.Log.Notice("Checking migrations...");

            var meta = database.Query<Meta>().SingleOrDefault();

            if (meta == null)
            {
                meta = new Meta(Guid.NewGuid());
                database.Save(meta);
            }

            while (meta.Version.Value < CurrentVersion)
            {
                Global.Log.Notice("Migrating to version {0}.", (meta.Version.Value + 1));

                using (var transaction = database.BeginTransaction())
                {
                    meta.Version.Value++;
                    Migrate(database, meta.Version.Value);
                    database.Save(meta);
                    transaction.Commit();
                }

                Global.Log.Notice("Migration applied.");
            }

            Global.Log.Notice("Migrations done.");
        }

        private static void Migrate(IDatabase database, int version)
        {
            switch (version)
            {
                case 23:
                    database.DropColumn<BallotTemplate>("announcement");
                    database.DropColumn<BallotTemplate>("invitation");
                    database.DropColumn<BallotTemplate>("ballotpaper");
                    database.DropColumn<MembershipType>("billtemplatelatex");
                    database.DropTable("sendingtemplate");
                    database.DropTable("sendingtemplatelanguage");
                    break;
                case 24:
                    database.AddColumn<Person>(p => p.PaymentParameterUpdateReminderDate);
                    database.AddColumn<Person>(p => p.PaymentParameterUpdateReminderLevel);
                    break;
                case 25:
                    database.AddColumn<Points>(p => p.Url);
                    break;
                case 26:
                    SetNextPersonNumber(database);
                    break;
                case 27:
                    database.AddColumn<MembershipType>(m => m.TriplePoints);
                    database.AddColumn<MembershipType>(m => m.DoublePoints);
                    break;
                case 28:
                    ChangeBillSendingTemplates(database);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void ChangeBillSendingTemplates(IDatabase database)
        {
            foreach (var billSendingTemplate in database.Query<BillSendingTemplate>())
            {
                if (!string.IsNullOrEmpty(billSendingTemplate.LetterLatex.Value))
                {
                    var letterTemplate = new LatexTemplate(Guid.NewGuid());
                    letterTemplate.Organization.Value = billSendingTemplate.MembershipType.Value.Organization.Value;
                    letterTemplate.Text.Value = billSendingTemplate.LetterLatex.Value;
                    letterTemplate.Language.Value = billSendingTemplate.Language.Value;
                    letterTemplate.Label.Value = billSendingTemplate.Name.Value;
                    letterTemplate.AssignmentType.Value = TemplateAssignmentType.BillSendingTemplate;
                    database.Save(letterTemplate);

                    var letterTemplateAssignment = new LatexTemplateAssignment(Guid.NewGuid());
                    letterTemplateAssignment.AssignedId.Value = billSendingTemplate.Id.Value;
                    letterTemplateAssignment.FieldName.Value = BillSendingTemplate.BillSendingLetterFieldName;
                    letterTemplateAssignment.AssignedType.Value = TemplateAssignmentType.BillSendingTemplate;
                    letterTemplateAssignment.Template.Value = letterTemplate;
                    database.Save(letterTemplateAssignment);

                    billSendingTemplate.LetterLatex.Value = String.Empty;
                    database.Save(billSendingTemplate);
                }

                if (!string.IsNullOrEmpty(billSendingTemplate.MailSubject.Value))
                {
                    var mailTemplate = new MailTemplate(Guid.NewGuid());
                    mailTemplate.Organization.Value = billSendingTemplate.MembershipType.Value.Organization.Value;
                    mailTemplate.Language.Value = billSendingTemplate.Language.Value;
                    mailTemplate.Label.Value = billSendingTemplate.Name.Value;
                    mailTemplate.AssignmentType.Value = TemplateAssignmentType.BillSendingTemplate;
                    mailTemplate.Subject.Value = billSendingTemplate.MailSubject.Value;
                    mailTemplate.HtmlText.Value = billSendingTemplate.MailHtmlText.Value;
                    mailTemplate.PlainText.Value = billSendingTemplate.MailPlainText.Value;
                    database.Save(mailTemplate);

                    var mailTemplateAssignment = new MailTemplateAssignment(Guid.NewGuid());
                    mailTemplateAssignment.AssignedId.Value = billSendingTemplate.Id.Value;
                    mailTemplateAssignment.FieldName.Value = BillSendingTemplate.BillSendingMailFieldName;
                    mailTemplateAssignment.AssignedType.Value = TemplateAssignmentType.BillSendingTemplate;
                    mailTemplateAssignment.Template.Value = mailTemplate;
                    database.Save(mailTemplateAssignment);

                    billSendingTemplate.MailSubject.Value = String.Empty;
                    billSendingTemplate.MailHtmlText.Value = String.Empty;
                    billSendingTemplate.MailPlainText.Value = String.Empty;
                    database.Save(billSendingTemplate);
                }
            }
        }

        private static void SetNextPersonNumber(IDatabase database)
        {
            var sequence = database.Query<Sequence>().SingleOrDefault();

            if (sequence == null)
            {
                sequence = new Sequence(Guid.NewGuid());
                sequence.NextPersonNumber.Value =
                    database.Query<Person>().Max(p => p.Number.Value) + 1;
                database.Save(sequence);
            }
        }

        private static void CheckPaymentParameters(IDatabase database)
        {
            foreach (var membershipType in database.Query<MembershipType>())
            {
                var model = membershipType.CreatePaymentModel(database);

                if (model != null)
                {
                    foreach (var parameterType in model.ParameterTypes)
                    {
                        if (!membershipType.PaymentParameters
                            .Any(p => p.Key.Value == parameterType.Key))
                        {
                            var newParameter = new PaymentParameter(Guid.NewGuid());
                            newParameter.Key.Value = parameterType.Key;
                            newParameter.Value.Value = parameterType.DefaultValue;
                            newParameter.Type.Value = membershipType;
                            database.Save(newParameter);
                        }
                    }

                    foreach (var parameter in membershipType.PaymentParameters)
                    {
                        if (!model.ParameterTypes.Any(
                            pt => pt.Key == parameter.Key))
                        {
                            parameter.Delete(database); 
                        } 
                    }
                }
            }
        }
    }
}

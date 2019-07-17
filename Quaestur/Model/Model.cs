using System;
using System.Linq;
using SiteLibrary;

namespace Quaestur
{
    public static class Model
    {
        public static int CurrentVersion = 20;

        public static void Install(IDatabase database)
        {
            CreateAllTables(database);
            Migrate(database);
            CheckPaymentParameters(database);
        }

        private static void CreateAllTables(IDatabase database)
        {
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
            database.CreateTable<SendingTemplate>();
            database.CreateTable<SendingTemplateLanguage>();
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
        }

        private static void Migrate(IDatabase database)
        {
            var meta = database.Query<Meta>().SingleOrDefault();

            if (meta == null)
            {
                meta = new Meta(Guid.NewGuid());
            }

            while (meta.Version.Value < CurrentVersion)
            {
                using (var transaction = database.BeginTransaction())
                {
                    meta.Version.Value++;
                    Migrate(database, meta.Version.Value);
                    database.Save(meta);
                    transaction.Commit();
                }
            }
        }

        private static void Migrate(IDatabase database, int version)
        {
            switch (version)
            {
                case 2:
                    database.AddColumn<BillSendingTemplate>(bst => bst.SendingMode);
                    break;
                case 3:
                    database.AddColumn<Person>(p => p.Deleted);
                    break;
                case 4:
                    database.AddColumn<Person>(p => p.TwoFactorSecret);
                    break;
                case 5:
                    database.AddColumn<Membership>(m => m.HasVotingRight);
                    break;
                case 6:
                    database.AddColumn<Oauth2Client>(c => c.RequireTwoFactor);
                    break;
                case 7:
                    database.AddColumn<Oauth2Client>(c => c.Access);
                    break;
                case 8:
                    database.AddColumn<Person>(p => p.PasswordType);
                    break;
                case 9:
                    UpdatePasswordTypes(database);
                    break;
                case 10:
                    SecureTotpSecrets(database);
                    break;
                case 11:
                    database.ModifyColumnType<Group>(g => g.GpgKeyPassphrase);
                    break;
                case 12:
                    EncryptGpgPassphrases(database);
                    break;
                case 13:
                    database.AddColumn<MembershipType>(m => m.MaximumPoints);
                    database.AddColumn<MembershipType>(m => m.MaximumDiscount);
                    break;
                case 14:
                    database.AddColumn<MembershipType>(m => m.Deprecated2);
                    break;
                case 15:
                    database.AddColumn<MembershipType>(m => m.MaximumBalanceForward);
                    break;
                case 16:
                    database.ModifyColumnType<BallotTemplate>(t => t.Deprecated1);
                    database.ModifyColumnType<BallotTemplate>(t => t.Deprecated2);
                    MigrateSendingTemplates(database);
                    break;
                case 17:
                    MigrateBallotPapers(database);
                    break;
                case 18:
                    database.AddColumn<MembershipType>(m => m.SenderGroup);
                    break;
                case 19:
                    MigrateMembershipType(database);
                    break;
                case 20:
                    database.AddColumn<MailTemplate>(t => t.Organization);
                    database.AddColumn<MailTemplate>(t => t.AssignmentType);
                    database.AddColumn<LatexTemplate>(t => t.Organization);
                    database.AddColumn<LatexTemplate>(t => t.AssignmentType);
                    MigrateTemplate(database);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void MigrateTemplate(IDatabase database)
        {
            foreach (var templateAssignment in database.Query<MailTemplateAssignment>())
            {
                var template = templateAssignment.Template.Value;
                template.Organization.Value =
                    templateAssignment.GetOrganization(database);
                template.AssignmentType.Value =
                    templateAssignment.AssignedType.Value;
                database.Save(template);
            }

            foreach (var templateAssignment in database.Query<LatexTemplateAssignment>())
            {
                var template = templateAssignment.Template.Value;
                template.Organization.Value =
                    templateAssignment.GetOrganization(database);
                template.AssignmentType.Value =
                    templateAssignment.AssignedType.Value;
                database.Save(template);
            }
        }

        private static void MigrateMembershipType(IDatabase database)
        {
            foreach (var membershipType in database.Query<MembershipType>())
            {
                foreach (var language in LanguageExtensions.Natural)
                {
                    var billDocumentValue = membershipType.Deprecated1.Value.GetValueOrEmpty(language);

                    if (!string.IsNullOrEmpty(billDocumentValue))
                    {
                        var translator = new Translator(new Translation(database), language);

                        var newTemplate = new LatexTemplate(Guid.NewGuid());
                        newTemplate.Language.Value = language;
                        newTemplate.Label.Value = membershipType.Name.Value[language] + " " + language.Translate(translator) + " bill document";
                        newTemplate.Text.Value = billDocumentValue;
                        database.Save(newTemplate);

                        var newAssignment = new LatexTemplateAssignment(Guid.NewGuid());
                        newAssignment.Template.Value = newTemplate;
                        newAssignment.AssignedId.Value = membershipType.Id.Value;
                        newAssignment.AssignedType.Value = TemplateAssignmentType.MembershipType;
                        newAssignment.FieldName.Value = MembershipType.BillLDocumentFieldName;
                        database.Save(newAssignment);
                    }

                    var pointsTallyDocumentValue = membershipType.Deprecated2.Value.GetValueOrEmpty(language);

                    if (!string.IsNullOrEmpty(billDocumentValue))
                    {
                        var translator = new Translator(new Translation(database), language);

                        var newTemplate = new LatexTemplate(Guid.NewGuid());
                        newTemplate.Language.Value = language;
                        newTemplate.Label.Value = membershipType.Name.Value[language] + " " + language.Translate(translator) + " points tally document";
                        newTemplate.Text.Value = pointsTallyDocumentValue;
                        database.Save(newTemplate);

                        var newAssignment = new LatexTemplateAssignment(Guid.NewGuid());
                        newAssignment.Template.Value = newTemplate;
                        newAssignment.AssignedId.Value = membershipType.Id.Value;
                        newAssignment.AssignedType.Value = TemplateAssignmentType.MembershipType;
                        newAssignment.FieldName.Value = MembershipType.PointsTallyDocumentFieldName;
                        database.Save(newAssignment);
                    }
                }
            }
        }

        private static void MigrateBallotPapers(IDatabase database)
        {
            foreach (var ballotTemplate in database.Query<BallotTemplate>())
            {
                foreach (var language in LanguageExtensions.Natural)
                {
                    var value = ballotTemplate.Deprecated3.Value.GetValueOrEmpty(language);

                    if (!string.IsNullOrEmpty(value))
                    {
                        var translator = new Translator(new Translation(database), language);

                        var newTemplate = new LatexTemplate(Guid.NewGuid());
                        newTemplate.Language.Value = language;
                        newTemplate.Label.Value = ballotTemplate.Name.Value[language] + " " + language.Translate(translator) + " ballot paper";
                        newTemplate.Text.Value = value;
                        database.Save(newTemplate);

                        var newAssignment = new LatexTemplateAssignment(Guid.NewGuid());
                        newAssignment.Template.Value = newTemplate;
                        newAssignment.AssignedId.Value = ballotTemplate.Id.Value;
                        newAssignment.AssignedType.Value = TemplateAssignmentType.BallotTemplate;
                        newAssignment.FieldName.Value = BallotTemplate.BallotPaperFieldName;
                        database.Save(newAssignment);
                    }
                }
            } 
        }

        private static TemplateAssignmentType ConvertAssingmentType(SendingTemplateParentType type)
        {
            switch (type)
            {
                case SendingTemplateParentType.BallotTemplate:
                    return TemplateAssignmentType.BallotTemplate;
                default:
                    throw new NotSupportedException();
            } 
        }

        private static string ConvertAssingmentFieldName(string fieldName)
        {
            switch (fieldName)
            {
                case "announcement":
                    return BallotTemplate.AnnouncementMailFieldName;
                case "invitation":
                    return BallotTemplate.InvitationMailFieldName;
                default:
                    throw new NotSupportedException();
            }
        }

        private static void MigrateSendingTemplates(IDatabase database)
        {
            foreach (var ballotTemplate in database.Query<BallotTemplate>())
            {
                ballotTemplate.Deprecated1.Value = null;
                ballotTemplate.Deprecated2.Value = null;
                database.Save(ballotTemplate);
            }

            foreach (var sendingTemplate in database.Query<SendingTemplate>())
            {
                foreach (var language in sendingTemplate.Languages)
                {
                    var template = new MailTemplate(Guid.NewGuid());
                    template.Language.Value = language.Language.Value;
                    template.Label.Value = language.MailSubject.Value;
                    template.Subject.Value = language.MailSubject.Value;
                    template.HtmlText.Value = language.MailHtmlText.Value;
                    template.PlainText.Value = language.MailPlainText.Value;
                    database.Save(template);

                    var assignment = new MailTemplateAssignment(Guid.NewGuid());
                    assignment.Template.Value = template;
                    assignment.AssignedType.Value = ConvertAssingmentType(sendingTemplate.ParentType.Value);
                    assignment.AssignedId.Value = sendingTemplate.ParentId.Value;
                    assignment.FieldName.Value = ConvertAssingmentFieldName(sendingTemplate.FieldName.Value);
                    database.Save(template);

                    database.Delete(language);
                }

                database.Delete(sendingTemplate);
            }
        }

        private static void EncryptGpgPassphrases(IDatabase database)
        {
            foreach (var group in database.Query<Group>())
            {
                var passphraseData = Global.Security.SecureGpgPassphrase(group.GpgKeyPassphrase.Value);
                group.GpgKeyPassphrase.Value = Convert.ToBase64String(passphraseData);
                database.Save(group);
            }
        }

        private static void SecureTotpSecrets(IDatabase database)
        {
            foreach (var person in database.Query<Person>())
            {
                if (person.TwoFactorSecret.Value != null)
                {
                    var totpData = Global.Security.SecureTotp(person.TwoFactorSecret.Value);
                    person.TwoFactorSecret.Value = totpData;
                    database.Save(person);
                }
            }
        }

        private static void UpdatePasswordTypes(IDatabase database)
        { 
            foreach (var person in database.Query<Person>())
            {
                if (person.PasswordType.Value == PasswordType.None &&
                    person.PasswordHash.Value != null)
                {
                    person.PasswordType.Value = PasswordType.Local;
                    database.Save(person);
                }
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

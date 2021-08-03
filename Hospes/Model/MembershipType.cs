using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public class MembershipType : DatabaseObject
    {
        public ForeignKeyField<Organization, MembershipType> Organization { get; private set; }
        public MultiLanguageStringField Name { get; private set; }
        public ForeignKeyField<Group, MembershipType> SenderGroup { get; private set; }

        public MembershipType() : this(Guid.Empty)
        {
        }

		public MembershipType(Guid id) : base(id)
        {
            Organization = new ForeignKeyField<Organization, MembershipType>(this, "organizationid", false, o => o.MembershipTypes);
            Name = new MultiLanguageStringField(this, "name");
            SenderGroup = new ForeignKeyField<Group, MembershipType>(this, "sendergroup", true, null);
        }

        public const string PointsTallyMailFieldName = "PointsTallyMails";
        public const string SettlementMailFieldName = "SettlementMails";
        public const string BillDocumentFieldName = "BillDocuments";
        public const string SettlementDocumentFieldName = "SettlementDocuments";
        public const string PointsTallyDocumentFieldName = "PointsTallyDocuments";
        public const string PaymentParameterUpdateRequiredMailFieldName = "PaymentParameterUpdateRequiredMails";
        public const string PaymentParameterUpdateInvitationMailFieldName = "PaymentParameterUpdateInvitationMails";

        public TemplateField PointsTallyMail
        {
            get { return new TemplateField(TemplateAssignmentType.MembershipType, Id.Value, PointsTallyMailFieldName); }
        }

        public TemplateField SettlementMail
        {
            get { return new TemplateField(TemplateAssignmentType.MembershipType, Id.Value, SettlementMailFieldName); }
        }

        public TemplateField BillDocument
        {
            get { return new TemplateField(TemplateAssignmentType.MembershipType, Id.Value, BillDocumentFieldName); }
        }

        public TemplateField SettlementDocument
        {
            get { return new TemplateField(TemplateAssignmentType.MembershipType, Id.Value, SettlementDocumentFieldName); }
        }

        public TemplateField PointsTallyDocument
        {
            get { return new TemplateField(TemplateAssignmentType.MembershipType, Id.Value, PointsTallyDocumentFieldName); }
        }

        public TemplateField PaymentParameterUpdateRequiredMail
        {
            get { return new TemplateField(TemplateAssignmentType.MembershipType, Id.Value, PaymentParameterUpdateRequiredMailFieldName); }
        }

        public TemplateField PaymentParameterUpdateInvitationMail
        {
            get { return new TemplateField(TemplateAssignmentType.MembershipType, Id.Value, PaymentParameterUpdateInvitationMailFieldName); }
        }

        public IEnumerable<MailTemplateAssignment> PointsTallyMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", PointsTallyMailFieldName)));
        }

        public IEnumerable<MailTemplateAssignment> SettlementMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", SettlementMailFieldName)));
        }

        public IEnumerable<LatexTemplateAssignment> BillDocuments(IDatabase database)
        {
            return database.Query<LatexTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", BillDocumentFieldName)));
        }

        public IEnumerable<LatexTemplateAssignment> SettlementDocuments(IDatabase database)
        {
            return database.Query<LatexTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", SettlementDocumentFieldName)));
        }

        public IEnumerable<LatexTemplateAssignment> PointsTallyDocuments(IDatabase database)
        {
            return database.Query<LatexTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", PointsTallyDocumentFieldName)));
        }

        public IEnumerable<MailTemplateAssignment> PaymentParameterUpdateRequiredMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", PaymentParameterUpdateRequiredMailFieldName)));
        }

        public IEnumerable<MailTemplateAssignment> PaymentParameterUpdateInvitationMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", PaymentParameterUpdateInvitationMailFieldName)));
        }

        public MailTemplate GetPointsTallyMail(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, PointsTallyMails);
        }

        public MailTemplate GetSettlementMail(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, SettlementMails);
        }

        public LatexTemplate GetBillDocument(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, BillDocuments);
        }

        public LatexTemplate GetSettlementDocument(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, SettlementDocuments);
        }

        public LatexTemplate GetPointsTallyDocument(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, PointsTallyDocuments);
        }

        public MailTemplate GetPaymentParameterUpdateRequiredMail(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, PaymentParameterUpdateRequiredMails);
        }

        public MailTemplate GetPaymentParameterUpdateInvitationMail(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, PaymentParameterUpdateInvitationMails);
        }

        public static string GetFieldNameTranslation(Translator translator, string fieldName)
        {
            switch (fieldName)
            {
                case PointsTallyMailFieldName:
                    return translator.Get("BallotTemplate.FieldName.PointsTallyMail", "Points tally mail field name of the ballot template", "Points tally mail");
                case BillDocumentFieldName:
                    return translator.Get("BallotTemplate.FieldName.BillDocument", "Bill document field name of the ballot template", "Bill document");
                case PointsTallyDocumentFieldName:
                    return translator.Get("BallotTemplate.FieldName.PointsTallyDocument", "Points tally document field name of the ballot template", "Points tally document");
                default:
                    throw new NotSupportedException();
            }
        }

        public override void Delete(IDatabase database)
        {
            foreach (var membership in database
                .Query<Membership>(DC.Equal("membershiptypeid", Id.Value))
                .ToList())
            {
                membership.Delete(database);
            }

            foreach (var template in database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value)))
            {
                template.Delete(database);
            }

            foreach (var template in database.Query<LatexTemplateAssignment>(DC.Equal("assignedid", Id.Value)))
            {
                template.Delete(database);
            }

            database.Delete(this);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

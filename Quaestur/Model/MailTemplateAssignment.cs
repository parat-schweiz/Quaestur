﻿using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class MailTemplateAssignment : DatabaseObject, ITemplateAssignment<MailTemplate>
    {
        public ForeignKeyField<MailTemplate, MailTemplateAssignment> Template { get; private set; }
        public EnumField<TemplateAssignmentType> AssignedType { get; private set; }
        public Field<Guid> AssignedId { get; private set; }
        public StringField FieldName { get; private set; }

        public MailTemplateAssignment() : this(Guid.Empty)
        {
        }

        public MailTemplateAssignment(Guid id) : base(id)
        {
            Template = new ForeignKeyField<MailTemplate, MailTemplateAssignment>(this, "templateid", false, null);
            AssignedType = new EnumField<TemplateAssignmentType>(this, "assignedtype", TemplateAssignmentType.BallotTemplate, TemplateAssignmentTypeExtensions.Translate);
            AssignedId = new Field<Guid>(this, "assignedid", Guid.Empty);
            FieldName = new StringField(this, "fieldname", 256, AllowStringType.SimpleText);
        }

        public Organization GetOrganization(IDatabase database)
        {
            var o = AssignedTo(database);

            if (o is BallotTemplate)
            {
                return ((BallotTemplate)o).Organizer.Value.Organization.Value;
            }
            else if (o is MembershipType)
            {
                return ((MembershipType)o).Organization.Value;
            }
            else if (o is BillSendingTemplate)
            {
                return ((BillSendingTemplate)o).MembershipType.Value.Organization.Value;
            }
            else if (o is Subscription)
            {
                return ((Subscription)o).Membership.Value.Organization.Value;
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return ToString();
        }

        public string FieldNameTranslation(Translator translator)
        {
            return translator.Get(
                "MailTemplateAssignment.FieldName." + FieldName.Value,
                FieldName.Value + " field in the mail template assignment",
                FieldName.Value);
        }

        public string GetText(IDatabase database, Translator translator)
        {
            return AssignedTo(database).GetText(translator) + "/" + FieldNameTranslation(translator);
        }

        public DatabaseObject AssignedTo(IDatabase database)
        {
            switch (AssignedType.Value)
            {
                case TemplateAssignmentType.BallotTemplate:
                    return database.Query<BallotTemplate>(AssignedId.Value);
                case TemplateAssignmentType.MembershipType:
                    return database.Query<MembershipType>(AssignedId.Value);
                case TemplateAssignmentType.BillSendingTemplate:
                    return database.Query<BillSendingTemplate>(AssignedId.Value);
                case TemplateAssignmentType.Subscription:
                    return database.Query<Subscription>(AssignedId.Value);
                default:
                    throw new NotSupportedException();
            }
        }

        public string TranslateFieldName(IDatabase database, Translator translator)
        {
            switch (AssignedType.Value)
            {
                case TemplateAssignmentType.BallotTemplate:
                    return BallotTemplate.GetFieldNameTranslation(translator, FieldName.Value);
                case TemplateAssignmentType.MembershipType:
                    return MembershipType.GetFieldNameTranslation(translator, FieldName.Value);
                default:
                    throw new NotSupportedException();
            }
        }

        public PartAccess AssignedPartAccess
        {
            get
            {
                switch (AssignedType.Value)
                {
                    case TemplateAssignmentType.BallotTemplate:
                        return PartAccess.Ballot;
                    case TemplateAssignmentType.MembershipType:
                        return PartAccess.Structure;
                    default:
                        throw new NotSupportedException();
                }
            }
        }

        MailTemplate ITemplateAssignment<MailTemplate>.Template
        {
            get { return Template.Value; }
            set { Template.Value = value; }
        }

        TemplateAssignmentType ITemplateAssignment<MailTemplate>.AssignedType
        {
            get { return AssignedType.Value; }
            set { AssignedType.Value = value; }
        }

        Guid ITemplateAssignment<MailTemplate>.AssignedId
        {
            get { return AssignedId.Value; }
            set { AssignedId.Value = value; }
        }

        string ITemplateAssignment<MailTemplate>.FieldName
        {
            get { return FieldName.Value; }
            set { FieldName.Value = value; }
        }

        public bool HasAccess(IDatabase database, Session session, AccessRight right)
        {
            switch (AssignedType.Value)
            {
                case TemplateAssignmentType.BallotTemplate:
                    var organization1 = (AssignedTo(database) as BallotTemplate).Organizer.Value.Organization.Value;
                    return session.HasAccess(organization1, AssignedPartAccess, right);
                case TemplateAssignmentType.MembershipType:
                    var organization2 = (AssignedTo(database) as MembershipType).Organization.Value;
                    return session.HasAccess(organization2, AssignedPartAccess, right);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

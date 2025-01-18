using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class Subscription : DatabaseObject
    {
        public MultiLanguageStringField Name { get; set; }
        public ForeignKeyField<MembershipType, Subscription> Membership { get; set; }
        public ForeignKeyField<Tag, Subscription> Tag { get; set; }
        public ForeignKeyField<Group, Subscription> SenderGroup { get; set; }

        public Subscription() : this(Guid.Empty)
        {
        }

        public const string SubscribePrePagesFieldName = "SubscribePrePages";
        public const string SubscribeMailsFieldName = "SubscribeMails";
        public const string SubscribePostPagesFieldName = "SubscribePostPages";
        public const string UnsubscribePrePagesFieldName = "UnsubscribePrePages";
        public const string UnsubscribePostPagesFieldName = "UnsubscribePostPages";
        public const string JoinPrePagesFieldName = "JoinPrePages";
        public const string JoinPagesFieldName = "JoinPages";
        public const string JoinPostPagesFieldName = "JoinPostPages";
        public const string JoinConfirmMailsFieldName = "JoinConfirmMails";
        public const string ConfirmMailPagesFieldName = "ConfirmMailPages";
        public const string PageHeadersFieldName = "PageHeader";
        public const string PageFootersFieldName = "PageFooter";
        public const string UpdatePagesFieldName = "UpdatePages";

        public PageTemplateAssignmentField SubscribePrePages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, SubscribePrePagesFieldName); }
        }

        public MailTemplateAssignmentField SubscribeMails
        {
            get { return new MailTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, SubscribeMailsFieldName); }
        }

        public PageTemplateAssignmentField SubscribePostPages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, SubscribePostPagesFieldName); }
        }

        public PageTemplateAssignmentField UnsubscribePrePages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, UnsubscribePrePagesFieldName); }
        }

        public PageTemplateAssignmentField UnsubscribePostPages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, UnsubscribePostPagesFieldName); }
        }

        public PageTemplateAssignmentField JoinPrePages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, JoinPrePagesFieldName); }
        }

        public PageTemplateAssignmentField JoinPages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, JoinPagesFieldName); }
        }

        public PageTemplateAssignmentField JoinPostPages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, JoinPostPagesFieldName); }
        }

        public MailTemplateAssignmentField JoinConfirmMails
        {
            get { return new MailTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, JoinConfirmMailsFieldName); }
        }

        public PageTemplateAssignmentField ConfirmMailPages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, ConfirmMailPagesFieldName); }
        }

        public PageTemplateAssignmentField UpdatePages
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, UpdatePagesFieldName); }
        }

        public PageTemplateAssignmentField PageHeaders
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, PageHeadersFieldName); }
        }

        public PageTemplateAssignmentField PageFooters
        {
            get { return new PageTemplateAssignmentField(TemplateAssignmentType.Subscription, Id.Value, PageFootersFieldName); }
        }

        public Subscription(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Membership = new ForeignKeyField<MembershipType, Subscription>(this, "membershiptypeid", false, null);
            Tag = new ForeignKeyField<Tag, Subscription>(this, "tagid", false, null);
            SenderGroup = new ForeignKeyField<Group, Subscription>(this, "sendergroupid", false, null);
        }

        public override void Delete(IDatabase database)
        {
            foreach (var template in database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value)))
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

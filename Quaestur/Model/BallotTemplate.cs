using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class BallotTemplate : DatabaseObject
    {
        public MultiLanguageStringField Name { get; private set; }
        public ForeignKeyField<Group, BallotTemplate> Organizer { get; private set; }
        public ForeignKeyField<Tag, BallotTemplate> ParticipantTag { get; private set; }

        [Obsolete("Replaced by explicit announcment date in each ballot")]
        public Field<int> PreparationDays { get; private set; }

        [Obsolete("Replaced by explicit start date in each ballot")]
        public Field<int> VotingDays { get; private set; }

        public BallotTemplate() : this(Guid.Empty)
        {
        }

        public BallotTemplate(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name", AllowStringType.SimpleText);
            Organizer = new ForeignKeyField<Group, BallotTemplate>(this, "organizerid", false, null);
            ParticipantTag = new ForeignKeyField<Tag, BallotTemplate>(this, "participanttagid", false, null);
            PreparationDays = new Field<int>(this, "preparationdays", 3);
            VotingDays = new Field<int>(this, "votingdays", 1);
        }

        public const string AnnouncementMailFieldName = "AnnouncementMails";
        public const string InvitationMailFieldName = "InvitationMails";
        public const string BallotPaperFieldName = "BallotPapers";

        public TemplateField AnnouncementMail
        {
            get
            {
                return new TemplateField(TemplateAssignmentType.BallotTemplate, Id.Value, AnnouncementMailFieldName);
            } 
        }

        public TemplateField InvitationMail
        {
            get
            {
                return new TemplateField(TemplateAssignmentType.BallotTemplate, Id.Value, InvitationMailFieldName);
            }
        }

        public TemplateField BallotPaper
        {
            get
            {
                return new TemplateField(TemplateAssignmentType.BallotTemplate, Id.Value, BallotPaperFieldName);
            }
        }

        public IEnumerable<MailTemplateAssignment> AnnouncementMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", AnnouncementMailFieldName)));
        }

        public IEnumerable<MailTemplateAssignment> InvitationMails(IDatabase database)
        {
            return database.Query<MailTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", InvitationMailFieldName)));
        }

        public IEnumerable<LatexTemplateAssignment> BallotPapers(IDatabase database)
        {
            return database.Query<LatexTemplateAssignment>(DC.Equal("assignedid", Id.Value).And(DC.Equal("fieldname", BallotPaperFieldName)));
        }

        public MailTemplate GetAnnouncementMail(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, AnnouncementMails);
        }

        public MailTemplate GetInvitationMail(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, InvitationMails);
        }

        public LatexTemplate GetBallotPaper(IDatabase database, Language language)
        {
            return TemplateUtil.GetItem(database, language, BallotPapers);
        }

        public static string GetFieldNameTranslation(Translator translator, string fieldName)
        { 
            switch (fieldName)
            {
                case AnnouncementMailFieldName:
                    return translator.Get("BallotTemplate.FieldName.AnnouncementMail", "Announcement mail field name of the ballot template", "Announcement mail");
                case InvitationMailFieldName:
                    return translator.Get("BallotTemplate.FieldName.InvitationMail", "Invitation mail field name of the ballot template", "Invitation mail");
                case BallotPaperFieldName:
                    return translator.Get("BallotTemplate.FieldName.BallotPaper", "Ballot paper field name of the ballot template", "Ballot paper");
                default:
                    throw new NotSupportedException();
            }
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var ballot in database.Query<Ballot>(DC.Equal("templateid", Id.Value)))
            {
                ballot.Delete(database); 
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

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

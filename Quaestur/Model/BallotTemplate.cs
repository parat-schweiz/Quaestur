using System;
using System.Collections.Generic;

namespace Quaestur
{
    public class BallotTemplate : DatabaseObject
    {
        public MultiLanguageStringField Name { get; private set; }
        public ForeignKeyField<Group, BallotTemplate> Organizer { get; private set; }
        public ForeignKeyField<Tag, BallotTemplate> ParticipantTag { get; private set; }
        public Field<int> PreparationDays { get; private set; }
        public Field<int> VotingDays { get; private set; }
        public MultiLanguageStringField AnnouncementMailSubject { get; private set; }
        public MultiLanguageStringField AnnouncementMailText { get; private set; }
        public MultiLanguageStringField AnnouncementLetter { get; private set; }
        public MultiLanguageStringField InvitationMailSubject { get; private set; }
        public MultiLanguageStringField InvitationMailText { get; private set; }
        public MultiLanguageStringField InvitationLetter { get; private set; }
        public MultiLanguageStringField VoterCard { get; private set; }
        public MultiLanguageStringField BallotPaper { get; private set; }

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
            AnnouncementMailSubject = new MultiLanguageStringField(this, "announcementmailsubject", AllowStringType.SimpleText);
            AnnouncementMailText = new MultiLanguageStringField(this, "announcementmailtext", AllowStringType.SafeHtml);
            AnnouncementLetter = new MultiLanguageStringField(this, "announcementletter", AllowStringType.SafeLatex);
            InvitationMailSubject = new MultiLanguageStringField(this, "invitationmailsubject", AllowStringType.SimpleText);
            InvitationMailText = new MultiLanguageStringField(this, "invitationmailtext", AllowStringType.SafeHtml);
            InvitationLetter = new MultiLanguageStringField(this, "invitationletter", AllowStringType.SafeLatex);
            VoterCard = new MultiLanguageStringField(this, "votercard", AllowStringType.SafeLatex);
            BallotPaper = new MultiLanguageStringField(this, "ballotpaper", AllowStringType.SafeLatex);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var ballot in database.Query<Ballot>(DC.Equal("templateid", Id)))
            {
                ballot.Delete(database); 
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

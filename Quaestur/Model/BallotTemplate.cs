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
        public ForeignKeyField<SendingTemplate, BallotTemplate> Announcement { get; private set; }
        public ForeignKeyField<SendingTemplate, BallotTemplate> Invitation { get; private set; }
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
            Announcement = new ForeignKeyField<SendingTemplate, BallotTemplate>(this, "announcement", false, null);
            Invitation = new ForeignKeyField<SendingTemplate, BallotTemplate>(this, "invitation", false, null);
            BallotPaper = new MultiLanguageStringField(this, "ballotpaper", AllowStringType.SafeLatex);
        }

        public static string GetFieldNameTranslation(Translator translator, string fieldName)
        { 
            switch (fieldName)
            {
                case "announcement":
                    return translator.Get("BallotTemplate.FieldName.Announcement", "Announcement field name of the ballot template", "Announcement");
                case "invitation":
                    return translator.Get("BallotTemplate.FieldName.Invitation", "Invitation field name of the ballot template", "Invitation");
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

            database.Delete(this);
            Announcement.Value.Delete(database);
            Invitation.Value.Delete(database);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

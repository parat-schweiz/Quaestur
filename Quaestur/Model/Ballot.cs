using System;
using System.Collections.Generic;

namespace Quaestur
{
    public enum BallotStatus
    {
        New = 0,
        Announcing = 1,
        Voting = 2,
        Finished = 3,
        Canceled = 4,
    }

    public static class BallotStatusExtensions
    {
        public static string Translate(this BallotStatus status, Translator translator)
        {
            switch (status)
            {
                case BallotStatus.New:
                    return translator.Get("Enum.BallotStatus.New", "Value 'New' in BallotStatus enum", "New");
                case BallotStatus.Announcing:
                    return translator.Get("Enum.BallotStatus.Announcing", "Value 'Announcing' in BallotStatus enum", "Announcing");
                case BallotStatus.Voting:
                    return translator.Get("Enum.BallotStatus.Voting", "Value 'Voting' in BallotStatus enum", "Voting");
                case BallotStatus.Finished:
                    return translator.Get("Enum.BallotStatus.Finished", "Value 'Finished' in BallotStatus enum", "Finished");
                case BallotStatus.Canceled:
                    return translator.Get("Enum.BallotStatus.Canceled", "Value 'Canceled' in BallotStatus enum", "Canceled");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Ballot : DatabaseObject
    {
        public ForeignKeyField<BallotTemplate, Ballot> Template { get; private set; }
        public EnumField<BallotStatus> Status { get; private set; }
        public Field<DateTime> EndDate { get; private set; }
        public MultiLanguageStringField AnnouncementText { get; private set; }
        public MultiLanguageStringField Questions { get; private set; }

        public DateTime AnnouncementDate
        {
            get
            {
                return EndDate.Value.AddDays(1 - Template.Value.VotingDays.Value - Template.Value.PreparationDays.Value);
            }
        }

        public DateTime StartDate
        {
            get
            {
                return EndDate.Value.AddDays(1 - Template.Value.VotingDays.Value);
            }
        }

        public Ballot() : this(Guid.Empty)
        {
        }

        public Ballot(Guid id) : base(id)
        {
            Template = new ForeignKeyField<BallotTemplate, Ballot>(this, "templateid", false, null);
            Status = new EnumField<BallotStatus>(this, "status", BallotStatus.New, BallotStatusExtensions.Translate);
            EndDate = new Field<DateTime>(this, "enddate", new DateTime(1850, 1, 3));
            AnnouncementText = new MultiLanguageStringField(this, "announcementtext", AllowStringType.SafeHtml);
            Questions = new MultiLanguageStringField(this, "questions", AllowStringType.SafeLatex);
        }

        public override string ToString()
        {
            return "Ballot of " + EndDate.Value.ToShortDateString();
        }

        public override void Delete(IDatabase database)
        {
            foreach (var ballotPaper in database.Query<BallotPaper>(DC.Equal("ballotid", Id.Value)))
            {
                ballotPaper.Delete(database); 
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return translator.Get(
                "Ballot.Text", 
                "Text designation of a ballot", 
                "Ballot of {0}", 
                translator.FormatLongDate(EndDate.Value));
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public class Article : DatabaseObject
    {
        public ForeignKeyField<Ticket, Article> Ticket { get; private set; }
        public Field<DateTime> SentDate { get; private set; }
        public Field<DateTime> ReceivedDate { get; private set; }
        public StringField Subject { get; private set; }
        public StringField Text { get; private set; }
        public ByteArrayField Data { get; private set; }
        public List<Attachement> Attachements { get; private set; }
        public List<Participant> Participants { get; private set; }

        public IEnumerable<Participant> From
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.From); }
        }

        public IEnumerable<Participant> To
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.To); }
        }

        public IEnumerable<Participant> CC
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.CC); }
        }

        public IEnumerable<Participant> BCC
        {
            get { return Participants.Where(p => p.Type.Value == ParticipantType.BCC); }
        }

        public Article() : this(Guid.Empty)
        {
        }

        public Article(Guid id) : base(id)
        {
            Ticket = new ForeignKeyField<Ticket, Article>(this, "ticketid", false, t => t.Articles);
            SentDate = new Field<DateTime>(this, "sentdate", new DateTime(1970, 1, 1));
            ReceivedDate = new Field<DateTime>(this, "receiveddate", new DateTime(1970, 1, 1));
            Text = new StringField(this, "text", 262144, AllowStringType.SafeHtml);
            Subject = new StringField(this, "subject", 1024, AllowStringType.SimpleText);
            Data = new ByteArrayField(this, "data", true);
            Attachements = new List<Attachement>();
            Participants = new List<Participant>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Attachement>("articleid", Id.Value, () => Attachements);
                yield return new MultiCascade<Participant>("articleid", Id.Value, () => Participants);
            }
        }

        public override string ToString()
        {
            return Subject.Value;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var address in database.Query<Attachement>(DC.Equal("articleid", Id.Value)))
            {
                address.Delete(database);
            }

            foreach (var address in database.Query<Participant>(DC.Equal("articleid", Id.Value)))
            {
                address.Delete(database);
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Subject.Value;
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace RedmineEngagement
{
    public class Issue : DatabaseObject
    {
        public Field<int> IssueId { get; private set; }
        public DateTimeField CreatedOn { get; private set; }
        public DateTimeField UpdatedOn { get; private set; }
        public List<Assignment> Assignments { get; private set; }

        public Issue() : this(Guid.Empty)
        {
        }

        public Issue(Guid id) : base(id)
        {
            IssueId = new Field<int>(this, "issueid", 0);
            CreatedOn = new DateTimeField(this, "created", new DateTime(1850, 1, 1));
            UpdatedOn = new DateTimeField(this, "updated", new DateTime(1850, 1, 1));
            Assignments = new List<Assignment>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get 
            {
                yield return new MultiCascade<Assignment>("issueid", Id.Value, () => Assignments);
            } 
        }

        public override string ToString()
        {
            return "Issue " + Id.ToString();
        }

        public override string GetText(Translator translator)
        {
            return Id.ToString();
        }

        public override void Delete(IDatabase database)
        {
            foreach (var assignment in database.Query<Assignment>(DC.Equal("issueid", Id.Value)))
            {
                database.Delete(assignment);
            }

            database.Delete(this); 
        }
    }
}

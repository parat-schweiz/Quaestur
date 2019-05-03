using System;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public class Feed : DatabaseObject
    {
		public MultiLanguageStringField Name { get; private set; }
        public ForeignKeyField<Feed, Feed> Parent { get; private set; }
        public List<Feed> Children { get; private set; }
        public List<Group> Groups { get; private set; }

        public Feed() : this(Guid.Empty)
        {
        }

		public Feed(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Parent = new ForeignKeyField<Feed, Feed>(this, "parentid", true, p => p.Children);
            Children = new List<Feed>();
            Groups = new List<Group>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Feed>("parentid", Id.Value, () => Children);
                yield return new MultiCascade<Group>("feedid", Id.Value, () => Groups);
            } 
        }

        public IEnumerable<Feed> Superordinates
        {
            get
            {
                var current = this;

                while (current.Parent.Value != null)
                {
                    current = current.Parent.Value;
                    yield return current; 
                }
            }
        }

        public IEnumerable<Feed> Subordinates
        {
            get 
            {
                foreach (var c in Children)
                {
                    yield return c;

                    foreach (var s in c.Subordinates)
                    {
                        yield return s; 
                    }
                }
            } 
        }

        public override void Delete(IDatabase database)
        {
            foreach (var mailing in database.Query<Mailing>(DC.Equal("recipientfeedid", Id.Value)))
            {
                mailing.Delete(database);
            }

            foreach (var subscription in database.Query<Subscription>(DC.Equal("feedid", Id.Value)))
            {
                subscription.Delete(database);
            }

            foreach (var group in Groups)
            {
                group.Delete(database);
            }

            foreach (var child in Children.ToList())
            {
                child.Parent.Value = Parent.Value;
                database.Save(child);
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

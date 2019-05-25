using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Petitio
{
    public class Queue : DatabaseObject
    {
		public MultiLanguageStringField Name { get; private set; }
        public ForeignKeyField<Queue, Queue> Parent { get; private set; }
        public List<Queue> Children { get; private set; }
        public List<Group> Groups { get; private set; }

        public Queue() : this(Guid.Empty)
        {
        }

		public Queue(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Parent = new ForeignKeyField<Queue, Queue>(this, "parentid", true, p => p.Children);
            Children = new List<Queue>();
            Groups = new List<Group>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Queue>("parentid", Id.Value, () => Children);
                yield return new MultiCascade<Group>("queueid", Id.Value, () => Groups);
            } 
        }

        public IEnumerable<Queue> Superordinates
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

        public IEnumerable<Queue> Subordinates
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
            foreach (var subscription in database.Query<Subscription>(DC.Equal("queueid", Id.Value)))
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

using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Census
{
    public class Organization : DatabaseObject
    {
		public MultiLanguageStringField Name { get; private set; }
        public ForeignKeyField<Organization, Organization> Parent { get; private set; }
        public List<Organization> Children { get; private set; }
        public List<Group> Groups { get; private set; }

        public Organization() : this(Guid.Empty)
        {
        }

		public Organization(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Parent = new ForeignKeyField<Organization, Organization>(this, "parentid", true, p => p.Children);
            Children = new List<Organization>();
            Groups = new List<Group>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<Organization>("parentid", Id.Value, () => Children);
                yield return new MultiCascade<Group>("organizationid", Id.Value, () => Groups);
            } 
        }

        public IEnumerable<Organization> Superordinates
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

        public IEnumerable<Organization> Subordinates
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

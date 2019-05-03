using System;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public abstract class DatabaseObject : IEquatable<DatabaseObject>
	{
        public bool NewlyCreated { get; private set; } = true;

        public void Updated()
        {
            NewlyCreated = false;

            foreach (var f in Fields)
            {
                f.Updated(); 
            } 
        }

        public bool Dirty
        { 
            get
            {
                return NewlyCreated || Fields.Any(f => f.Dirty);
            }
        }

        public void Validate()
        {
            foreach (var f in Fields)
            {
                f.Validate();
            }
        }

        public List<Field> Fields { get; private set; }

        public GuidIdPrimaryKeyField Id { get; private set; }

		public DatabaseObject(Guid id)
		{
            Fields = new List<Field>();
            Id = new GuidIdPrimaryKeyField(this, id);
		}

        public static bool operator ==(DatabaseObject a, DatabaseObject b)
        {
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            else if ((a is null) && (b is null))
            {
                return true;
            }
            else if ((a is null) || (b is null))
            {
                return false;
            }
            else
            {
                return a.Equals(b);
            }
        }

        public static bool operator !=(DatabaseObject a, DatabaseObject b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            var b = obj as DatabaseObject;

            if (b != null)
            {
                return Equals(b);
            }
            else
            {
                return false; 
            }
        }

        public bool Equals(DatabaseObject other)
        {
            return Id.Equals(other.Id);
        }

        public override int GetHashCode()
        {
            return 13371337 + Id.Value.GetHashCode();
        }

        public virtual IEnumerable<MultiCascade> Cascades { get { return new MultiCascade[0]; } }

        public abstract string GetText(Translator translator);

        public abstract void Delete(IDatabase database);
    }
}

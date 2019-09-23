using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace DiscourseEngagement
{
    public class Person : DatabaseObject
    {
        public Field<int> UserId { get; private set; }

        public Person() : this(Guid.Empty)
        {
        }

        public Person(Guid id) : base(id)
        {
            UserId = new Field<int>(this, "userid", 0);
        }

        public override string ToString()
        {
            return "Person " + Id.ToString();
        }

        public override string GetText(Translator translator)
        {
            return Id.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this); 
        }
    }
}

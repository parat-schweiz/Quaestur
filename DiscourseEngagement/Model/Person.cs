using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace DiscourseEngagement
{
    public class Person : DatabaseObject
    {
        public Field<int> DiscourseUserId { get; private set; }

        public Person() : this(Guid.Empty)
        {
        }

        public Person(Guid id) : base(id)
        {
            DiscourseUserId = new Field<int>(this, "number", 0);
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

using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Mercatus
{
    public class Seeder
    {
        private IDatabase _db;
        private Random _rnd;

        public Seeder(IDatabase db)
        {
            _db = db;
            _rnd = new Random(0);
        }

        public void MinimalSeed()
        {
        }
    }
}

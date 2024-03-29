﻿using System;
using System.Linq;
using System.Collections.Generic;
using SiteLibrary;

namespace Quaestur
{
    public class VotingRightsUpdateTask : ITask
    {
        private DateTime _lastUpdate;
        private Queue<Guid> _updateQueue;

        public VotingRightsUpdateTask()
        {
            _updateQueue = new Queue<Guid>(); 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastUpdate.AddMinutes(1))
            {
                _lastUpdate = DateTime.UtcNow;
                Global.Log.Info("Voting rights update task");

                if (_updateQueue.Count < 1)
                {
                    foreach (var id in database.Query<Membership>()
                        .OrderBy(m => m.Person.Value.Number.Value)
                        .Select(m => m.Id.Value))
                    {
                        _updateQueue.Enqueue(id);
                    }
                }

                if (_updateQueue.Count > 0)
                {
                    var id = _updateQueue.Dequeue();

                    using (var transaction = database.BeginTransaction())
                    {
                        var membership = database.Query<Membership>(id);

                        if (membership != null)
                        {
                            membership.UpdateVotingRight(database);
                            database.Save(membership);
                            transaction.Commit();
                        }
                    }
                }

                Global.Log.Info("Voting rights update task complete");
            }
        }
    }
}

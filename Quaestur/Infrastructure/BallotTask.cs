using System;
using System.Linq;
using System.Collections.Generic;

namespace Quaestur
{
    public class BallotTask : ITask
    {
        private DateTime _lastSending;
        private int _maxMailsCount;

        public BallotTask()
        {
            _lastSending = DateTime.MinValue; 
        }

        public void Run(IDatabase database)
        {
            if (DateTime.UtcNow > _lastSending.AddMinutes(5))
            {
                _lastSending = DateTime.UtcNow;
                _maxMailsCount = 500;
                Global.Log.Notice("Running ballot task");

                RunAll(database);

                Global.Log.Notice("Ballot task complete");
            }
        }

        private void Journal(IDatabase db, Membership membership, string key, string hint, string technical, params Func<Translator, string>[] parameters)
        {
            var translation = new Translation(db);
            var translator = new Translator(translation, membership.Person.Value.Language.Value);
            var entry = new JournalEntry(Guid.NewGuid());
            entry.Moment.Value = DateTime.UtcNow;
            entry.Text.Value = translator.Get(key, hint, technical, parameters.Select(p => p(translator)));
            entry.Subject.Value = translator.Get("Document.Billing.Process", "Billing process naming", "Billing process");
            entry.Person.Value = membership.Person.Value;
            db.Save(entry);

            var technicalTranslator = new Translator(translation, Language.Technical);
            Global.Log.Notice("{0} modified {1}: {2}",
                entry.Subject.Value,
                entry.Person.Value.ShortHand,
                technicalTranslator.Get(key, hint, technical, parameters.Select(p => p(technicalTranslator))));
        }

        private void RunAll(IDatabase database)
        {
            foreach (var ballot in database.Query<Ballot>())
            {
                RunBallot(database, ballot);
            }
        }

        private Dictionary<Guid, Membership> QueryAllMemberships(IDatabase database, Ballot ballot)
        {
            var list = new Dictionary<Guid, Membership>();

            foreach (var membership in database
                .Query<Membership>(DC.Equal("organizationid", ballot.Template.Value.Organizer.Value.Organization.Value.Id.Value)))
            {
                if (membership.IsActive && (!membership.Person.Value.Deleted.Value))
                {
                    list.Add(membership.Id.Value, membership);
                }
            }

            return list;
        }

        private Dictionary<Guid, BallotPaper> QueryAllBallotPapers(IDatabase database, Ballot ballot)
        {
            var list = new Dictionary<Guid, BallotPaper>();

            foreach (var ballotPaper in database.Query<BallotPaper>(DC.Equal("ballotid", ballot.Id.Value)))
            {
                list.Add(ballotPaper.Member.Value.Id.Value, ballotPaper);
            }

            return list;
        }

        private void RunBallot(IDatabase database, Ballot ballot)
        {
            switch (ballot.Status.Value)
            {
                case BallotStatus.New:
                    {
                        if (DateTime.Now.Date >= ballot.AnnouncementDate)
                        {
                            ballot.Status.Value = BallotStatus.Announcing;
                            database.Save(ballot);
                        }
                        UpdateMembers(database, ballot);
                    }
                    break;
                case BallotStatus.Announcing:
                    {
                        if (DateTime.Now.Date >= ballot.StartDate)
                        {
                            ballot.Status.Value = BallotStatus.Voting;
                            database.Save(ballot);
                        }
                        UpdateMembers(database, ballot);
                    }
                    break;
                case BallotStatus.Voting:
                    {
                        UpdateMembers(database, ballot);
                    }
                    break;
            }
        }

        private void UpdateMembers(IDatabase database, Ballot ballot)
        {
            var memberships = QueryAllMemberships(database, ballot);
            var ballotPapers = QueryAllBallotPapers(database, ballot);
            AddNewMembers(database, ballot, memberships, ballotPapers);
            UpdateMembers(database, ballot, memberships, ballotPapers);
        }

        private void UpdateMembers(IDatabase database, Ballot ballot, IDictionary<Guid, Membership> memberships, IDictionary<Guid, BallotPaper> ballotPapers)
        {
            foreach (var ballotPaper in ballotPapers.Values)
            {
                switch (ballotPaper.Status.Value)
                {
                    case BallotPaperStatus.New:
                        if (!memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                        {
                            ballotPaper.Status.Value = BallotPaperStatus.Canceled;
                            ballotPaper.LastUpdate.Value = DateTime.UtcNow;
                            database.Save(ballotPaper);
                        }
                        else if (ballot.Status.Value == BallotStatus.Announcing)
                        {
                            Announce(database, ballot, ballotPaper);
                        }
                        else if (ballot.Status.Value == BallotStatus.Voting)
                        {
                            Invite(database, ballot, ballotPaper);
                        }
                        else if (ballot.Status.Value == BallotStatus.Finished)
                        {
                            CheckRight(database, ballot, ballotPaper);
                        }
                        break;
                    case BallotPaperStatus.Informed:
                        if (!memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                        {
                            ballotPaper.Status.Value = BallotPaperStatus.Canceled;
                            ballotPaper.LastUpdate.Value = DateTime.UtcNow;
                            database.Save(ballotPaper);
                        }
                        else if (ballot.Status.Value == BallotStatus.Voting)
                        {
                            Invite(database, ballot, ballotPaper);
                        }
                        else if (ballot.Status.Value == BallotStatus.Finished)
                        {
                            CheckRight(database, ballot, ballotPaper);
                        }
                        break;
                    case BallotPaperStatus.Invited:
                        if (!memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                        {
                            ballotPaper.Status.Value = BallotPaperStatus.Canceled;
                            ballotPaper.LastUpdate.Value = DateTime.UtcNow;
                            database.Save(ballotPaper);
                        }
                        else if (ballot.Status.Value == BallotStatus.Finished)
                        {
                            CheckRight(database, ballot, ballotPaper);
                        }
                        break;
                    case BallotPaperStatus.Canceled:
                        if (memberships.ContainsKey(ballotPaper.Member.Value.Id.Value))
                        {
                            ballotPaper.Status.Value = BallotPaperStatus.New;
                            ballotPaper.LastUpdate.Value = DateTime.UtcNow;
                            database.Save(ballotPaper);
                        }
                        break;
                }
            }
        }

        private void Announce(IDatabase database, Ballot ballot, BallotPaper ballotPaper)
        {
        }

        private void Invite(IDatabase database, Ballot ballot, BallotPaper ballotPaper)
        {
        }

        private void CheckRight(IDatabase database, Ballot ballot, BallotPaper ballotPaper)
        {
            ballotPaper.Member.Value.UpdateVotingRight(database);

            if (ballotPaper.Member.Value.HasVotingRight.Value.Value)
            {
                ballotPaper.Status.Value = BallotPaperStatus.RightVerified;
            }
            else
            {
                ballotPaper.Status.Value = BallotPaperStatus.NoRight;
            }

            ballotPaper.LastUpdate.Value = DateTime.UtcNow;
            database.Save(ballotPaper);
        }

        private void AddNewMembers(IDatabase database, Ballot ballot, IDictionary<Guid, Membership> memberships, IDictionary<Guid, BallotPaper> ballotPapers)
        { 
            foreach (var membership in memberships.Values)
            {
                if (!ballotPapers.ContainsKey(membership.Id))
                {
                    var newBallotPaper = new BallotPaper(Guid.NewGuid());
                    newBallotPaper.Ballot.Value = ballot;
                    newBallotPaper.Member.Value = membership;
                    newBallotPaper.Status.Value = BallotPaperStatus.New;
                    newBallotPaper.LastUpdate.Value = DateTime.UtcNow;
                    database.Save(newBallotPaper);
                    ballotPapers.Add(newBallotPaper.Id.Value, newBallotPaper);
                }
            }
        }
    }
}

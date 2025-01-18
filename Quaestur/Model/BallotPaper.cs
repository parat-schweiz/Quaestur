using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public enum BallotPaperStatus
    {
        New = 0,
        Informed = 1,
        Invited = 2,
        RightVerified = 3,
        NoRight = 4,
        Canceled = 5,
        Voted = 6,
    }

    public static class BallotPaperStatusExtensions
    {
        public static string Translate(this BallotPaperStatus status, Translator translator)
        {
            switch (status)
            {
                case BallotPaperStatus.New:
                    return translator.Get("Enum.BallotPaperStatus.New", "Value 'New' in BallotPaperStatus enum", "New");
                case BallotPaperStatus.Informed:
                    return translator.Get("Enum.BallotPaperStatus.Informed", "Value 'Informed' in BallotPaperStatus enum", "Informed");
                case BallotPaperStatus.Invited:
                    return translator.Get("Enum.BallotPaperStatus.Invited", "Value 'Invited' in BallotPaperStatus enum", "Invited");
                case BallotPaperStatus.RightVerified:
                    return translator.Get("Enum.BallotPaperStatus.RightVerified", "Value 'RightVerified' in BallotPaperStatus enum", "Right verified");
                case BallotPaperStatus.NoRight:
                    return translator.Get("Enum.BallotPaperStatus.NoRight", "Value 'NoRight' in BallotPaperStatus enum", "No right");
                case BallotPaperStatus.Canceled:
                    return translator.Get("Enum.BallotPaperStatus.Canceled", "Value 'Canceled' in BallotPaperStatus enum", "Canceled");
                case BallotPaperStatus.Voted:
                    return translator.Get("Enum.BallotPaperStatus.Voted", "Value 'Voted' in BallotPaperStatus enum", "Voted");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class BallotPaper : DatabaseObject
    {
        public ForeignKeyField<Ballot, BallotPaper> Ballot { get; private set; }
        public ForeignKeyField<Membership, BallotPaper> Member { get; private set; }
        public EnumField<BallotPaperStatus> Status { get; private set; }
        public DateTimeField LastTry { get; private set; }

        public BallotPaper() : this(Guid.Empty)
        {
        }

        public BallotPaper(Guid id) : base(id)
        {
            Ballot = new ForeignKeyField<Ballot, BallotPaper>(this, "ballotid", false, null);
            Member = new ForeignKeyField<Membership, BallotPaper>(this, "memberid", false, null);
            Status = new EnumField<BallotPaperStatus>(this, "status", BallotPaperStatus.New, BallotPaperStatusExtensions.Translate);
            LastTry = new DateTimeField(this, "lasttry", DateTime.UtcNow);
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return base.ToString();
        }

        public byte[] ComputeCode()
        {
            using (var serializer = new Serializer())
            {
                serializer.Write(Ballot.Value.Id.Value);
                serializer.Write(Member.Value.Person.Value.Id.Value);
                serializer.Write(Member.Value.Organization.Value.Id.Value);

                using (var hmac = new HMACSHA256())
                {
                    hmac.Key = Ballot.Value.Secret.Value;
                    return hmac.ComputeHash(serializer.Data);
                }
            }
        }
    }
}

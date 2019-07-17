using System;
using System.Collections.Generic;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class BudgetPeriod : DatabaseObject
    {
        public ForeignKeyField<Organization, BudgetPeriod> Organization { get; private set; }
        public Field<DateTime> StartDate { get; private set; }
        public Field<DateTime> EndDate { get; private set; }
        public Field<long> TotalPoints { get; private set; }

        public BudgetPeriod() : this(Guid.Empty)
        {
        }

        public BudgetPeriod(Guid id) : base(id)
        {
            Organization = new ForeignKeyField<Organization, BudgetPeriod>(this, "organizationid", false, null);
            StartDate = new Field<DateTime>(this, "startdate", DateTime.UtcNow.Date);
            EndDate = new Field<DateTime>(this, "enddate", DateTime.UtcNow.Date.AddDays(365));
            TotalPoints = new Field<long>(this, "totalpoints", 0);
        }

        public override string ToString()
        {
            return Organization.Value.Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            database.Delete(this);
        }

        public void UpdateTotalPoints(IDatabase database)
        {
            var memberships = database.Query<Membership>(DC.Equal("organizationid", Organization.Value.Id.Value));
            long totalPoints = 0;

            foreach (var m in memberships)
            {
                var length = EndDate.Value.Date.Subtract(StartDate.Value.Date);
                var overlap = Dates.ComputeOverlap(m.StartDate.Value.Date, (m.EndDate.Value ?? DateTime.MaxValue).Date, StartDate.Value.Date, EndDate.Value.Date);
                var fraction = overlap.TotalDays / length.TotalDays;
                totalPoints += (long)Math.Floor((double)m.Type.Value.MaximumPoints.Value * fraction);
            }

            TotalPoints.Value = totalPoints;
        }

        public string TextYearPart
        {
            get
            {
                var start = StartDate.Value;
                var end = EndDate.Value;
                var days = end.Subtract(start).TotalDays;

                if (days >= 360)
                {
                    if (start.Year == end.Year)
                    {
                        return start.Year.ToString();
                    }
                    else
                    {
                        if (start.Year.ToString().Substring(0, 3) == end.Year.ToString().Substring(0, 3))
                        {
                            return start.Year.ToString() + "/" + end.Year.ToString().Substring(3);
                        }
                        else if (start.Year.ToString().Substring(0, 2) == end.Year.ToString().Substring(0, 3))
                        {
                            return start.Year.ToString() + "/" + end.Year.ToString().Substring(2);
                        }
                        else
                        {
                            return start.Year.ToString() + "/" + end.Year.ToString();
                        }
                    }
                }
                else
                {
                    return start.Year.ToString() + "." + start.Month.ToString();
                }
            }
        }

        public override string GetText(Translator translator)
        {
            return Organization.Value.GetText(translator) + " " + TextYearPart;
        }
    }
}
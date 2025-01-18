using System;
using System.Collections.Generic;
using System.Linq;
using BaseLibrary;
using SiteLibrary;

namespace Quaestur
{
    public class BudgetPeriod : DatabaseObject
    {
        public ForeignKeyField<Organization, BudgetPeriod> Organization { get; private set; }
        public DateField StartDate { get; private set; }
        public DateField EndDate { get; private set; }
        public Field<long> TotalPoints { get; private set; }

        public BudgetPeriod() : this(Guid.Empty)
        {
        }

        public BudgetPeriod(Guid id) : base(id)
        {
            Organization = new ForeignKeyField<Organization, BudgetPeriod>(this, "organizationid", false, null);
            StartDate = new DateField(this, "startdate", DateTime.UtcNow.Date);
            EndDate = new DateField(this, "enddate", DateTime.UtcNow.Date.AddDays(365));
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

        private TimeSpan Length
        {
            get
            {
                return EndDate.Value.Date.Subtract(StartDate.Value.Date);
            }
        }

        private decimal ComputePeriodFactor(IPaymentModel paymentModel)
        {
            var billingPeriod = paymentModel.GetBillingPeriod();
            var daysLength = (int)Math.Floor(Length.TotalDays);

            if (daysLength == billingPeriod)
            {
                return 1m;
            }
            else if (daysLength > billingPeriod)
            {
                if (daysLength >= 360 && daysLength <= 367)
                {
                    if (billingPeriod >= 28 && billingPeriod <= 32)
                    {
                        return 12m;
                    }
                    else if (billingPeriod >= 84 && billingPeriod <= 96)
                    {
                        return 4m;
                    }
                    else if (billingPeriod >= 168 && billingPeriod <= 192)
                    {
                        return 2m;
                    }
                    else if (billingPeriod >= 360 && billingPeriod <= 367)
                    {
                        return 1m; 
                    }
                    else
                    {
                        return (decimal)daysLength / (decimal)billingPeriod;
                    }
                }
                else
                {
                    return (decimal)daysLength / (decimal)billingPeriod;
                }
            }
            else
            {
                return (decimal)daysLength / (decimal)billingPeriod;
            }
        }

        public void UpdateTotalPoints(IDatabase database)
        {
            var memberships = database.Query<Membership>(DC.Equal("organizationid", Organization.Value.Id.Value));
            long totalPoints = 0;

            foreach (var m in memberships)
            {
                if (m.Type.Value.Payment.Value != PaymentModel.None)
                {
                    var overlap = Dates.ComputeOverlap(m.StartDate.Value.Date, (m.EndDate.Value ?? DateTime.MaxValue).Date, StartDate.Value.Date, EndDate.Value.Date);
                    decimal fraction = (decimal)overlap.TotalDays / (decimal)Length.TotalDays;
                    var paymentModel = m.Type.Value.CreatePaymentModel(database);
                    var periodFactor = ComputePeriodFactor(paymentModel);
                    totalPoints += (long)Math.Floor((decimal)m.Type.Value.MaximumPoints.Value * periodFactor * fraction);
                }
            }

            var transfers = database.Query<PointTransfer>(DC.Equal("sinkid", Id.Value));

            foreach (var transfer in transfers)
            {
                totalPoints += (long)Math.Floor((decimal)transfer.Source.Value.TotalPoints.Value * transfer.Share.Value / 100m);
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
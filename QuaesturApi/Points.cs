using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BaseLibrary;

namespace QuaesturApi
{
    public class Points
    {
        public Guid Id { get; private set; }
        public Guid OwnerId { get; private set; }
        public Guid BudgetId { get; private set; }
        public int Amount { get; private set; }
        public string Reason { get; private set; }
        public DateTime Moment { get; private set; }
        public PointsReferenceType ReferenceType { get; private set; }
        public Guid ReferenceId { get; private set; }

        public Points(JObject obj)
        {
            Id = Guid.Parse(obj.Value<string>("id"));
            OwnerId = Guid.Parse(obj.Value<string>("ownerid"));
            BudgetId = Guid.Parse(obj.Value<string>("budgetid"));
            Amount = obj.Value<int>("amount");
            Reason = obj.Value<string>("reason");
            Moment = obj.Value<string>("moment").ParseIsoDate();
            ReferenceType = (PointsReferenceType)Enum.Parse(typeof(PointsReferenceType), obj.Value<string>("referencetype"));
            ReferenceId = Guid.Parse(obj.Value<string>("referenceid"));
        }
    }
}

﻿using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace QuaesturApi
{
    public class PointBudget
    {
        public Guid Id { get; private set; }
        public MultiLanguageString Label { get; private set; }
        public MultiLanguageString FullLabel { get; private set; }
        public Guid OwnerId { get; private set; }
        public Guid PeriodId { get; private set; }
        public decimal Share { get; private set; }
        public long CurrentPoints { get; private set; }

        public PointBudget(JObject obj)
        {
            Id = Guid.Parse(obj.Value<string>("id"));
            Label = new MultiLanguageString(obj.Value<JObject>("label"));
            FullLabel = new MultiLanguageString(obj.Value<JObject>("fulllabel"));
            OwnerId = Guid.Parse(obj.Value<string>("ownerid"));
            PeriodId = Guid.Parse(obj.Value<string>("periodid"));
            Share = obj.Value<decimal>("Share");
            CurrentPoints = obj.Value<long>("currentpoints");
        }
    }
}

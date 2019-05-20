using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Scriba
{
    public class Pad
    {
        private readonly List<Change> _changes;

        public Guid Id { get; private set; }

        public Pad(Guid id)
        {
            Id = id;
            _changes = new List<Change>();
        }

        public void Add(Change change)
        {
            _changes.Add(change); 
        }

        public bool HasChanges(int sinceIndex)
        {
            return sinceIndex >= _changes.Count;
        }

        public JArray Changes(int sinceIndex)
        {
            var array = new JArray();

            for (int index = sinceIndex; index < _changes.Count; index++)
            {
                array.Add(_changes[index].ToJson());
            }

            return array;
        }
    }

    public enum ChangeType
    {
        Add,
        Delete 
    }

    public class Change
    {
        private const string TypeProperty = "Type";
        private const string IndexProperty = "Index";
        private const string DataProperty = "Data";

        public ChangeType Type { get; private set; }
        public int Index { get; private set; }
        public string Data { get; private set; }

        public Change(ChangeType type, int index, string data)
        {
            Type = type;
            Index = index;
            Data = data; 
        }

        public Change(JObject obj)
        {
            Type = (ChangeType)obj.ValueInt(TypeProperty);
            Index = obj.ValueInt(IndexProperty);
            Data = Encoding.UTF8.GetString(Convert.FromBase64String(obj.ValueString(DataProperty)));
        }

        public JObject ToJson()
        {
            return new JObject(
                new JProperty(TypeProperty, (int)Type),
                new JProperty(IndexProperty, Index),
                new JProperty(DataProperty, Convert.ToBase64String(Encoding.UTF8.GetBytes(Data))));
        }
    }
}

using System;
using Nancy;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class ReadOnlyWidget<TObject, TValue> : Widget<TObject>
        where TObject : DatabaseObject, new()
    {
        private readonly Func<TObject, TValue> _getValue;

        public TValue Value { get; set; }

        public ReadOnlyWidget(Form form, string id, int width, string phraseField, Func<TObject, TValue> getValue)
            : base(form, id, width, false, phraseField)
        {
            _getValue = getValue;
        }

        public ReadOnlyWidget(Form form, string id, int width, string phraseField, TValue value)
            : base(form, id, width, false, phraseField)
        {
            Value = value;
        }

        public override string Html
        {
            get { return Render("View/Widget/readonly.sshtml"); }
        }

        public virtual string StringValue
        {
            get { return Value?.ToString() ?? string.Empty; }
        }

        public override string Js { get { return string.Empty; } }

        public override string GetValue { get { return string.Empty; } }

        public override string SetValidation { get { return string.Empty; } }

        public override void LoadValue(TObject obj)
        {
            if (_getValue != null)
            {
                Value = _getValue(obj);
            }
        }

        public override void SaveValue(PostStatus status, JObject data, TObject obj)
        {
        }
    }
}

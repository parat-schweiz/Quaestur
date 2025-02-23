using System;
using Nancy;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class StringWidget<TObject> : Widget<TObject, StringField, string>
        where TObject : DatabaseObject, new()
    {
        private readonly Func<TObject, StringField> _field;
        private readonly Func<string, string> _validation;

        public bool Required { get; private set; }

        public StringWidget(
            Form form, 
            string id, 
            int width, 
            string phraseField, 
            Func<TObject, StringField> field, 
            bool required,
            Func<string, string> validation = null)
            : base(form, id, width, phraseField)
        {
            _field = field;
            _validation = validation;
            Required = required;
        }

        public override string Html
        {
            get { return Render("View/Widget/string.sshtml"); }
        }

        public override void SaveValue(PostStatus status, JObject data, TObject obj)
        {
            var field = _field(obj);
            var valid =
                Required ?
                status.AssignStringRequired(Id, field, data.ValueString(Id)) :
                status.AssignStringFree(Id, field, data.ValueString(Id));

            if (valid && (_validation != null))
            {
                var validationMessage = _validation(field.Value);
                if (!string.IsNullOrEmpty(validationMessage))
                {
                    status.Add(Id, validationMessage);
                }
            }
            if (field.Dirty)
            {
                UpdatedObject = obj;
            }
        }

        public override void LoadValue(TObject obj)
        {
            Value = _field(obj).Value;
        }
    }
}

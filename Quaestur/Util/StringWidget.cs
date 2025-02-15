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
            Form<TObject> form, 
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

        public override void AssignValue(PostStatus status, JObject data, TObject obj)
        {
            var valid =
                Required ?
                status.AssignStringRequired(Id, _field(obj), data.ValueString(Id)) :
                status.AssignStringFree(Id, _field(obj), data.ValueString(Id));

            if (valid && (_validation != null))
            {
                var validationMessage = _validation(_field(obj).Value);
                if (!string.IsNullOrEmpty(validationMessage))
                {
                    status.Add(Id, validationMessage);
                }
            }
        }
    }
}

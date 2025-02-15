using System;
using Nancy;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class DateWidget<TObject> : Widget<TObject, DateField, DateTime>
        where TObject : DatabaseObject, new()
    {
        private readonly Func<TObject, DateField> _field;

        public DateWidget(Form<TObject> form, string id, int width, string phraseField, Func<TObject, DateField> field)
            : base(form, id, width, phraseField)
        {
            _field = field;
        }

        public override string Html
        {
            get { return Render("View/Widget/date.sshtml"); }
        }

        public override string Js
        {
            get { return Render("View/Widget/date.js.sshtml"); }
        }

        public override void AssignValue(PostStatus status, JObject data, TObject obj)
        {
            status.AssignDateString(Id, _field(obj), data.ValueString(Id));
        }
    }
}

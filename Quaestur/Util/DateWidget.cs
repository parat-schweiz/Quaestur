using System;
using BaseLibrary;
using Nancy;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class DateWidget<TObject> : StructWidget<TObject, DateField, DateTime>
        where TObject : DatabaseObject, new()
    {
        private readonly Func<TObject, DateField> _field;

        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public Language Language { get; private set; }

        public string StringStartDate => StartDate.FormatSwissDateDay();
        public string StringEndDate => EndDate.FormatSwissDateDay();
        public string StringLanguage => Language.Locale();

        public override string StringValue
        {
            get { return Value?.FormatSwissDateDay() ?? string.Empty; }
        }

        public DateWidget(Form<TObject> form, string id, int width, bool required, string phraseField, Func<TObject, DateField> field, DateTime? defaultValue, DateTime startDate, DateTime endDate, Language language)
            : base(form, id, width, required, phraseField)
        {
            _field = field;
            Value = defaultValue;
            StartDate = startDate;
            EndDate = endDate;
            Language = language;
        }

        public override string Html
        {
            get { return Render("View/Widget/date.sshtml"); }
        }

        public override string Js
        {
            get { return Render("View/Widget/date.js.sshtml"); }
        }

        public override void SaveValue(PostStatus status, JObject data, TObject obj)
        {
            var field = _field(obj);
            status.AssignDateString(Id, field, data.ValueString(Id));
            if (field.Dirty)
            {
                UpdatedObject = obj;
            }
        }

        public override void LoadValue(TObject obj)
        {
            var field = _field(obj);
            Value = field.Value;
        }
    }
}

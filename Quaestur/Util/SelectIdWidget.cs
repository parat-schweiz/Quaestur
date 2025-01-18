using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class SelectIdWidget<TObject, TSelect> : Widget<TObject>
        where TObject : DatabaseObject, new()
        where TSelect : DatabaseObject, new()
    {
        private readonly Func<TObject, ForeignKeyField<TSelect, TObject>> _field;
        public IEnumerable<NamedIdViewModel> Items { get; private set; }

        public SelectIdWidget(
            Form form, 
            string id,
            int width, 
            bool required,
            string phraseField,
            Func<TObject, ForeignKeyField<TSelect, TObject>> field,
            Func<TSelect, NamedIdViewModel> create,
            Func<NamedIdViewModel> additionalItem = null,
            DataCondition dataCondition = null,
            Func<TSelect, bool> condition = null,
            Func<TSelect, object> orderBy = null)
            : base(form, id, width, required, phraseField)
        {
            _field = field;
            if (orderBy == null)
            {
                orderBy = x => x.ToString();
            }
            if (condition == null)
            {
                condition = i => true;
            }
            var query = dataCondition == null ? 
                Form.Module.Database.Query<TSelect>() : 
                Form.Module.Database.Query<TSelect>(dataCondition);
            var list = query
                .Where(condition)
                .OrderBy(orderBy)
                .Select(create)
                .ToList();
            if (additionalItem != null)
            {
                list.Add(additionalItem());
            }
            Items = list;
        }

        public override string Html
        {
            get { return Render("View/Widget/selectid.sshtml"); }
        }

        public override string Js
        {
            get { return string.Empty; }
        }

        public override string GetValue
        {
            get { return string.Format("formData.{1} = $(\"#{0}{1}\").val();", Form.Id, Id); }
        }

        public override string SetValidation
        {
            get { return string.Format("assignFieldValidation(\"{0}\", result);", Id); }
        }

        public override void SaveValue(PostStatus status, JObject data, TObject obj)
        {
            var field = _field(obj);
            status.AssignObjectIdString(Id, _field(obj), data.ValueString(Id));
            if (field.Dirty)
            {
                UpdatedObject = obj;
            }
        }

        public override void LoadValue(TObject obj)
        {
            var field = _field(obj);
            var selectedItem = Items.FirstOrDefault(t => t.Id == (field.Value?.Id?.ToString() ?? string.Empty));
            if (selectedItem != null)
            {
                selectedItem.Selected = true;
            }
        }
    }
}

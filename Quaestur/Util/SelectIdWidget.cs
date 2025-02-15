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
            string phraseField,
            Func<TObject, ForeignKeyField<TSelect, TObject>> field,
            Func<TSelect, NamedIdViewModel> create,
            DataCondition condition = null,
            Func<NamedIdViewModel> additionalItem = null,
            Func<NamedIdViewModel, object> orderBy = null)
            : base(form, id, width, phraseField)
        {
            _field = field;
            if (orderBy == null)
            {
                orderBy = x => x.Name;
            }
            var query = condition == null ? 
                Form.Module.Database.Query<TSelect>() : 
                Form.Module.Database.Query<TSelect>(condition);
            var list = query
                .Select(create)
                .OrderBy(orderBy)
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
            get { return string.Format("$(\"#{0}{1}\").val();", Form.Id, Id); }
        }

        public override string SetValidation
        {
            get { return string.Format("assignFieldValidation(\"{0}\", result);", Id); }
        }

        public override void AssignValue(PostStatus status, JObject data, TObject obj)
        {
            status.AssignObjectIdString(Id, _field(obj), data.ValueString(Id));
        }
    }
}

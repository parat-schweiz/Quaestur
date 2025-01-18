using System;
using System.IO;
using System.Text;
using Nancy;
using Nancy.ViewEngines;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class SubWidget<TObject, TSub> : IWidget<TObject>
        where TObject : DatabaseObject, new()
        where TSub : DatabaseObject, new()
    {
        private readonly IWidget<TSub> _widget;
        private readonly Func<TObject, bool, TSub> _get;

        public SubWidget(IWidget<TSub> widget, Func<TObject, bool, TSub> get)
        {
            _widget = widget;
            _get = get;
        }

        public Form Form => _widget.Form;
        public string Id => _widget.Id;
        public int Width => _widget.Width;
        public string PhraseField => _widget.PhraseField;

        public string Html => _widget.Html;
        public string Js => _widget.Js;
        public string GetValue => _widget.GetValue;
        public string SetValidation => _widget.SetValidation;
        public DatabaseObject UpdatedObject => _widget.UpdatedObject;

        public void LoadValue(TObject obj)
        {
            var sub = _get(obj, false);
            if (sub != null)
            {
                _widget.LoadValue(sub);
            }
        }

        public void SaveValue(PostStatus status, JObject data, TObject obj)
        {
            _widget.SaveValue(status, data, _get(obj, true));
        }
    }
}

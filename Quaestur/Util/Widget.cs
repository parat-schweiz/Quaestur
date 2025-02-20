using System;
using System.IO;
using System.Text;
using Nancy;
using Nancy.ViewEngines;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public interface IWidget
    {
        Form Form { get; }
        string Id { get; }
        int Width { get; }
        string PhraseField { get; }

        string Html { get; }
        string Js { get; }
        string GetValue { get; }
        string SetValidation { get; }
        DatabaseObject UpdatedObject { get; }
    }

    public interface IWidget<TObject> : IWidget
        where TObject : DatabaseObject, new()
    {
        void SaveValue(PostStatus status, JObject data, TObject obj);
        void LoadValue(TObject obj);
    }

    public abstract class SubWidget<TObject, TSub> : IWidget<TObject>
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

    public abstract class Widget<TObject> : IWidget<TObject>
        where TObject : DatabaseObject, new()
    {
        public Form Form { get; private set; }
        public string Id { get; private set; }
        public int Width { get; private set; }
        public string PhraseField { get; private set; }

        public abstract string Html { get; }
        public abstract string Js { get; }
        public abstract string GetValue { get; }
        public abstract string SetValidation { get; }

        public Widget(Form form, string id, int width, string phraseField)
        {
            Form = form;
            Id = id;
            Width = width;
            PhraseField = phraseField;
        }

        protected string Render(string viewName)
        {
            var response = Form.Module.ViewFactory.RenderView(viewName, this, ViewLocationContext);
            using (var stream = new MemoryStream())
            {
                response.Contents(stream);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private ViewLocationContext ViewLocationContext
        {
            get
            {
                return new ViewLocationContext
                {
                    Context = Form.Module.Context,
                    ModuleName = Form.Module.Context.NegotiationContext.ModuleName,
                    ModulePath = Form.Module.Context.NegotiationContext.ModulePath
                };
            }
        }

        public DatabaseObject UpdatedObject { get; protected set; }

        public abstract void SaveValue(PostStatus status, JObject data, TObject obj);
        public abstract void LoadValue(TObject obj);
    }

    public abstract class Widget<TObject, TField, TValue> : Widget<TObject>
        where TObject : DatabaseObject, new()
        where TField : ValueField<TValue>
    {
        public TValue Value { get; set; }

        public Widget(Form<TObject> form, string id, int width, string phraseField)
            : base(form, id, width, phraseField)
        {
        }

        public virtual string StringValue
        {
            get { return Value?.ToString() ?? string.Empty; }
        }

        public override string Js { get { return string.Empty; } }

        public override string GetValue
        {
            get { return string.Format("formData.{1} = $(\"#{0}{1}\").val();", Form.Id, Id); }
        }

        public override string SetValidation
        {
            get { return string.Format("assignFieldValidation(\"{0}\", result);", Id); }
        }

        public override void LoadValue(TObject obj)
        {
        }
    }
}

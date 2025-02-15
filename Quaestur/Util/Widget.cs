using System;
using System.IO;
using System.Text;
using Nancy;
using Nancy.ViewEngines;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public abstract class Widget<TObject>
        where TObject : DatabaseObject, new()
    {
        public Form Form { get; private set; }
        public string Id { get; private set; }
        public int Width { get; private set; }
        public string PhraseField { get; private set; }

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

        public abstract string Html { get; }
        public abstract string Js { get; }
        public abstract string GetValue { get; }
        public abstract string SetValidation { get; }

        public abstract void AssignValue(PostStatus status, JObject data, TObject obj);
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
            get { return Value.ToString(); }
        }

        public override string Js { get { return string.Empty; } }

        public override string GetValue
        {
            get { return string.Format("$(\"#{0}{1}\").val();", Form.Id, Id); }
        }

        public override string SetValidation
        {
            get { return string.Format("assignFieldValidation(\"{0}\", result);", Id); }
        }
    }
}

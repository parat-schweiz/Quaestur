using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Nancy;
using Nancy.ViewEngines;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public abstract class Widget<TObject> : IWidget<TObject>
        where TObject : DatabaseObject, new()
    {
        public Form Form { get; private set; }
        public string Id { get; private set; }
        public int Width { get; private set; }
        public bool Required { get; private set; }
        public string PhraseField { get; private set; }

        public string Classes
        {
            get
            {
                var list = new List<string>();
                if (Required)
                {
                    list.Add("required");
                }
                return string.Join(" ", list);
            }
        }

        public abstract string Html { get; }
        public abstract string Js { get; }
        public abstract string GetValue { get; }
        public abstract string SetValidation { get; }

        public Widget(Form form, string id, int width, bool required, string phraseField)
        {
            Form = form;
            Id = id;
            Width = width;
            Required = required;
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
}

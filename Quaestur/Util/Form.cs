using System;
using System.Collections.Generic;
using System.Linq;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public abstract class Form
    {
        public QuaesturModule Module { get; private set; }
        public string Id { get; private set; }
        public string Title { get; private set; }
        public string Text { get; private set; }
        public string ButtonOkId { get; private set; }
        public string PhraseButtonOk { get; private set; }
        public string SaveUrl { get; private set; }

        public Form(QuaesturModule module, string id, string title, string saveUrl, string phraseButtonOk, string text = null)
        {
            Module = module;
            Id = id;
            Title = title;
            Text = text;
            SaveUrl = saveUrl;
            ButtonOkId = id + "Ok";
            PhraseButtonOk = phraseButtonOk;
        }

        public abstract string Template { get; }

        public Negotiator Render()
        {
            return Module.View[Template, this];
        }
    }

    public abstract class Form<TObject> : Form
        where TObject : DatabaseObject, new()
    {
        private readonly List<IWidget<TObject>> _widgets;

        public IEnumerable<IWidget<TObject>> Widgets { get { return _widgets; } }
        public TObject Prototype { get; private set; }

        public Form(QuaesturModule module, string id, string title, string saveUrl, string phraseButtonOk, string text = null)
            : base(module, id, title, saveUrl, phraseButtonOk, text)
        {
            Prototype = new TObject();
            _widgets = new List<IWidget<TObject>>();
        }

        protected void Add(IWidget<TObject> widget)
        {
            _widgets.Add(widget);
        }

        public void SaveValues(PostStatus status, JObject data, TObject obj)
        {
            foreach (var widget in _widgets)
            {
                widget.SaveValue(status, data, obj);
            }
        }

        public void SaveObjects()
        { 
            foreach (var obj in _widgets
                .Select(w => w.UpdatedObject)
                .Where(o => o != null)
                .Distinct())
            {
                Module.Database.Save(obj);
            }
        }

        public void LoadValues(TObject obj)
        {
            foreach (var widget in _widgets)
            {
                widget.LoadValue(obj);
            }
        }
    }
}

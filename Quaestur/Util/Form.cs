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
            Id = id;
            Title = title;
            Text = text;
            SaveUrl = saveUrl;
            ButtonOkId = id + "Ok";
            PhraseButtonOk = phraseButtonOk;
        }

        public abstract void ClearUpdated();

        public abstract void Save();

        public abstract string Template { get; }

        public Negotiator Render()
        {
            return Module.View[Template, this];
        }
    }

    public abstract class SubFormHandler<TParent>
        where TParent : DatabaseObject, new()
    {
        public abstract void SaveSubForm(PostStatus status, JObject data, TParent obj);

        public abstract void LoadSubForm(TParent obj);

        public abstract void ClearUpdated();

        public abstract void Save();
    }

    public class SubFormHandler<TParent, TSub> : SubFormHandler<TParent>
        where TParent : DatabaseObject, new()
        where TSub : DatabaseObject, new()
    {
        private readonly Form<TSub> _form;
        private readonly Func<TParent, bool, TSub> _select;

        public SubFormHandler(Form<TSub> form, Func<TParent, bool, TSub> select)
        {
            _form = form;
            _select = select;
        }

        public override void SaveSubForm(PostStatus status, JObject data, TParent obj)
        {
            _form.SaveValues(status, data, _select(obj, true));
        }

        public override void ClearUpdated()
        {
            _form.ClearUpdated();
        }

        public override void LoadSubForm(TParent obj)
        {
            var sub = _select(obj, false);
            if (sub != null)
            {
                _form.LoadValues(sub);
            }
        }

        public override void Save()
        {
            _form.Save();
        }
    }

    public abstract class Form<TObject> : Form
        where TObject : DatabaseObject, new()
    {
        private readonly List<IWidget<TObject>> _widgets;

        public IEnumerable<IWidget<TObject>> Widgets { get { return _widgets; } }
        public TObject Prototype { get; private set; }

        public Form(QuaesturModule module, string id, string title, string saveUrl, string text = null)
            : base(module, id, title, saveUrl, text)
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

        public void LoadValues(TObject obj)
        {
            foreach (var widget in _widgets)
            {
                widget.LoadValue(obj);
            }
        }
    }
}

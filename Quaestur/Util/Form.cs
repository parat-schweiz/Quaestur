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

        public Form(QuaesturModule module, string id, string title, string text = null)
        {
            Id = id;
            Title = title;
            Text = text;
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
        public abstract void AssingSubForms(PostStatus status, JObject data, TParent obj);

        public abstract void ClearUpdated();

        public abstract void Save();
    }

    public class SubFormHandler<TParent, TSub> : SubFormHandler<TParent>
        where TParent : DatabaseObject, new()
        where TSub : DatabaseObject, new()
    {
        private readonly Form<TSub> _form;
        private readonly Func<TParent, TSub> _select;

        public SubFormHandler(Form<TSub> form, Func<TParent, TSub> select)
        {
            _form = form;
            _select = select;
        }

        public override void AssingSubForms(PostStatus status, JObject data, TParent obj)
        {
            _form.AssignValues(status, data, _select(obj));
        }

        public override void ClearUpdated()
        {
            _form.ClearUpdated();
        }

        public override void Save()
        {
            _form.Save();
        }
    }

    public abstract class Form<TObject> : Form
        where TObject : DatabaseObject, new()
    {
        private readonly List<Widget<TObject>> _widgets;
        private readonly List<DatabaseObject> _updatedObjects;
        private readonly List<SubFormHandler<TObject>> _subForms;

        public IEnumerable<Widget<TObject>> Widgets { get { return _widgets; } }
        public TObject Prototype { get; private set; }

        public Form(QuaesturModule module, string id, string title, string text = null)
            : base(module, id, title, text)
        {
            Prototype = new TObject();
            _widgets = new List<Widget<TObject>>();
            _updatedObjects = new List<DatabaseObject>();
            _subForms = CreateSubForms().ToList();
        }

        protected void Add(Widget<TObject> widget)
        {
            _widgets.Add(widget);
        }

        public override void ClearUpdated()
        {
            _updatedObjects.Clear();
            foreach (var sub in _subForms)
            {
                sub.ClearUpdated();
            }
        }

        public override void Save()
        {
            foreach (var obj in _updatedObjects)
            {
                Module.Database.Save(obj);
            }
            foreach (var sub in _subForms)
            {
                sub.Save();
            }
        }

        protected virtual IEnumerable<SubFormHandler<TObject>> CreateSubForms()
        {
            return new SubFormHandler<TObject>[0];
        }

        public void AssignValues(PostStatus status, JObject data, TObject obj)
        {
            foreach (var widget in _widgets)
            {
                widget.AssignValue(status, data, obj);
            }
            _updatedObjects.Add(obj);
            foreach (var sub in _subForms)
            {
                sub.AssingSubForms(status, data, obj);
            }
        }
    }
}

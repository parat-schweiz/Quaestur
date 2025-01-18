using System;
using System.IO;
using System.Text;
using Nancy;
using Nancy.ViewEngines;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public abstract class ClassWidget<TObject, TField, TValue> : Widget<TObject>
        where TObject : DatabaseObject, new()
        where TField : ValueField<TValue>
        where TValue : class
    {
        public TValue Value { get; set; }

        public ClassWidget(Form form, string id, int width, bool required, string phraseField)
            : base(form, id, width, required, phraseField)
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

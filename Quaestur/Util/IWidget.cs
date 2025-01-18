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
}

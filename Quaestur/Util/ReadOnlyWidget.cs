using System;
using Nancy;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class ReadOnlyWidget<TObject> : IWidget<TObject>
        where TObject : DatabaseObject, new()
    {
        public Form Form { get; private set; }
        public string Id { get; private set; }
        public int Width { get; private set; }
        public string PhraseField { get; private set; }

        public string Html => throw new NotImplementedException();

        public string Js => throw new NotImplementedException();

        public string GetValue => throw new NotImplementedException();

        public string SetValidation => throw new NotImplementedException();

        public DatabaseObject UpdatedObject => throw new NotImplementedException();

        public void LoadValue(TObject obj)
        {
            throw new NotImplementedException();
        }

        public void SaveValue(PostStatus status, JObject data, TObject obj)
        {
            throw new NotImplementedException();
        }
    }
}

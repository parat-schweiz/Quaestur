using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Nancy.Responses;
using Newtonsoft.Json;
using SiteLibrary;
using BaseLibrary;

namespace Quaestur
{
    public class ResourceModule : QuaesturModule
    {
        public ResourceModule()
        {
            Get("/headerimage", parameters =>
            {
                var systemWideFile = Database.Query<SystemWideFile>(DC.Equal("type", (int)SystemWideFileType.HeaderImage)).FirstOrDefault();
                if (systemWideFile != null)
                {
                    var stream = new MemoryStream(systemWideFile.Data);
                    return new StreamResponse(() => stream, systemWideFile.ContentType.Value);
                }
                else
                {
                    var imagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "images", "quaestur.png");
                    var stream = File.OpenRead(imagePath);
                    return new StreamResponse(() => stream, "image/png");
                }
            });
            Get("/favicon.svg", parameters =>
            {
                var systemWideFile = Database.Query<SystemWideFile>(DC.Equal("type", (int)SystemWideFileType.Favicon)).FirstOrDefault();
                if (systemWideFile != null)
                {
                    if (systemWideFile.ContentType.Value == "image/svg+xml")
                    {
                        var stream = new MemoryStream(systemWideFile.Data);
                        return new StreamResponse(() => stream, systemWideFile.ContentType.Value);
                    }
                }

                return new TextResponse(HttpStatusCode.NotFound, "File not found");
            });
        }
    }
}

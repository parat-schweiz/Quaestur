using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.Responses;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Scriba
{
    public class PadViewModel
    {
        public Guid Id { get; private set; }

        public PadViewModel(Pad pad)
        {
            Id = pad.Id; 
        }
    }

    public class PadModule : ScribaModule
    {
        public PadModule()
        {
            //this.RequiresAuthentication();

            Get("/pad/new", parameters =>
            {
                var pad = new Pad(Guid.NewGuid());
                Global.Pads.Add(pad);
                return Response.AsRedirect("/pad/" + pad.Id.ToString());
            });
            Get("/pad/{id}", parameters =>
            {
                string idString = parameters.id;

                if (Guid.TryParse(idString, out Guid id))
                {
                    using (var padLock = Global.Pads.Get(id))
                    {
                        if (padLock.Pad != null)
                        {
                            return View["View/pad.sshtml", new PadViewModel(padLock.Pad)];
                        }
                    }
                }

                return new NotFoundResponse();
            });
            Get("/pad/{id}/get/{index}", parameters =>
            {
                string idString = parameters.id;
                string indexString = parameters.index;

                if (Guid.TryParse(idString, out Guid id) &&
                    int.TryParse(indexString, out int index))
                {
                    var start = DateTime.UtcNow;

                    while (DateTime.UtcNow.Subtract(start).TotalSeconds < 30)
                    {
                        using (var padLock = Global.Pads.Get(id))
                        {
                            if (padLock.Pad != null)
                            {
                                if (padLock.Pad.HasChanges(index))
                                {
                                    return new TextResponse(padLock.Pad.Changes(index).ToString(), "application/json");
                                }
                            }
                            else
                            {
                                return new NotFoundResponse();
                            }
                        }
                    }

                    return new TextResponse(new JArray().ToString(), "application/json");
                }
                else
                {
                    return new NotFoundResponse();
                }
            });
            Post("/pad/{id}/change", parameters =>
            {
                string idString = parameters.id;

                if (Guid.TryParse(idString, out Guid id))
                {
                    using (var padLock = Global.Pads.Get(id))
                    {
                        if (padLock.Pad != null)
                        {
                            padLock.Pad.Add(new Change(JObject.Parse(ReadBody())));
                        }
                    }
                }

                return null;
            });
        }
    }
}

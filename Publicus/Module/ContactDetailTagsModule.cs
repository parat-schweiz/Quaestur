using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailTagsViewModel
    {
    }

    public class ContactDetailTagsModule : PublicusModule
    {
        public ContactDetailTagsModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/tags/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.TagAssignments, AccessRight.Read))
                    {
                        return View["View/contactdetail_tags.sshtml", new ContactDetailTagsViewModel()];
                    }
                }

                return null;
            };
        }
    }
}

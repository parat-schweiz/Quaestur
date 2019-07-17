using System;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Quaestur
{
    public class PersonDetailTagsViewModel
    {
    }

    public class PersonDetailTagsModule : QuaesturModule
    {
        public PersonDetailTagsModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/tags/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.TagAssignments, AccessRight.Read))
                    {
                        return View["View/persondetail_tags.sshtml", new PersonDetailTagsViewModel()];
                    }
                }

                return string.Empty;
            });
        }
    }
}

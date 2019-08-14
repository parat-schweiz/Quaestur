using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class TagAssignmentEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Tag;
        public List<NamedIdViewModel> Tags;
        public string PhraseFieldTag;

        public TagAssignmentEditViewModel()
        { 
        }

        public TagAssignmentEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("TagAssignment.Edit.Title", "Title of the edit tagAssignment dialog", "Edit tag assignment"), 
                   "tagAssignmentEditDialog")
        {
            PhraseFieldTag = translator.Get("TagAssignment.Edit.Field.Tag", "Field 'Tag' in the edit tagAssignment dialog", "Tag").EscapeHtml();
            Tags = new List<NamedIdViewModel>();
        }

        public TagAssignmentEditViewModel(Translator translator, IDatabase db, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            Tag = string.Empty;
            Tags.AddRange(
                db.Query<Tag>()
                .Where(t => t.Mode.Value.HasFlag(TagMode.Manual))
                .Where(t => !person.TagAssignments.Any(a => a.Tag == t))
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
        }
    }

    public class TagAssignmentEdit : QuaesturModule
    {
        public TagAssignmentEdit()
        {
            RequireCompleteLogin();

            Get("/tagassignment/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.TagAssignments, AccessRight.Write))
                    {
                        return View["View/tagAssignmentedit.sshtml",
                            new TagAssignmentEditViewModel(Translator, Database, person)];
                    }
                }

                return null;
            });
            Post("/tagassignment/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<TagAssignmentEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.TagAssignments, AccessRight.Write))
                    {
                        var tagAssignment = new TagAssignment(Guid.NewGuid());
                        status.AssignObjectIdString("Tag", tagAssignment.Tag, model.Tag);
                        tagAssignment.Person.Value = person;

                        if (status.IsSuccess)
                        {
                            Database.Save(tagAssignment);
                            Journal(tagAssignment.Person.Value,
                                "TagAssignment.Journal.Add",
                                "Journal entry added tag",
                                "Added tag {0}",
                                t => tagAssignment.Tag.Value.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/tagassignment/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var tagAssignment = Database.Query<TagAssignment>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(tagAssignment))
                {
                    if (status.HasAccess(tagAssignment.Person.Value, PartAccess.TagAssignments, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            tagAssignment.Delete(Database);

                            Journal(tagAssignment.Person.Value,
                                "TagAssignment.Journal.Delete",
                                "Journal entry removed tag",
                                "Remvoed tag {0}",
                                t => tagAssignment.Tag.Value.GetText(t));

                            transaction.Commit();
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
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

        public TagAssignmentEditViewModel(Translator translator, IDatabase db, Contact contact)
            : this(translator)
        {
            Method = "add";
            Id = contact.Id.ToString();
            Tag = string.Empty;
            Tags.AddRange(
                db.Query<Tag>()
                .Where(t => t.Mode.Value.HasFlag(TagMode.Manual))
                .Where(t => !contact.TagAssignments.Any(a => a.Tag == t))
                .Select(t => new NamedIdViewModel(translator, t, false))
                .OrderBy(t => t.Name));
        }
    }

    public class TagAssignmentEdit : PublicusModule
    {
        public TagAssignmentEdit()
        {
            this.RequiresAuthentication();

            Get["/tagassignment/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.TagAssignments, AccessRight.Write))
                    {
                        return View["View/tagAssignmentedit.sshtml",
                            new TagAssignmentEditViewModel(Translator, Database, contact)];
                    }
                }

                return null;
            };
            Post["/tagassignment/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<TagAssignmentEditViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    if (status.HasAccess(contact, PartAccess.TagAssignments, AccessRight.Write))
                    {
                        var tagAssignment = new TagAssignment(Guid.NewGuid());
                        status.AssignObjectIdString("Tag", tagAssignment.Tag, model.Tag);
                        tagAssignment.Contact.Value = contact;

                        if (status.IsSuccess)
                        {
                            Database.Save(tagAssignment);
                            Journal(tagAssignment.Contact.Value,
                                "TagAssignment.Journal.Add",
                                "Journal entry added tag",
                                "Added tag {0}",
                                t => tagAssignment.Tag.Value.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/tagassignment/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var tagAssignment = Database.Query<TagAssignment>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(tagAssignment))
                {
                    if (status.HasAccess(tagAssignment.Contact.Value, PartAccess.TagAssignments, AccessRight.Write))
                    {
                        Database.Delete(tagAssignment);
                        Journal(tagAssignment.Contact.Value,
                            "TagAssignment.Journal.Delete",
                            "Journal entry removed tag",
                            "Remvoed tag {0}",
                            t => tagAssignment.Tag.Value.GetText(t));
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

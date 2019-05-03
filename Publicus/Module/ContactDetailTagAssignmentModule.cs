using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class ContactDetailTagAssignmentItemViewModel
    {
        public string Id;
        public string Name;
        public string Usage;

        private static string GetText(Translator translator, TagUsage usage)
        {
            var list = new List<string>();

            if (usage.HasFlag(TagUsage.Mailing))
            {
                list.Add(translator.Get("TagAssignment.Usage.Mailing", "Tag usage flag 'Mailing'", "Mailing"));
            }

            return string.Join(", ", list);
        }

        public ContactDetailTagAssignmentItemViewModel(Translator translator, TagAssignment tagAssignment)
        {
            Id = tagAssignment.Id.Value.ToString();
            Name = tagAssignment.Tag.Value.Name.Value[translator.Language].EscapeHtml();
            Usage = GetText(translator, tagAssignment.Tag.Value.Usage.Value).EscapeHtml();
        }
    }

    public class ContactDetailTagAssignmentViewModel
    {
        public string Id;
        public string Editable;
        public List<ContactDetailTagAssignmentItemViewModel> List;
        public string PhraseHeaderName;
        public string PhraseHeaderUsage;

        public ContactDetailTagAssignmentViewModel(Translator translator, Session session, Contact contact)
        {
            Id = contact.Id.Value.ToString();
            List = new List<ContactDetailTagAssignmentItemViewModel>(
                contact.TagAssignments
                .Select(m => new ContactDetailTagAssignmentItemViewModel(translator, m))
                .OrderBy(m => m.Name));
            Editable =
                session.HasAccess(contact, PartAccess.TagAssignments, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderName = translator.Get("Contact.Detail.TagAssignment.Header.Name", "Column 'Name' on the tagAssignment tab of the contact detail page", "Name").EscapeHtml();
            PhraseHeaderUsage = translator.Get("Contact.Detail.TagAssignment.Header.Usage", "Column 'Usage' on the tagAssignment tab of the contact detail page", "Usage").EscapeHtml();
        }
    }

    public class ContactDetailTagAssignmentModule : PublicusModule
    {
        public ContactDetailTagAssignmentModule()
        {
            this.RequiresAuthentication();

            Get["/contact/detail/tagassignments/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.TagAssignments, AccessRight.Read))
                    {
                        return View["View/contactdetail_tagassignments.sshtml", 
                            new ContactDetailTagAssignmentViewModel(Translator, CurrentSession, contact)];
                    }
                }

                return null;
            };
        }
    }
}

using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class PersonDetailTagAssignmentItemViewModel
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

        public PersonDetailTagAssignmentItemViewModel(Translator translator, TagAssignment tagAssignment)
        {
            Id = tagAssignment.Id.Value.ToString();
            Name = tagAssignment.Tag.Value.Name.Value[translator.Language].EscapeHtml();
            Usage = GetText(translator, tagAssignment.Tag.Value.Usage.Value).EscapeHtml();
        }
    }

    public class PersonDetailTagAssignmentViewModel
    {
        public string Id;
        public string Editable;
        public List<PersonDetailTagAssignmentItemViewModel> List;
        public string PhraseHeaderName;
        public string PhraseHeaderUsage;

        public PersonDetailTagAssignmentViewModel(Translator translator, Session session, Person person)
        {
            Id = person.Id.Value.ToString();
            List = new List<PersonDetailTagAssignmentItemViewModel>(
                person.TagAssignments
                .Select(m => new PersonDetailTagAssignmentItemViewModel(translator, m))
                .OrderBy(m => m.Name));
            Editable =
                session.HasAccess(person, PartAccess.TagAssignments, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseHeaderName = translator.Get("Person.Detail.TagAssignment.Header.Name", "Column 'Name' on the tagAssignment tab of the person detail page", "Name").EscapeHtml();
            PhraseHeaderUsage = translator.Get("Person.Detail.TagAssignment.Header.Usage", "Column 'Usage' on the tagAssignment tab of the person detail page", "Usage").EscapeHtml();
        }
    }

    public class PersonDetailTagAssignmentModule : QuaesturModule
    {
        public PersonDetailTagAssignmentModule()
        {
            RequireCompleteLogin();

            Get("/person/detail/tagassignments/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.TagAssignments, AccessRight.Read))
                    {
                        return View["View/persondetail_tagassignments.sshtml", 
                            new PersonDetailTagAssignmentViewModel(Translator, CurrentSession, person)];
                    }
                }

                return null;
            });
        }
    }
}

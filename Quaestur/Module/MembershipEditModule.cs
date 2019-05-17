using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using SiteLibrary;

namespace Quaestur
{
    public class MembershipEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Organization;
        public string MembershipType;
        public string StartDate;
        public string EndDate;
        public List<NamedIdViewModel> Organizations;
        public string PhraseFieldOrganization;
        public string PhraseFieldStartDate;
        public string PhraseFieldEndDate;

        public MembershipEditViewModel()
        { 
        }

        public MembershipEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("Membership.Edit.Title", "Title of the edit membership dialog", "Edit membership"), 
                   "membershipEditDialog")
        {
            PhraseFieldOrganization = translator.Get("Membership.Edit.Field.Organization", "Field 'Organization' in the edit membership dialog", "Organization").EscapeHtml();
            PhraseFieldStartDate = translator.Get("Membership.Edit.Field.StartDate", "Field 'Start date' in the edit membership dialog", "Start date").EscapeHtml();
            PhraseFieldEndDate = translator.Get("Membership.Edit.Field.EndDate", "Field 'End date' in the edit membership dialog", "End date").EscapeHtml();
            Organizations = new List<NamedIdViewModel>();
        }

        public MembershipEditViewModel(Translator translator, IDatabase db, Person person)
            : this(translator)
        {
            Method = "add";
            Id = person.Id.ToString();
            Organization = string.Empty;
            StartDate = string.Empty;
            EndDate = string.Empty;
            Organizations.AddRange(
                db.Query<Organization>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
        }

        public MembershipEditViewModel(Translator translator, IDatabase db, Membership membership)
            : this(translator)
        {
            Method = "edit";
            Id = membership.Id.ToString();
            Organization = membership.Organization.Value.Name.Value[translator.Language].EscapeHtml();
            StartDate = membership.StartDate.Value.ToString("dd.MM.yyyy");
            EndDate =
                membership.EndDate.Value.HasValue ?
                membership.EndDate.Value.Value.ToString("dd.MM.yyyy") :
                string.Empty;
            Organizations.AddRange(
                db.Query<Organization>()
                .Select(o => new NamedIdViewModel(translator, o, o == membership.Organization))
                .OrderBy(o => o.Name));
        }
    }

    public class MembershipTypesListViewModel
    {
        public string PhraseFieldMembershipType;
        public List<NamedIdViewModel> MembershipTypes;

        public MembershipTypesListViewModel(Translator translator, IDatabase db, Organization organization, Membership membership)
        {
            PhraseFieldMembershipType = translator.Get("Membership.Edit.Field.MembershipType", "Field 'Type' in the edit membership dialog", "Type").EscapeHtml();
            var membershipTypeId = membership != null ? membership.Type.Value.Id.Value : Guid.Empty;
            MembershipTypes = new List<NamedIdViewModel>(organization.MembershipTypes
                .Select(mt => new NamedIdViewModel(translator, mt, mt.Id.Value == membershipTypeId))
                .OrderBy(mt => mt.Name));
        }
    }

    public class MembershipEdit : QuaesturModule
    {
        public MembershipEdit()
        {
            RequireCompleteLogin();

            Get("/membership/edit/{mid}/types/{oid}", parameters =>
            {
                var membership = Database.Query<Membership>((string)parameters.mid);
                var organization = Database.Query<Organization>((string)parameters.oid);

                if (organization != null)
                {
                    if (HasAccess(organization, PartAccess.Membership, AccessRight.Read))
                    {
                        return View["View/membershipedit_types.sshtml",
                            new MembershipTypesListViewModel(Translator, Database, organization, membership)];
                    }
                }

                return null;
            });
            Get("/membership/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);

                if (membership != null)
                {
                    if (HasAccess(membership.Person.Value, PartAccess.Membership, AccessRight.Write))
                    {
                        return View["View/membershipedit.sshtml",
                            new MembershipEditViewModel(Translator, Database, membership)];
                    }
                }

                return null;
            });
            Post("/membership/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MembershipEditViewModel>(ReadBody());
                var membership = Database.Query<Membership>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membership))
                {
                    if (status.HasAccess(membership.Person.Value, PartAccess.Membership, AccessRight.Write))
                    {
                        status.AssignObjectIdString("Organization", membership.Organization, model.Organization);
                        status.AssignObjectIdString("MembershipType", membership.Type, model.MembershipType);
                        status.AssignDateString("StartDate", membership.StartDate, model.StartDate);
                        status.AssignDateString("EndDate", membership.EndDate, model.EndDate);

                        if (status.IsSuccess)
                        {
                            membership.UpdateVotingRight(Database);
                            Database.Save(membership);
                            Journal(membership.Person.Value,
                                "Membership.Journal.Edit",
                                "Journal entry edited membership",
                                "Change membership {0}",
                                t => membership.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/membership/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var person = Database.Query<Person>(idString);

                if (person != null)
                {
                    if (HasAccess(person, PartAccess.Membership, AccessRight.Write))
                    {
                        return View["View/membershipedit.sshtml",
                            new MembershipEditViewModel(Translator, Database, person)];
                    }
                }

                return null;
            });
            Post("/membership/add/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<MembershipEditViewModel>(ReadBody());
                var person = Database.Query<Person>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(person))
                {
                    if (status.HasAccess(person, PartAccess.Membership, AccessRight.Write))
                    {
                        var membership = new Membership(Guid.NewGuid());
                        status.AssignObjectIdString("Organization", membership.Organization, model.Organization);
                        status.AssignObjectIdString("MembershipType", membership.Type, model.MembershipType);
                        status.AssignDateString("StartDate", membership.StartDate, model.StartDate);
                        status.AssignDateString("EndDate", membership.EndDate, model.EndDate);
                        membership.Person.Value = person;

                        if (status.IsSuccess)
                        {
                            membership.UpdateVotingRight(Database);
                            Database.Save(membership);
                            Journal(membership.Person.Value,
                                "Membership.Journal.Add",
                                "Journal entry addded membership",
                                "Added membership {0}",
                                t => membership.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/membership/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var membership = Database.Query<Membership>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(membership))
                {
                    if (status.HasAccess(membership.Person.Value, PartAccess.Membership, AccessRight.Write))
                    {
                        membership.Delete(Database);
                        Journal(membership.Person.Value,
                            "Membership.Journal.Delete",
                            "Journal entry removed membership",
                            "Removed membership {0}",
                            t => membership.GetText(t));
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

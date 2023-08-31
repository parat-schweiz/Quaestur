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
    public class OrganizationEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public List<MultiItemViewModel> Name;
        public string Parent;
        public List<NamedIdViewModel> Parents;
        public string PhraseFieldParent;
        public List<MultiItemViewModel> BillName;
        public List<MultiItemViewModel> BillStreet;
        public List<MultiItemViewModel> BillLocation;
        public string BillCountry;
        public List<NamedIdViewModel> BillCountries;
        public string PhraseFieldBillCountry;
        public string BillIban;
        public string PhraseFieldBillIban;

        public OrganizationEditViewModel()
        { 
        }

        public OrganizationEditViewModel(Translator translator)
            : base(translator, translator.Get("Organization.Edit.Title", "Title of the organization edit dialog", "Edit organization"), "organizationEditDialog")
        {
            PhraseFieldParent = translator.Get("Organization.Edit.Field.Parent", "Parent field in the organization edit dialog", "Parent").EscapeHtml();
            PhraseFieldBillCountry = translator.Get("Organization.Edit.Field.BillCountry", "Bill country field in the organization edit dialog", "Bill country").EscapeHtml();
            PhraseFieldBillIban = translator.Get("Organization.Edit.Field.BillIban", "Bill IBAN field in the organization edit dialog", "Bill IBAN").EscapeHtml();
        }

        public OrganizationEditViewModel(Translator translator, IDatabase db)
            : this(translator)
        {
            Method = "add";
            Id = "new";
            Name = translator.CreateLanguagesMultiItem("Organization.Edit.Field.Name", "Name field in the organization edit dialog", "Name ({0})", new MultiLanguageString());
            BillName = translator.CreateLanguagesMultiItem("Organization.Edit.Field.BillName", "Bill name field in the organization edit dialog", "Bill name ({0})", new MultiLanguageString());
            BillStreet = translator.CreateLanguagesMultiItem("Organization.Edit.Field.BillStreet", "Bill street address line field in the organization edit dialog", "Bill street address line ({0})", new MultiLanguageString());
            BillLocation = translator.CreateLanguagesMultiItem("Organization.Edit.Field.BillLocation", "Bill location including postal code field in the organization edit dialog", "Bill location including postal code ({0})", new MultiLanguageString());
            BillCountry = string.Empty;
            BillIban = string.Empty;
            Parent = string.Empty;
            Parents = new List<NamedIdViewModel>(
                db.Query<Organization>()
                .Select(o => new NamedIdViewModel(translator, o, false))
                .OrderBy(o => o.Name));
            Parents.Add(new NamedIdViewModel(
                translator.Get("Organization.Edit.Field.Parent.None", "No value in the field 'Parent' in the organization edit dialog", "<None>"),
                false, true));
            BillCountries = new List<NamedIdViewModel>(db
                .Query<Country>()
                .Select(c => new NamedIdViewModel(translator, c, false)));
        }

        public OrganizationEditViewModel(Translator translator, IDatabase db, Organization organization)
            : this(translator)
        {
            Method = "edit";
            Id = organization.Id.Value.ToString();
            Name = translator.CreateLanguagesMultiItem("Organization.Edit.Field.Name", "Name field in the organization edit dialog", "Name ({0})", organization.Name.Value);
            BillName = translator.CreateLanguagesMultiItem("Organization.Edit.Field.BillName", "Bill name field in the organization edit dialog", "Bill name ({0})", organization.BillName.Value);
            BillStreet = translator.CreateLanguagesMultiItem("Organization.Edit.Field.BillStreet", "Bill street address line field in the organization edit dialog", "Bill street address line ({0})", organization.BillStreet.Value);
            BillLocation = translator.CreateLanguagesMultiItem("Organization.Edit.Field.BillLocation", "Bill location including postal code field in the organization edit dialog", "Bill location including postal code ({0})", organization.BillLocation.Value);
            BillCountry =
                organization.BillCountry.Value != null ?
                organization.BillCountry.Value.Id.Value.ToString() :
                string.Empty;
            BillIban = organization.BillIban.Value;
            Parent =
                organization.Parent.Value != null ?
                organization.Parent.Value.Id.Value.ToString() :
                string.Empty;
            Parents = new List<NamedIdViewModel>(
                db.Query<Organization>()
                .Where(o => !organization.Subordinates.Contains(o))
                .Where(o => organization != o)
                .Select(o => new NamedIdViewModel(translator, o, o == organization.Parent.Value))
                .OrderBy(o => o.Name));
            Parents.Add(new NamedIdViewModel(
                translator.Get("Organization.Edit.Field.Parent.None", "No value in the field 'Parent' in the organization edit dialog", "<None>"),
                false, organization.Parent.Value == null));
            BillCountries = new List<NamedIdViewModel>(db
                .Query<Country>()
                .Select(c => new NamedIdViewModel(translator, c, organization.BillCountry.Value == c)));
        }
    }

    public class OrganizationViewModel : MasterViewModel
    {
        public OrganizationViewModel(IDatabase database, Translator translator, Session session)
            : base(database, translator, 
            translator.Get("Organization.List.Title", "Title of the organization list page", "Organizations"), 
            session)
        { 
        }
    }

    public class OrganizationListItemViewModel
    {
        public string Id;
        public string Name;
        public string Indent;
        public string Width;
        public string Editable;
        public string PhraseDeleteConfirmationQuestion;

        public OrganizationListItemViewModel(Translator translator, Session session, Organization organization, int indent)
        {
            Id = organization.Id.Value.ToString();
            Name = organization.Name.Value[translator.Language].EscapeHtml();
            Indent = indent.ToString() + "%";
            Width = (70 - indent).ToString() + "%";
            Editable =
                session.HasAccess(organization, PartAccess.Structure, AccessRight.Write) ?
                "editable" : "accessdenied";
            PhraseDeleteConfirmationQuestion = translator.Get("Organization.List.Delete.Confirm.Question", "Delete organization confirmation question", "Do you really wish to delete organization {0}?", organization.GetText(translator)).EscapeHtml();
        }
    }

    public class OrganizationListViewModel
    {
        public string PhraseHeaderName;
        public string PhraseHeaderMembershipTypes;
        public string PhraseHeaderGroups;
        public string PhraseDeleteConfirmationTitle;
        public string PhraseDeleteConfirmationInfo;
        public List<OrganizationListItemViewModel> List;
        public bool AddAccess;

        private void AddRecursive(Translator translator, Session session, Organization organization, int indent)
        {
            AddAccess = session.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write);
            int addIndent = 0;

            if (session.HasAccess(organization, PartAccess.Structure, AccessRight.Read))
            {
                List.Add(new OrganizationListItemViewModel(translator, session, organization, indent));
                addIndent = 5;
            }

            foreach (var child in organization.Children)
            {
                AddRecursive(translator, session, child, indent + addIndent);
            }
        }

        public OrganizationListViewModel(Translator translator, Session session, IDatabase database)
        {
            PhraseHeaderName = translator.Get("Organization.List.Header.Name", "Column 'Name' in the organization list", "Name").EscapeHtml();
            PhraseHeaderMembershipTypes = translator.Get("Organization.List.Header.MembershipTypes", "Column 'Membership types' in the organization list", "Memberships").EscapeHtml();
            PhraseHeaderGroups = translator.Get("Organization.List.Header.Groups", "Column 'Groups' in the organization list", "Groups").EscapeHtml();
            PhraseDeleteConfirmationTitle = translator.Get("Organization.List.Delete.Confirm.Title", "Delete organization confirmation title", "Delete?").EscapeHtml();
            PhraseDeleteConfirmationInfo = translator.Get("Organization.List.Delete.Confirm.Info", "Delete organization confirmation info", "This will also delete all memberships, groups, roles, permissions and mailings under that organization.").EscapeHtml();
            List = new List<OrganizationListItemViewModel>();
            var organizations = database.Query<Organization>();

            foreach (var organization in organizations
                .Where(o => o.Parent.Value == null)
                .OrderBy(o => o.Name.Value[translator.Language]))
            {
                AddRecursive(translator, session, organization, 0);
            }
        }
    }

    public class OrganizationEdit : QuaesturModule
    {
        public OrganizationEdit()
        {
            RequireCompleteLogin();

            Get("/organization", parameters =>
            {
                return View["View/organization.sshtml",
                    new OrganizationViewModel(Database, Translator, CurrentSession)];
            });
            Get("/organization/list", parameters =>
            {
                return View["View/organizationlist.sshtml",
                    new OrganizationListViewModel(Translator, CurrentSession, Database)];
            });
            Get("/organization/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);

                if (organization != null)
                {
                    if (HasAccess(organization, PartAccess.Structure, AccessRight.Write))
                    {
                        return View["View/organizationedit.sshtml",
                            new OrganizationEditViewModel(Translator, Database, organization)];
                    }
                }

                return string.Empty;
            });
            Post("/organization/edit/{id}", parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<OrganizationEditViewModel>(ReadBody());
                var organization = Database.Query<Organization>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(organization))
                {
                    if (status.HasAccess(organization, PartAccess.Structure, AccessRight.Write))
                    {
                        status.AssignMultiLanguageRequired("Name", organization.Name, model.Name);
                        status.AssignMultiLanguageRequired("BillName", organization.BillName, model.BillName);
                        status.AssignMultiLanguageRequired("BillStreet", organization.BillStreet, model.BillStreet);
                        status.AssignMultiLanguageRequired("BillLocation", organization.BillLocation, model.BillLocation);
                        status.AssignObjectIdString("BillCountry", organization.BillCountry, model.BillCountry);
                        status.AssignStringFree("BillIban", organization.BillIban, model.BillIban);
                        status.AssignObjectIdString("Parent", organization.Parent, model.Parent);

                        if (status.IsSuccess)
                        {
                            Database.Save(organization);
                            Notice("{0} changed organization {1}", CurrentSession.User.ShortHand, organization);
                        }
                    }
                }

                return status.CreateJsonData();
            });
            Get("/organization/add", parameters =>
            {
                if (HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    return View["View/organizationedit.sshtml",
                        new OrganizationEditViewModel(Translator, Database)];
                }
                return string.Empty;
            });
            Post("/organization/add/new", parameters =>
            {
                var model = JsonConvert.DeserializeObject<OrganizationEditViewModel>(ReadBody());
                var status = CreateStatus();

                if (status.HasSystemWideAccess(PartAccess.Structure, AccessRight.Write))
                {
                    string idString = parameters.id;
                    var organization = new Organization(Guid.NewGuid());
                    status.AssignMultiLanguageRequired("Name", organization.Name, model.Name);
                    status.AssignMultiLanguageRequired("BillName", organization.BillName, model.BillName);
                    status.AssignMultiLanguageRequired("BillStreet", organization.BillStreet, model.BillStreet);
                    status.AssignMultiLanguageRequired("BillLocation", organization.BillLocation, model.BillLocation);
                    status.AssignObjectIdString("BillCountry", organization.BillCountry, model.BillCountry);
                    status.AssignStringFree("BillIban", organization.BillIban, model.BillIban);
                    status.AssignObjectIdString("Parent", organization.Parent, model.Parent);

                    if (status.IsSuccess)
                    {
                        Database.Save(organization);
                        Notice("{0} added organization {1}", CurrentSession.User.ShortHand, organization);
                    }
                }

                return status.CreateJsonData();
            });
            Get("/organization/delete/{id}", parameters =>
            {
                string idString = parameters.id;
                var organization = Database.Query<Organization>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(organization))
                {
                    if (status.HasAccess(organization, PartAccess.Structure, AccessRight.Write))
                    {
                        using (var transaction = Database.BeginTransaction())
                        {
                            organization.Delete(Database);
                            transaction.Commit();
                            Notice("{0} deleted organization {1}", CurrentSession.User.ShortHand, organization);
                        }
                    }
                }

                return status.CreateJsonData();
            });
        }
    }
}

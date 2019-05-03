using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;

namespace Publicus
{
    public class SwitchViewModel
    {
        public string SourceId;
        public string TargetId;
    }

    public class PostalAddressEditViewModel : DialogViewModel
    {
        public string Method;
        public string Id;
        public string Street;
        public string CareOf;
        public string PostOfficeBox;
        public string Place;
        public string PostalCode;
        public string State;
        public string Country;
        public List<NamedIdViewModel> States;
        public List<NamedIdViewModel> Countries;
        public string PhraseFieldCareOf;
        public string PhraseFieldStreet;
        public string PhraseFieldPostOfficeBox;
        public string PhraseFieldPostalCode;
        public string PhraseFieldPlace;
        public string PhraseFieldState;
        public string PhraseFieldCountry;

        public PostalAddressEditViewModel()
        { 
        }

        public PostalAddressEditViewModel(Translator translator)
            : base(translator, 
                   translator.Get("PostalAddress.Edit.Title", "Title of the postal address edit dialog", "Edit postal address"), 
                   "postalAddressEditDialog")
        {
            PhraseFieldCareOf = translator.Get("PostalAddress.Edit.Field.CareOf", "Field 'CareOf' in the postal address edit dialog", "c /o").EscapeHtml();
            PhraseFieldStreet = translator.Get("PostalAddress.Edit.Field.Street", "Field 'Street' in the postal address edit dialog", "Street").EscapeHtml();
            PhraseFieldPostOfficeBox = translator.Get("PostalAddress.Edit.Field.PostOfficeBox", "Field 'PostOfficeBox' in the postal address edit dialog", "P.O. Box").EscapeHtml();
            PhraseFieldPostalCode = translator.Get("PostalAddress.Edit.Field.PostalCode", "Field 'PostalCode' in the postal address edit dialog", "Postal Code").EscapeHtml();
            PhraseFieldPlace = translator.Get("PostalAddress.Edit.Field.Place", "Field 'Place' in the postal address edit dialog", "Place").EscapeHtml();
            PhraseFieldState = translator.Get("PostalAddress.Edit.Field.State", "Field 'State' in the postal address edit dialog", "State").EscapeHtml();
            PhraseFieldCountry = translator.Get("PostalAddress.Edit.Field.Country", "Field 'Country' in the postal address edit dialog", "Country").EscapeHtml();
        }

        public PostalAddressEditViewModel(Translator translator, IDatabase db, Contact contact)
            : this(translator)
        {
            Method = "add";
            Id = contact.Id.ToString();
            Street = string.Empty;
            CareOf = string.Empty;
            PostOfficeBox = string.Empty;
            Place = string.Empty;
            PostalCode = string.Empty;
            State = string.Empty;
            Country = string.Empty;
            States = new List<NamedIdViewModel>(
                db.Query<State>()
                .Select(s => new NamedIdViewModel(translator, s, false))
                .OrderBy(s => s.Name));
            States.Add(new NamedIdViewModel(
                translator.Get("PostalAddress.Edit.Field.State.None", "No value in the field 'State' in the postal address edit dialog", "<None>"), 
                false, true));
            Countries = new List<NamedIdViewModel>(
                db.Query<Country>()
                .Select(c => new NamedIdViewModel(translator, c, false))
                .OrderBy(c => c.Name));
            Countries.First().Selected = true;
        }

        public PostalAddressEditViewModel(Translator translator, IDatabase db, PostalAddress address)
            : this(translator)
        {
            Method = "edit";
            Id = address.Id.ToString();
            Street = address.Street.Value.EscapeHtml();
            CareOf = address.CareOf.Value.EscapeHtml();
            PostOfficeBox = address.PostOfficeBox.Value.EscapeHtml();
            Place = address.Place.Value.EscapeHtml();
            PostalCode = address.PostalCode.Value.EscapeHtml();
            State = string.Empty;
            Country = string.Empty;
            States = new List<NamedIdViewModel>(
                db.Query<State>()
                .Select(s => new NamedIdViewModel(translator, s, address.State.Value == s))
                .OrderBy(s => s.Name));
            States.Add(new NamedIdViewModel(
                translator.Get("PostalAddress.Edit.Field.State.None", "No value in the field 'State' in the postal address edit dialog", "<None>"),
                false, address.State.Value == null));
            Countries = new List<NamedIdViewModel>(
                db.Query<Country>()
                .Select(c => new NamedIdViewModel(translator, c, address.Country.Value == c))
                .OrderBy(c => c.Name));
        }
    }

    public class PostalAddressEdit : PublicusModule
    {
        public PostalAddressEdit()
        {
            this.RequiresAuthentication();

            Get["/postaladdress/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var address = Database.Query<PostalAddress>(idString);

                if (address != null)
                {
                    if (HasAccess(address.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        return View["View/postaladdressedit.sshtml",
                            new PostalAddressEditViewModel(Translator, Database, address)];
                    }
                }

                return null;
            };
            Post["/postaladdress/edit/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PostalAddressEditViewModel>(ReadBody());
                var address = Database.Query<PostalAddress>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(address))
                {
                    if (status.HasAccess(address.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        status.AssignStringFree("CareOf", address.CareOf, model.CareOf);
                        status.AssignStringFree("Street", address.Street, model.Street);
                        status.AssignStringFree("PostOfficeBox", address.PostOfficeBox, model.PostOfficeBox);
                        status.AssignStringFree("CareOf", address.PostalCode, model.PostalCode);
                        status.AssignStringFree("PostalCode", address.Place, model.Place);
                        status.AssignObjectIdString("State", address.State, model.State);
                        status.AssignObjectIdString("Country", address.Country, model.Country);

                        if (status.IsSuccess)
                        {
                            Database.Save(address);
                            Journal(address.Contact,
                                "PostalAddress.Journal.Edit",
                                "Journal entry changed postal address",
                                "Changed postal address {0}",
                                t => address.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/postaladdress/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var contact = Database.Query<Contact>(idString);

                if (contact != null)
                {
                    if (HasAccess(contact, PartAccess.Contact, AccessRight.Write))
                    {
                        return View["View/postaladdressedit.sshtml",
                            new PostalAddressEditViewModel(Translator, Database, contact)];
                    }
                }

                return null;
            };
            Post["/postaladdress/add/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var model = JsonConvert.DeserializeObject<PostalAddressEditViewModel>(ReadBody());
                var contact = Database.Query<Contact>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(contact))
                {
                    if (status.HasAccess(contact, PartAccess.Contact, AccessRight.Write))
                    {
                        var address = new PostalAddress(Guid.NewGuid());
                        status.AssignStringFree("CareOf", address.CareOf, model.CareOf);
                        status.AssignStringFree("Street", address.Street, model.Street);
                        status.AssignStringFree("PostOfficeBox", address.PostOfficeBox, model.PostOfficeBox);
                        status.AssignStringFree("CareOf", address.PostalCode, model.PostalCode);
                        status.AssignStringFree("PostalCode", address.Place, model.Place);
                        status.AssignObjectIdString("State", address.State, model.State);
                        status.AssignObjectIdString("Country", address.Country, model.Country);
                        address.Precedence.Value = contact.PostalAddresses.MaxOrDefault(a => a.Precedence.Value, 0) + 1;
                        address.Contact.Value = contact;

                        if (status.IsSuccess)
                        {
                            Database.Save(address);
                            Journal(address.Contact,
                                "PostalAddress.Journal.Add",
                                "Journal entry adding postal address",
                                "Added postal address {0}",
                                t => address.GetText(t));
                        }
                    }
                }

                return status.CreateJsonData();
            };
            Get["/postaladdress/delete/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var address = Database.Query<PostalAddress>(idString);
                var status = CreateStatus();

                if (status.ObjectNotNull(address))
                {
                    if (status.HasAccess(address.Contact.Value, PartAccess.Contact, AccessRight.Write))
                    {
                        address.Delete(Database);
                        Journal(address.Contact,
                            "PostalAddress.Journal.Delete",
                            "Journal entry deleting postal addresses",
                            "Deleted postal address {0}",
                            t => address.GetText(t));
                    }
                }

                return status.CreateJsonData();
            };
            Post["/postaladdress/switch"] = parameters =>
            {
                var model = JsonConvert.DeserializeObject<SwitchViewModel>(ReadBody());
                var source = Database.Query<PostalAddress>(model.SourceId);
                var status = CreateStatus();

                if (status.ObjectNotNull(source))
                {
                    if (Guid.TryParse(model.TargetId, out Guid targetId))
                    {
                        if (status.HasAccess(source.Contact.Value, PartAccess.Contact, AccessRight.Write))
                        {
                            var target = source.Contact.Value.PostalAddresses
                                .FirstOrDefault(a => a.Id.Equals(targetId));

                            if (status.ObjectNotNull(target))
                            {
                                var sourcePrecedence = source.Precedence.Value;
                                var targetPrecedence = target.Precedence.Value;
                                source.Precedence.Value = targetPrecedence;
                                target.Precedence.Value = sourcePrecedence;

                                if (source.Dirty || target.Dirty)
                                {
                                    Database.Save(source.Contact);
                                    Journal(source.Contact,
                                        "PostalAddress.Journal.Switch",
                                        "Journal entry switching two postal addresses",
                                        "Changed precedence between postal addresses {0} and {1}",
                                        t => source.GetText(t),
                                        t => target.GetText(t));
                                }
                            }
                        }
                    }
                    else
                    {
                        status.SetErrorNotFound();
                    }
                }

                return status.CreateJsonData();
            };
        }
    }
}

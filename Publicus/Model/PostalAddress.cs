using System;
using System.Collections.Generic;

namespace Publicus
{
    public class PostalAddress : DatabaseObject
    {
        public ForeignKeyField<Contact, PostalAddress> Contact { get; set; }
        public StringField Street { get; set; }
        public StringField CareOf { get; set; }
        public StringField PostOfficeBox { get; set; }
        public StringField Place { get; set; }
        public StringField PostalCode { get; set; }
        public ForeignKeyField<State, PostalAddress> State { get; set; }
        public ForeignKeyField<Country, PostalAddress> Country { get; set; }
        public Field<int> Precedence { get; set; }

        public PostalAddress() : this(Guid.Empty)
        {
        }

        public PostalAddress(Guid id) : base(id)
        {
            Contact = new ForeignKeyField<Contact, PostalAddress>(this, "contactid", false, p => p.PostalAddresses);
            Street = new StringField(this, "street", 256);
            CareOf = new StringField(this, "careof", 256);
            PostOfficeBox = new StringField(this, "postofficebox", 256);
            Place = new StringField(this, "place", 256);
            PostalCode = new StringField(this, "postalcode", 256);
            State = new ForeignKeyField<State, PostalAddress>(this, "stateid", true, null);
            Country = new ForeignKeyField<Country, PostalAddress>(this, "countryid", false, null);
            Precedence = new Field<int>(this, "precdence", 0);
        }

        public IEnumerable<string> Elements
        {
            get
            {
                if (CareOf.Value.Length > 0)
                {
                    yield return "c/o " + CareOf;
                }

                if (Street.Value.Length > 0)
                {
                    yield return Street;
                }

                if (PostOfficeBox.Value.Length > 0)
                {
                    yield return "Postfach " + PostOfficeBox;
                }

                if (PlaceWithPostalCode.Length > 0)
                {
                    yield return PlaceWithPostalCode;
                }
            }
        }

        public string FourLinesText(Translator translator)
        {
            var parts = new List<string>();

            if (CareOf.Value.Length > 0)
            {
                parts.Add("c/o " + CareOf.Value);
            }

            if (StreetOrPostOfficeBox.Length > 0)
            {
                parts.Add(StreetOrPostOfficeBox);
            }

            if (PlaceWithPostalCode.Length > 0)
            {
                parts.Add(PlaceWithPostalCode);
            }

            parts.Add(Country.Value.Name.Value[translator.Language]);

            while (parts.Count < 4)
            {
                parts.Add(string.Empty);
            }

            return string.Join(Environment.NewLine, parts);
        }

        public string Text(Translator translator)
        {
            var parts = new List<string>();

            if (StreetOrPostOfficeBox.Length > 0)
            {
                parts.Add(StreetOrPostOfficeBox);
            }

            if (PlaceWithPostalCode.Length > 0)
            {
                parts.Add(PlaceWithPostalCode);
            }

            if (Country != null)
            {
                parts.Add(Country.Value.Name.Value[translator.Language]);
            }

            return string.Join(", ", parts);
        }

        public string StateOrCountry(Translator translator)
        {
            if (State != null)
            {
                return State.Value.Name.Value[translator.Language];
            }
            else if (Country != null)
            {
                return Country.Value.Name.Value[translator.Language];
            }
            else
            {
                return string.Empty;
            }
        }

        public string StreetOrPostOfficeBox
        {
            get
            {
                if (Street.Value.Length > 0)
                {
                    return Street;
                }
                else
                {
                    return PostOfficeBox; 
                }
            } 
        }

        public string PlaceWithPostalCode
        {
            get
            {
                var place = Place.Value;

                if (PostalCode.Value.Length > 0)
                {
                    place = PostalCode.Value + " " + place; 
                }

                return place;
            }
        }

        public override string ToString()
        {
            return "PostalAddress " + StreetOrPostOfficeBox + " " + PlaceWithPostalCode;
        }

        public override void Delete(IDatabase database)
        {
            Contact.Value.PostalAddresses.Remove(this);
            database.Delete(this); 
        }

        public override string GetText(Translator translator)
        {
            return StreetOrPostOfficeBox + ", " + PlaceWithPostalCode;
        }

        public bool IsValid
        {
            get
            {
                bool isValid = true;
                isValid &= (!string.IsNullOrEmpty(PlaceWithPostalCode));
                isValid &= (!string.IsNullOrEmpty(StreetOrPostOfficeBox));
                return isValid;
            }
        }
    }

    public static class PostalAddressExtensions
    {
        public static string CountryOrEmpty(this PostalAddress address, Translator translator)
        {
            return (address == null) ? string.Empty : address.Country.Value.Name.Value[translator.Language];
        }

        public static string StateOrEmpty(this PostalAddress address, Translator translator)
        {
            var state = (address == null) ? null : address.State.Value;
            return (state == null) ? string.Empty : state.Name.Value[translator.Language];
        }

        public static string PostalCodeOrEmpty(this PostalAddress address)
        {
            return (address == null) ? string.Empty : address.PostalCode.Value;
        }

        public static string PlaceOrEmpty(this PostalAddress address)
        {
            return (address == null) ? string.Empty : address.Place.Value;
        }

        public static string PostOfficeBoxOrEmpty(this PostalAddress address)
        {
            return (address == null) ? string.Empty : address.PostOfficeBox.Value;
        }

        public static string StreetOrEmpty(this PostalAddress address)
        {
            return (address == null) ? string.Empty : address.Street.Value;
        }

        public static string CareOfOrEmpty(this PostalAddress address)
        {
            return (address == null) ? string.Empty : address.CareOf.Value;
        }
    }
}

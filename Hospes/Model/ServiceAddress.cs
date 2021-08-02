using System;
using System.Collections.Generic;
using SiteLibrary;

namespace Hospes
{
    public enum ServiceType
    {
        EMail,
        Phone,
        Fax,
    }

    public static class ServiceTypeExtensions
    {
        public static string Translate(this ServiceType service, Translator translator)
        {
            switch (service)
            {
                case ServiceType.EMail:
                    return translator.Get("Enum.ServiceType.EMail", "Value 'E-Mail' in enum service type", "E-Mail");
                case ServiceType.Phone:
                    return translator.Get("Enum.ServiceType.Phone", "Value 'Phone' in enum service type", "Phone");
                case ServiceType.Fax:
                    return translator.Get("Enum.ServiceType.Fax", "Value 'Fax' in enum service type", "Fax");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum AddressCategory
    { 
        Home,
        Work,
        Mobile,
        Organization,
    }

    public static class AddressCategoryExtensions
    {
        public static string Translate(this AddressCategory category, Translator translator)
        {
            switch (category)
            {
                case AddressCategory.Home:
                    return translator.Get("Enum.AddressCategory.Home", "Value 'Home' in enum address category", "Home");
                case AddressCategory.Organization:
                    return translator.Get("Enum.AddressCategory.Organization", "Value 'Organization' in enum address category", "Organization");
                case AddressCategory.Work:
                    return translator.Get("Enum.AddressCategory.Work", "Value 'Work' in enum address category", "Work");
                case AddressCategory.Mobile:
                    return translator.Get("Enum.AddressCategory.Mobile", "Value 'Mobile' in enum address category", "Mobile");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class ServiceAddress : DatabaseObject
    {
        public ForeignKeyField<Person, ServiceAddress> Person { get; set; }
        public StringField Address { get; set; }
        public EnumField<ServiceType> Service { get; set; }
        public EnumField<AddressCategory> Category { get; set; }
        public Field<int> Precedence { get; set; }

        public ServiceAddress() : this(Guid.Empty)
        {
        }

		public ServiceAddress(Guid id) : base(id)
        {
            Person = new ForeignKeyField<Person, ServiceAddress>(this, "personid", false, p => p.ServiceAddresses);
            Address = new StringField(this, "address", 256);
            Service = new EnumField<ServiceType>(this, "service", ServiceType.EMail, ServiceTypeExtensions.Translate);
            Category = new EnumField<AddressCategory>(this, "category", AddressCategory.Home, AddressCategoryExtensions.Translate);
            Precedence = new Field<int>(this, "precedence", 0);
        }

        public override string ToString()
        {
            return "ServiceAddress " + Address.Value;
        }

        public override string GetText(Translator translator)
        {
            return Address.Value;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var document in database.Query<Sending>(DC.Equal("addressid", Id.Value)))
            {
                document.Delete(database);
            }

            database.Delete(this);
        }
    }
}

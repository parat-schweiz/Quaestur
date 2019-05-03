using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;

namespace Publicus
{
    public class Contact : DatabaseObject
    {
        public StringField Organization { get; private set; }
        public StringField Title { get; private set; }
        public StringField FirstName { get; private set; }
        public StringField MiddleNames { get; private set; }
        public StringField LastName { get; private set; }
        public Field<DateTime> BirthDate { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public List<PostalAddress> PostalAddresses { get; private set; }
        public List<ServiceAddress> ServiceAddresses { get; private set; }
        public List<Subscription> Subscriptions { get; private set; }
        public List<TagAssignment> TagAssignments { get; private set; }
        public List<PublicKey> PublicKeys { get; private set; }
        public Field<bool> Deleted { get; private set; }

        public Contact() : this(Guid.Empty)
        {
        }

        public Contact(Guid id) : base(id)
        {
            Organization = new StringField(this, "username", 32);
            Title = new StringField(this, "title", 256);
            FirstName = new StringField(this, "firstname", 256);
            MiddleNames = new StringField(this, "middlenames", 256);
            LastName = new StringField(this, "lastname", 256);
            BirthDate = new Field<DateTime>(this, "birthdate", new DateTime(1850, 1, 1));
            Language = new EnumField<Language>(this, "language", Publicus.Language.English, LanguageExtensions.Translate);
            Deleted = new Field<bool>(this, "deleted", false);
            PostalAddresses = new List<PostalAddress>();
            ServiceAddresses = new List<ServiceAddress>();
            Subscriptions = new List<Subscription>();
            TagAssignments = new List<TagAssignment>();
            PublicKeys = new List<PublicKey>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<PostalAddress>("contactid", Id.Value, () => PostalAddresses);
                yield return new MultiCascade<ServiceAddress>("contactid", Id.Value, () => ServiceAddresses);
                yield return new MultiCascade<Subscription>("contactid", Id.Value, () => Subscriptions);
                yield return new MultiCascade<TagAssignment>("contactid", Id.Value, () => TagAssignments);
                yield return new MultiCascade<PublicKey>("contactid", Id.Value, () => PublicKeys);
            }
        }

        public IEnumerable<Subscription> ActiveSubscriptions
        {
            get
            {
                return Subscriptions.Where(m => m.IsActive); 
            }
        }

        public string PrimaryPostalAddressFiveLines(Translator translator)
        {
            var address = PrimaryPostalAddress;

            if (address == null)
            {
                return ShortHand +
                    Environment.NewLine +
                    Environment.NewLine +
                    Environment.NewLine +
                    Environment.NewLine;
            }
            else
            {
                return ShortHand +
                    Environment.NewLine +
                    address.FourLinesText(translator);
            }
        }

        public PostalAddress PrimaryPostalAddress
        {
            get
            {
                return PostalAddresses
                    .OrderBy(a => a.Precedence.Value)
                    .FirstOrDefault(); 
            }
        }

        public string PrimaryPostalAddressText(Translator translator)
        {
            var address = PrimaryPostalAddress;

            if (address == null)
            {
                return string.Empty;
            }
            else
            {
                return address.Text(translator);
            }
        }

        public ServiceAddress PrimaryAddress(ServiceType type)
        {
            return ServiceAddresses
                .Where(a => a.Service.Value == type)
                .OrderBy(a => a.Precedence.Value)
                .FirstOrDefault();
        }

        public string PrimaryMailAddress
        {
            get 
            {
                return ServiceAddresses
                    .Where(a => a.Service.Value == ServiceType.EMail)
                    .OrderBy(a => a.Precedence.Value)
                    .Select(a => a.Address.Value)
                    .FirstOrDefault() ?? string.Empty;
            } 
        }

        public string PrimaryPhoneNumber
        {
            get
            {
                return ServiceAddresses
                    .Where(a => a.Service.Value == ServiceType.Phone)
                    .OrderBy(a => a.Precedence.Value)
                    .Select(a => a.Address.Value)
                    .FirstOrDefault() ?? string.Empty;
            }
        }

        public string ShortTitleAndNames
        {
            get
            {
                var name = ShortFirstNames;

                if (Title.Value.Length > 0)
                {
                    name = Title + " " + name; 
                }

                return name;
            }
        }

        public string ShortHand
        {
            get
            {
                if (LastName.Value.Length > 0)
                {
                    var name = LastName.Value;

                    if (ShortFirstNames.Length > 0)
                    {
                        name = ShortFirstNames + " " + name; 
                    }

                    return name;
                }
                else
                {
                    return Organization; 
                }
            }
        }

        public string SortName
        {
            get
            {
                var name = Organization.Value;

                if (LastName.Value.Length > 0)
                {
                    name += ", " + LastName.Value;

                    if (ShortFirstNames.Length > 0)
                    {
                        name += ", " + ShortFirstNames;
                    }
                }

                return name;
            }
        }

        public override string ToString()
        {
            return "Contact " + ShortHand;
        }

        public string ShortFirstNames
        {
            get
            {
                var name = FirstName.Value;

                foreach (var m in MiddleNames.Value.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries))
                {
                    name += " " + m.First() + ".";
                }

                return name;
            } 
        }

        public string FullFirstNames
        {
            get
            {
                var name = FirstName.Value;

                if (MiddleNames.Value.Length > 0)
                {
                    name += " " + MiddleNames;
                }

                return name;
            }
        }

        public string FullName
        {
            get
            {
                var name = LastName.Value;

                if (MiddleNames.Value.Length > 0)
                {
                    name = MiddleNames + " " + name; 
                }

                if (FirstName.Value.Length > 0)
                {
                    name = FirstName + " " + name;
                }

                if (Title.Value.Length > 0)
                {
                    name = Title + " " + name;
                }

                return name;
            }
        }

        public override string GetText(Translator translator)
        {
            return ShortHand;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var address in database.Query<PostalAddress>(DC.Equal("contactid", Id.Value)))
            {
                address.Delete(database); 
            }

            foreach (var address in database.Query<ServiceAddress>(DC.Equal("contactid", Id.Value)))
            {
                address.Delete(database);
            }

            foreach (var subscription in database.Query<Subscription>(DC.Equal("contactid", Id.Value)))
            {
                subscription.Delete(database);
            }

            foreach (var roleAssignment in database.Query<RoleAssignment>(DC.Equal("contactid", Id.Value)))
            {
                roleAssignment.Delete(database);
            }

            foreach (var tagAssignment in database.Query<TagAssignment>(DC.Equal("contactid", Id.Value)))
            {
                tagAssignment.Delete(database);
            }

            foreach (var document in database.Query<Document>(DC.Equal("contactid", Id.Value)))
            {
                document.Delete(database);
            }

            foreach (var entry in database.Query<JournalEntry>(DC.Equal("contactid", Id.Value)))
            {
                entry.Delete(database);
            }

            database.Delete(this); 
        }
    }
}

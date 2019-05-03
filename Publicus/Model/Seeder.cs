using System;
using System.Linq;
using System.Collections.Generic;

namespace Publicus
{
    public class Seeder
    {
        private IDatabase _db;
        private Random _rnd;

        public Seeder(IDatabase db)
        {
            _db = db;
            _rnd = new Random(0);
        }

        public void FullSeed()
        {
            AddSystemWideSettings();

            var count = _db.Query<Contact>().Count();

            while (count < 223)
            {
                CreateContact(count + 1);
                count++;
            }
        }

        public void MinimalSeed()
        {
            AddSystemWideSettings();
        }

        private void AddSystemWideSettings()
        {
            var settings = _db.Query<SystemWideSettings>().SingleOrDefault();

            if (settings == null)
            {
                settings = new SystemWideSettings(Guid.NewGuid());
                settings.Currency.Value = "CHF";
                _db.Save(settings);
            }
        }

        private enum RoleProfile
        {
            SysAdmin,
            BoardAdmin,
            BoardMember,
            GroupAdmin,
            GroupMember,
        }

        private void AddPermission(Role role, SubjectAccess subject, PartAccess part, AccessRight right)
        {
            var permission = new Permission(Guid.NewGuid());
            permission.Role.Value = role;
            permission.Subject.Value = subject;
            permission.Part.Value = part;
            permission.Right.Value = right;
            _db.Save(permission);
        }

        private Role GetRole(Group group, string name, RoleProfile profile)
        {
            var role = group.Roles.FirstOrDefault(g => g.Name.Value.AnyValue == name);

            if (role == null)
            {
                role = new Role(Guid.NewGuid());
                role.Name.Value[Language.German] = name;
                role.Group.Value = group;
                _db.Save(role);

                switch (profile)
                {
                    case RoleProfile.SysAdmin:
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.CustomDefinitions, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Structure, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Contact, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Subscription, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.RoleAssignments, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.TagAssignments, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Demography, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Documents, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Mailings, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Anonymous, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Journal, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SystemWide, PartAccess.Crypto, AccessRight.Write);
                        break;
                    case RoleProfile.BoardAdmin:
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.CustomDefinitions, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Structure, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Contact, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Subscription, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.RoleAssignments, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.TagAssignments, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Demography, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Documents, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Mailings, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Anonymous, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Journal, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Crypto, AccessRight.Write);
                        break;
                    case RoleProfile.BoardMember:
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.CustomDefinitions, AccessRight.Read);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Structure, AccessRight.Read);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Subscription, AccessRight.Read);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.RoleAssignments, AccessRight.Read);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Contact, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.TagAssignments, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Demography, AccessRight.Read);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Documents, AccessRight.Read);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Mailings, AccessRight.Write);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Anonymous, AccessRight.Read);
                        AddPermission(role, SubjectAccess.SubFeed, PartAccess.Journal, AccessRight.Read);
                        break;
                    case RoleProfile.GroupAdmin:
                        AddPermission(role, SubjectAccess.Group, PartAccess.CustomDefinitions, AccessRight.Read);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Structure, AccessRight.Read);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Subscription, AccessRight.Read);
                        AddPermission(role, SubjectAccess.Group, PartAccess.RoleAssignments, AccessRight.Write);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Contact, AccessRight.Write);
                        AddPermission(role, SubjectAccess.Group, PartAccess.TagAssignments, AccessRight.Write);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Demography, AccessRight.Read);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Documents, AccessRight.Read);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Mailings, AccessRight.Write);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Anonymous, AccessRight.Read);
                        AddPermission(role, SubjectAccess.Group, PartAccess.Journal, AccessRight.Read);
                        break;
                    case RoleProfile.GroupMember:
                        AddPermission(role, SubjectAccess.Group, PartAccess.Anonymous, AccessRight.Read);
                        break;
                }
            }

            return role;
        }

        private Group GetGroup(Feed feed, string name)
        {
            var group = feed.Groups.FirstOrDefault(g => g.Name.Value.AnyValue == name);

            if (group == null)
            {
                group = new Group(Guid.NewGuid());
                group.Name.Value[Language.German] = name;
                group.MailName.Value[Language.German] = feed.Name.Value.AnyValue + " " + name;
                group.MailAddress.Value[Language.German] = 
                    name.ToLowerInvariant().Replace(" ", "-") + "@" +
                    feed.Name.Value.AnyValue.ToLowerInvariant().Replace(" ", "-") + ".ch";
                group.Feed.Value = feed;
                _db.Save(group);
            }

            return group;
        }

        private Language SelectLanguage()
        {
            if (_rnd.Next(0, 1) == 0)
            {
                return Language.German;
            }
            else
            {
                return Language.French; 
            }
        }

        private Contact CreateContact(int number)
        {
            var female = _rnd.Next(2) == 1;
            int middleNameCount = _rnd.Next(5);
            if (middleNameCount > 2) middleNameCount = 0;

            var contact = new Contact(Guid.NewGuid());
            contact.Language.Value = SelectLanguage();
            contact.LastName.Value = LastNames.Skip(_rnd.Next(LastNames.Count())).First();

            if (female)
            {
                contact.FirstName.Value = FemaleFirstNames.Skip(_rnd.Next(FemaleFirstNames.Count())).First();
                var middleNames = new List<string>();
                for (int i = 0; i < middleNameCount; i++)
                {
                    middleNames.Add(FemaleFirstNames.Skip(_rnd.Next(FemaleFirstNames.Count())).First());
                }
                contact.MiddleNames.Value = string.Join(" ", middleNames);
            }
            else
            {
                contact.FirstName.Value = MaleFirstNames.Skip(_rnd.Next(MaleFirstNames.Count())).First();
                var middleNames = new List<string>();
                for (int i = 0; i < middleNameCount; i++)
                {
                    middleNames.Add(MaleFirstNames.Skip(_rnd.Next(MaleFirstNames.Count())).First());
                }
                contact.MiddleNames.Value = string.Join(" ", middleNames);
            }

            contact.BirthDate.Value = new DateTime(1960, 1, 1).AddDays(_rnd.NextDouble() * 40d * 365d);
            contact.Organization.Value = "New Organization";

            var homeMail = new ServiceAddress(Guid.NewGuid());
            homeMail.Service.Value = ServiceType.EMail;
            homeMail.Category.Value = AddressCategory.Home;
            homeMail.Precedence.Value = contact.ServiceAddresses.MaxOrDefault(a => a.Precedence.Value, 0) + 1;
            homeMail.Address.Value =
                "stefan+" +
                contact.FirstName.Value.ToLowerInvariant() + "." +
                contact.LastName.Value.ToLowerInvariant() + "@savvy.ch";
            homeMail.Contact.Value = contact;

            if (_rnd.Next(3) != 0)
            {
                var mobile = new ServiceAddress(Guid.NewGuid());
                mobile.Service.Value = ServiceType.Phone;
                mobile.Category.Value = AddressCategory.Mobile;
                mobile.Precedence.Value = contact.ServiceAddresses.MaxOrDefault(a => a.Precedence.Value, 0) + 1;
                mobile.Address.Value =
                    MobilePrefix.Skip(_rnd.Next(MobilePrefix.Count())).First() +
                    ComposeNumber();
                mobile.Contact.Value = contact;
            }

            if (_rnd.Next(3) == 0)
            {
                var phone = new ServiceAddress(Guid.NewGuid());
                phone.Service.Value = ServiceType.Phone;
                phone.Category.Value = AddressCategory.Mobile;
                phone.Precedence.Value = contact.ServiceAddresses.MaxOrDefault(a => a.Precedence.Value, 0) + 1;
                phone.Address.Value =
                    PhonePrefix.Skip(_rnd.Next(PhonePrefix.Count())).First() +
                    ComposeNumber();
                phone.Contact.Value = contact;
            }

            var place = Places.Skip(_rnd.Next(Places.Count())).First();
            var postalAddress = new PostalAddress(Guid.NewGuid());
            postalAddress.Country.Value = GetCountry("Schweiz");
            postalAddress.State.Value = GetState(place.Item3);
            postalAddress.PostalCode.Value = place.Item1.ToString();
            postalAddress.Place.Value = place.Item2;
            postalAddress.Street.Value =
                Streets.Skip(_rnd.Next(Streets.Count())).First() +
                " " + (_rnd.Next(23) + 1).ToString();
            postalAddress.Precedence.Value = contact.PostalAddresses.MaxOrDefault(a => a.Precedence.Value, 0) + 1;
            postalAddress.Contact.Value = contact;

            var subscription = new Subscription(Guid.NewGuid());
            subscription.Contact.Value = contact;
            subscription.Feed.Value = GetFeed("Piratenpartei Zentralschweiz", null);
            subscription.StartDate.Value = new DateTime(2018, 12, 18);

            switch (_rnd.Next(4))
            {
                case 0:
                    var subscription2 = new Subscription(Guid.NewGuid());
                    subscription2.Contact.Value = contact;
                    subscription2.Feed.Value = GetFeed("Piratenpartei Zug", "Piratenpartei Zentralschweiz");
                    subscription2.StartDate.Value = new DateTime(2018, 12, 18);
                    break;
                case 1:
                    var subscription3 = new Subscription(Guid.NewGuid());
                    subscription3.Contact.Value = contact;
                    subscription3.Feed.Value = GetFeed("Piratenpartei Luzern", "Piratenpartei Zentralschweiz");
                    subscription3.StartDate.Value = new DateTime(2018, 12, 18);
                    break;
            }

            var tagAssignment1 = new TagAssignment(Guid.NewGuid());
            tagAssignment1.Tag.Value = GetTag("Partizipationsmails", TagUsage.Mailing, TagMode.Default | TagMode.Manual);
            tagAssignment1.Contact.Value = contact;

            if (_rnd.Next(5) < 4)
            {
                var tagAssignment2 = new TagAssignment(Guid.NewGuid());
                tagAssignment2.Tag.Value = GetTag("Verantstaltungsmails", TagUsage.Mailing, TagMode.Default | TagMode.Manual | TagMode.Self);
                tagAssignment2.Contact.Value = contact;
            }

            if (_rnd.Next(5) < 4)
            {
                var tagAssignment3 = new TagAssignment(Guid.NewGuid());
                tagAssignment3.Tag.Value = GetTag("Aktionsmails", TagUsage.Mailing, TagMode.Default | TagMode.Manual | TagMode.Self);
                tagAssignment3.Contact.Value = contact;
            }

            if (_rnd.Next(5) < 3)
            {
                var tagAssignment4 = new TagAssignment(Guid.NewGuid());
                tagAssignment4.Tag.Value = GetTag("Aktivist", TagUsage.None, TagMode.Manual);
                tagAssignment4.Contact.Value = contact;
            }

            _db.Save(contact);

            return contact;
        }

        private State GetState(string name)
        {
            var state = _db.Query<State>(DC.Equal("name", name)).FirstOrDefault();

            if (state == null)
            {
                state = new State(Guid.NewGuid());
                state.Name.Value[Language.German] = name;
                _db.Save(state);
            }

            return state;
        }

        private Country GetCountry(string name)
        {
            var country = _db.Query<Country>(DC.Equal("name", name)).FirstOrDefault();

            if (country == null)
            {
                country = new Country(Guid.NewGuid());
                country.Name.Value[Language.German] = name;
                _db.Save(country); 
            }

            return country;
        }

        private Tag GetTag(string name, TagUsage usage, TagMode mode)
        {
            var tag = _db.Query<Tag>(DC.Equal("name", name)).FirstOrDefault();

            if (tag == null)
            {
                tag = new Tag(Guid.NewGuid());
                tag.Name.Value[Language.German] = name;
                tag.Usage.Value = usage;
                tag.Mode.Value = mode;
                _db.Save(tag);
            }

            return tag;
        }

        private Feed GetFeed(string name, string parent)
        {
            var feed = _db.Query<Feed>().FirstOrDefault(o => o.Name.Value.AnyValue == name);

            if (feed == null)
            {
                feed = new Feed(Guid.NewGuid());
                feed.Name.Value[Language.German] = name;

                if (parent != null && parent.Length > 0)
                {
                    feed.Parent.Value = GetFeed(parent, null); 
                }

                _db.Save(feed);
            }

            return feed;
        }

        private string ComposeNumber()
        {
            return
                _rnd.Next(10).ToString() +
                _rnd.Next(10).ToString() +
                _rnd.Next(10).ToString() +
                " " +
                _rnd.Next(10).ToString() +
                _rnd.Next(10).ToString() +
                " " +
                _rnd.Next(10).ToString() +
                _rnd.Next(10).ToString();
        }

        private IEnumerable<string> Streets
        {
            get
            {
                yield return "Bahnhofstrasse";
                yield return "Dorfstrasse";
                yield return "Bundesplatz";
                yield return "Postgasse";
                yield return "Markgasse";
                yield return "Marktplatz";
                yield return "Rathausgasse";
                yield return "Rathausplatz";
                yield return "Museumstrasse";
                yield return "Helvetiastrasse";
                yield return "Bahnhofstrasse";
                yield return "Wildstrasse";
                yield return "Elfenstrasse";
                yield return "Gruberstrasse";
                yield return "Breitfeldstrasse";
                yield return "Tellplatz";
                yield return "Morgartenstrasse";
                yield return "Nordring";
                yield return "Südring";
                yield return "Ostring";
                yield return "Westring";
                yield return "Steckweg";
                yield return "Quartiergasse";
                yield return "Hofweg";
                yield return "Lagerweg";
                yield return "Schulweg";
                yield return "Falkenweg";
                yield return "Sennweg";
                yield return "Amselweg";
                yield return "Drosselweg";
                yield return "Tannenweg";
                yield return "Buchenweg";
                yield return "Forstweg";
                yield return "Zeltweg";
                yield return "Simonstrasse";
                yield return "Riedweg";
                yield return "Gartenstrasse";
                yield return "Wagnerstrasse";
                yield return "Pilgerweg";
                yield return "Rohrweg";
                yield return "Sonneggweg";
                yield return "Grenzweg";
                yield return "Floraweg";
                yield return "Schützenstrasse";
                yield return "Hangweg";
                yield return "Hubelweg";
                yield return "Spiegelstrasse";
                yield return "Erlenweg";
                yield return "Eichenweg";
                yield return "Holderweg";
                yield return "Finkenweg";
                yield return "Fliederweg";
                yield return "Erikaweg";
            } 
        }

        private IEnumerable<Tuple<int, string, string>> Places
        {
            get
            {
                yield return new Tuple<int, string, string>(1000, "Lausanne", "Waadt");
                yield return new Tuple<int, string, string>(1200, "Genève", "Genf");
                yield return new Tuple<int, string, string>(2000, "Neuchâtel", "Neuenburg");
                yield return new Tuple<int, string, string>(2300, "La Chaux-de-Fonds", "Neuenburg");
                yield return new Tuple<int, string, string>(2500, "Biel/Bienne", "Bern");
                yield return new Tuple<int, string, string>(2800, "Delémont", "Jura");
                yield return new Tuple<int, string, string>(2900, "Porrentruy", "Jura");
                yield return new Tuple<int, string, string>(3000, "Bern", "Bern");
                yield return new Tuple<int, string, string>(3400, "Burgdorf", "Bern");
                yield return new Tuple<int, string, string>(3600, "Thun", "Bern");
                yield return new Tuple<int, string, string>(3700, "Spiez", "Bern");
                yield return new Tuple<int, string, string>(3900, "Brig", "Wallis");
                yield return new Tuple<int, string, string>(4000, "Basel", "Basel-Stadt");
                yield return new Tuple<int, string, string>(4500, "Solothurn", "Solothurn");
                yield return new Tuple<int, string, string>(4600, "Olten", "Solothurn");
                yield return new Tuple<int, string, string>(4800, "Zofingen", "Aargau");
                yield return new Tuple<int, string, string>(4900, "Langenthal", "Bern");
                yield return new Tuple<int, string, string>(5000, "Aarau", "Aargau");
                yield return new Tuple<int, string, string>(5200, "Brugg", "Aargau");
                yield return new Tuple<int, string, string>(5400, "Baden", "Aargau");
                yield return new Tuple<int, string, string>(5600, "Lenzburg", "Aargau");
                yield return new Tuple<int, string, string>(6000, "Luzern", "Luzern");
                yield return new Tuple<int, string, string>(6300, "Zug", "Zug");
                yield return new Tuple<int, string, string>(6500, "Bellinzona", "Tessin");
                yield return new Tuple<int, string, string>(6600, "Locarno", "Tessin");
                yield return new Tuple<int, string, string>(6900, "Lugano", "Tessin");
                yield return new Tuple<int, string, string>(7000, "Chur", "Graubünden");
                yield return new Tuple<int, string, string>(7500, "St. Moritz", "Graubünden");
                yield return new Tuple<int, string, string>(8000, "Zürich", "Zürich");
                yield return new Tuple<int, string, string>(8200, "Schaffhausen", "Schaffhausen");
                yield return new Tuple<int, string, string>(8400, "Winterthur", "Zürich");
                yield return new Tuple<int, string, string>(8500, "Frauenfeld", "Thurgau");
                yield return new Tuple<int, string, string>(8600, "Dübendorf", "Zürich");
                yield return new Tuple<int, string, string>(8700, "Küsnacht", "Zurich");
                yield return new Tuple<int, string, string>(8800, "Thalwil", "Zürich");
                yield return new Tuple<int, string, string>(9000, "St. Gallen", "St. Gallen");
                yield return new Tuple<int, string, string>(9100, "Herisau", "Appenzell Ausserrhoden");
                yield return new Tuple<int, string, string>(9200, "Gossau", "St. Gallen");
                yield return new Tuple<int, string, string>(9500, "Wil", "St. Gallen");
            }
        }

        private IEnumerable<string> PhonePrefix
        {
            get
            {
                yield return "+41 21 ";
                yield return "+41 22 ";
                yield return "+41 26 ";
                yield return "+41 31 ";
                yield return "+41 32 ";
                yield return "+41 33 ";
                yield return "+41 41 ";
                yield return "+41 43 ";
                yield return "+41 44 ";
                yield return "+41 52 ";
                yield return "+41 55 ";
                yield return "+41 61 ";
                yield return "+41 71 ";
            }
        }

        private IEnumerable<string> MobilePrefix
        {
            get
            {
                yield return "+41 79 ";
                yield return "+41 78 ";
                yield return "+41 77 ";
                yield return "+41 76 ";
                yield return "+41 75 ";
            }
        }

        private IEnumerable<string> LastNames
        {
            get
            {
                yield return "Smith";
                yield return "Johnson";
                yield return "Williams";
                yield return "Jones";
                yield return "Brown";
                yield return "Davis";
                yield return "Miller";
                yield return "Wilson";
                yield return "Moore";
                yield return "Taylor";
                yield return "Anderson";
                yield return "Thomas";
                yield return "Jackson";
                yield return "White";
                yield return "Harris";
                yield return "Martin";
                yield return "Thompson";
                yield return "Garcia";
                yield return "Martinez";
                yield return "Robinson";
                yield return "Clark";
                yield return "Rodriguez";
                yield return "Lewis";
                yield return "Lee";
                yield return "Walker";
                yield return "Hall";
                yield return "Allen";
                yield return "Young";
                yield return "Hernandez";
                yield return "King";
                yield return "Wright";
                yield return "Lopez";
                yield return "Hill";
                yield return "Scott";
                yield return "Green";
                yield return "Adams";
                yield return "Baker";
                yield return "Gonzalez";
                yield return "Nelson";
                yield return "Carter";
                yield return "Mitchell";
                yield return "Perez";
                yield return "Roberts";
                yield return "Turner";
                yield return "Phillips";
                yield return "Campbell";
                yield return "Parker";
                yield return "Evans";
                yield return "Edwards";
                yield return "Collins";
                yield return "Stewart";
                yield return "Sanchez";
                yield return "Morris";
                yield return "Rogers";
                yield return "Reed";
                yield return "Cook";
                yield return "Morgan";
                yield return "Bell";
                yield return "Murphy";
                yield return "Bailey";
                yield return "Rivera";
                yield return "Cooper";
                yield return "Richardson";
                yield return "Cox";
                yield return "Howard";
                yield return "Ward";
                yield return "Torres";
                yield return "Peterson";
                yield return "Gray";
                yield return "Ramirez";
                yield return "James";
                yield return "Watson";
                yield return "Brooks";
                yield return "Kelly";
                yield return "Sanders";
                yield return "Price";
                yield return "Bennett";
                yield return "Wood";
                yield return "Barnes";
                yield return "Ross";
                yield return "Henderson";
                yield return "Coleman";
                yield return "Jenkins";
                yield return "Perry";
                yield return "Powell";
                yield return "Long";
                yield return "Patterson";
                yield return "Hughes";
                yield return "Flores";
                yield return "Washington";
                yield return "Butler";
                yield return "Simmons";
                yield return "Foster";
                yield return "Gonzales";
                yield return "Bryant";
                yield return "Alexander";
                yield return "Russell";
                yield return "Griffin";
                yield return "Diaz";
                yield return "Hayes";
            }
        }

        private IEnumerable<string> MaleFirstNames
        {
            get
            {
                yield return "James";
                yield return "John";
                yield return "Robert";
                yield return "Michael";
                yield return "William";
                yield return "David";
                yield return "Richard";
                yield return "Joseph";
                yield return "Thomas";
                yield return "Charles";
                yield return "Christopher";
                yield return "Daniel";
                yield return "Matthew";
                yield return "Anthony";
                yield return "Donald";
                yield return "Mark";
                yield return "Paul";
                yield return "Steven";
                yield return "Andrew";
                yield return "Kenneth"; //20
                yield return "George";
                yield return "Joshua";
                yield return "Kevin";
                yield return "Brian";
                yield return "Edward";
                yield return "Ronald";
                yield return "Timothy";
                yield return "Jason";
                yield return "Jeffrey";
                yield return "Ryan";
                yield return "Jacob";
                yield return "Gary";
                yield return "Nicholas";
                yield return "Eric";
                yield return "Stephen";
                yield return "Jonathan";
                yield return "Larry";
                yield return "Justin";
                yield return "Scott";
                yield return "Brandon"; //40
            }
        }

        private IEnumerable<string> FemaleFirstNames
        {
            get
            {
                yield return "Mary";
                yield return "Mary";
                yield return "Jennifer";
                yield return "Linda";
                yield return "Elizabeth";
                yield return "Barbara";
                yield return "Susan";
                yield return "Jessica";
                yield return "Sarah";
                yield return "Margaret";
                yield return "Karen";
                yield return "Nancy";
                yield return "Lisa";
                yield return "Betty";
                yield return "Dorothy";
                yield return "Sandra";
                yield return "Ashley";
                yield return "Kimberly";
                yield return "Donna";
                yield return "Emily"; //20
                yield return "Carol";
                yield return "Michelle";
                yield return "Amanda";
                yield return "Melissa";
                yield return "Deborah";
                yield return "Stephanie";
                yield return "Rebecca";
                yield return "Laura";
                yield return "Helen";
                yield return "Sharon";
                yield return "Cynthia";
                yield return "Kathleen";
                yield return "Amy";
                yield return "Shirley";
                yield return "Angela";
                yield return "Anna";
                yield return "Ruth";
                yield return "Brenda";
                yield return "Pamela";
                yield return "Nicole";
            }
        }
    }
}

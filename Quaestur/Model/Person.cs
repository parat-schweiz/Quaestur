﻿using System;
using System.Linq;
using System.Collections.Generic;
using Nancy;
using Nancy.Security;
using Nancy.Authentication.Forms;
using SiteLibrary;

namespace Quaestur
{
    public enum PasswordType
    {
        None = 0,
        Local = 1,
        SecurityService = 2,
    }

    public static class PasswordTypeExtensions
    {
        public static string Translate(this PasswordType type, Translator translator)
        {
            switch (type)
            {
                case PasswordType.None:
                    return translator.Get("Enum.PasswordType.None", "None value in the password type enum", "None");
                case PasswordType.Local:
                    return translator.Get("Enum.PasswordType.Local", "Local value in the password type enum", "Local");
                case PasswordType.SecurityService:
                    return translator.Get("Enum.PasswordType.SecurityService", "Security service value in the password type enum", "Security service");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public enum UserStatus
    {
        Locked = 0,
        Active = 1,
    }

    public static class UserStatusExtensions
    {
        public static string Translate(this UserStatus status, Translator translator)
        {
            switch (status)
            {
                case UserStatus.Active:
                    return translator.Get("Enum.UserStatus.Active", "Active value in the user status enum", "Active");
                case UserStatus.Locked:
                    return translator.Get("Enum.UserStatus.Locked", "Locked value in the user status enum", "Locked");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Person : DatabaseObject
    {
        public Field<int> Number { get; private set; }
        public StringField UserName { get; private set; }
        public EnumField<PasswordType> PasswordType { get; private set; }
        public ByteArrayField PasswordHash { get; private set; }
        public StringField Title { get; private set; }
        public StringField FirstName { get; private set; }
        public StringField MiddleNames { get; private set; }
        public StringField LastName { get; private set; }
        public DateField BirthDate { get; private set; }
        public EnumField<UserStatus> UserStatus { get; private set; }
        public EnumField<Language> Language { get; private set; }
        public List<PostalAddress> PostalAddresses { get; private set; }
        public List<ServiceAddress> ServiceAddresses { get; private set; }
        public List<Membership> Memberships { get; private set; }
        public List<RoleAssignment> RoleAssignments { get; private set; }
        public List<TagAssignment> TagAssignments { get; private set; }
        public List<PublicKey> PublicKeys { get; private set; }
        public Field<bool> Deleted { get; private set; }
        public ByteArrayField TwoFactorSecret { get; private set; }
        public DateTimeNullField PaymentParameterUpdateReminderDate { get; private set; }
        public FieldNull<int> PaymentParameterUpdateReminderLevel { get; private set; }
        public List<PersonalPaymentParameter> PaymentParameters { get; private set; }

        public Person() : this(Guid.Empty)
        {
        }

        public Person(Guid id) : base(id)
        {
            Number = new Field<int>(this, "number", 0);
            UserName = new StringField(this, "username", 32);
            PasswordType = new EnumField<PasswordType>(this, "passwordtype", Quaestur.PasswordType.None, PasswordTypeExtensions.Translate);
            PasswordHash = new ByteArrayField(this, "passwordhash", true);
            Title = new StringField(this, "title", 256);
            FirstName = new StringField(this, "firstname", 256);
            MiddleNames = new StringField(this, "middlenames", 256);
            LastName = new StringField(this, "lastname", 256);
            BirthDate = new DateField(this, "birthdate", new DateTime(1850, 1, 1));
            UserStatus = new EnumField<UserStatus>(this, "userstatus", Quaestur.UserStatus.Locked, UserStatusExtensions.Translate);
            Language = new EnumField<Language>(this, "language", SiteLibrary.Language.English, LanguageExtensions.Translate);
            Deleted = new Field<bool>(this, "deleted", false);
            TwoFactorSecret = new ByteArrayField(this, "twofactorsecret", true);
            PaymentParameterUpdateReminderDate = new DateTimeNullField(this, "paymentparameterupdatereminderdate");
            PaymentParameterUpdateReminderLevel = new FieldNull<int>(this, "paymentparameterupdatereminderlevel");
            PostalAddresses = new List<PostalAddress>();
            ServiceAddresses = new List<ServiceAddress>();
            Memberships = new List<Membership>();
            RoleAssignments = new List<RoleAssignment>();
            TagAssignments = new List<TagAssignment>();
            PublicKeys = new List<PublicKey>();
            PaymentParameters = new List<PersonalPaymentParameter>();
        }

        public override IEnumerable<MultiCascade> Cascades
        {
            get
            {
                yield return new MultiCascade<PostalAddress>("personid", Id.Value, () => PostalAddresses);
                yield return new MultiCascade<ServiceAddress>("personid", Id.Value, () => ServiceAddresses);
                yield return new MultiCascade<Membership>("personid", Id.Value, () => Memberships);
                yield return new MultiCascade<RoleAssignment>("personid", Id.Value, () => RoleAssignments);
                yield return new MultiCascade<TagAssignment>("personid", Id.Value, () => TagAssignments);
                yield return new MultiCascade<PublicKey>("personid", Id.Value, () => PublicKeys);
                yield return new MultiCascade<PersonalPaymentParameter>("personid", Id.Value, () => PaymentParameters);
            }
        }

        public IEnumerable<Membership> ActiveMemberships
        {
            get
            {
                return Memberships.Where(m => m.IsActive); 
            }
        }

        public string HasVotingRight(IDatabase database, Translator translator)
        {
            UpdateAllVotingRights(database);

            if (Memberships.Count < 1)
            {
                return translator.Get("Person.VotingRight.NotApplicable", "When voting right is not applicable because there is no membership", "N/A");
            }
            else if (Memberships.All(m => m.HasVotingRight.Value.Value))
            {
                return translator.Get("Person.VotingRight.Yes", "When the person has voting right in all of her memberships", "Yes");
            }
            else if (Memberships.Any(m => m.HasVotingRight.Value.Value))
            {
                return translator.Get("Person.VotingRight.Partial", "When the person has voting right in some but not all of her memberships", "Partial");
            }
            else
            {
                return translator.Get("Person.VotingRight.No", "When the person has voting right in none of her memberships", "No");
            }
        }

        public void UpdateAllVotingRights(IDatabase database)
        {
            foreach (var membership in Memberships)
            {
                if (!membership.HasVotingRight.Value.HasValue)
                {
                    membership.UpdateVotingRight(database);
                    database.Save(membership);
                }
            }
        }

        public long CreditsBalance(IDatabase database)
        {
            return database
                .Query<Credits>(DC.Equal("ownerid", Id.Value))
                .SumOrDefault(c => c.Amount.Value, 0);
        }

        public decimal MoneyBalance(IDatabase database)
        {
            return database
                .Query<Prepayment>(DC.Equal("personid", Id.Value))
                .SumOrDefault(c => c.Amount.Value, 0M);
        }

        public long PointsBalance(IDatabase database)
        {
            var tally = database.Query<PointsTally>(DC.Equal("personid", Id.Value))
                .OrderByDescending(t => t.UntilDate.Value)
                .FirstOrDefault();
            var tallyDate = tally != null ? tally.UntilDate.Value : DateTime.MinValue;
            var tallyForward = tally != null ? tally.ForwardBalance.Value : 0;
            return database
                .Query<Points>(DC.Equal("ownerid", Id.Value))
                .Where(p => p.Moment.Value > tallyDate)
                .SumOrDefault(t => t.Amount.Value, 0) +
                tallyForward;
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

        public decimal CurrentPrepayment(IDatabase database)
        {
            return database.Query<Prepayment>(DC.Equal("personid", Id.Value)).Sum(p => p.Amount);
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
                    return UserName; 
                }
            }
        }

        public string SortName
        {
            get
            {
                if (LastName.Value.Length > 0)
                {
                    var name = LastName.Value;

                    if (ShortFirstNames.Length > 0)
                    {
                        name += ", " + ShortFirstNames;
                    }

                    return name;
                }
                else
                {
                    return UserName;
                }
            }
        }

        public override string ToString()
        {
            return "Person " + ShortHand;
        }

        public string FirstOrUserName
        {
            get 
            {
                if (!string.IsNullOrEmpty(FirstName.Value))
                {
                    return FirstName.Value; 
                }
                else
                {
                    return UserName.Value;
                }
            }
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
            foreach (var address in database.Query<PostalAddress>(DC.Equal("personid", Id.Value)))
            {
                address.Delete(database); 
            }

            foreach (var address in database.Query<ServiceAddress>(DC.Equal("personid", Id.Value)))
            {
                address.Delete(database);
            }

            foreach (var publicKey in database.Query<PublicKey>(DC.Equal("personid", Id.Value)))
            {
                publicKey.Delete(database);
            }

            foreach (var membership in database.Query<Membership>(DC.Equal("personid", Id.Value)))
            {
                membership.Delete(database);
            }

            foreach (var roleAssignment in database.Query<RoleAssignment>(DC.Equal("personid", Id.Value)))
            {
                roleAssignment.Delete(database);
            }

            foreach (var tagAssignment in database.Query<TagAssignment>(DC.Equal("personid", Id.Value)))
            {
                tagAssignment.Delete(database);
            }

            foreach (var document in database.Query<Document>(DC.Equal("personid", Id.Value)))
            {
                document.Delete(database);
            }

            foreach (var entry in database.Query<JournalEntry>(DC.Equal("personid", Id.Value)))
            {
                entry.Delete(database);
            }

            foreach (var parameter in database.Query<PersonalPaymentParameter>(DC.Equal("personid", Id.Value)))
            {
                parameter.Delete(database);
            }

            foreach (var pointsTally in database.Query<PointsTally>(DC.Equal("personid", Id.Value)))
            {
                pointsTally.Delete(database);
            }

            foreach (var points in database.Query<Points>(DC.Equal("ownerid", Id.Value)))
            {
                points.Delete(database);
            }

            foreach (var authorization in database.Query<Oauth2Authorization>(DC.Equal("userid", Id.Value)))
            {
                authorization.Delete(database);
            }

            foreach (var session in database.Query<Oauth2Session>(DC.Equal("userid", Id.Value)))
            {
                session.Delete(database);
            }

            foreach (var prepayment in database.Query<Prepayment>(DC.Equal("personid", Id.Value)))
            {
                prepayment.Delete(database);
            }

            foreach (var settings in database.Query<SearchSettings>(DC.Equal("personid", Id.Value)))
            {
                settings.Delete(database);
            }

            foreach (var deviceSession in database.Query<DeviceSession>(DC.Equal("userid", Id.Value)))
            {
                deviceSession.Delete(database);
            }

            var reservedUserName = new ReservedUserName(Guid.NewGuid());
            reservedUserName.UserId.Value = Id.Value;
            reservedUserName.UserName.Value = UserName.Value;
            database.Save(reservedUserName);

            database.Delete(this); 
        }
    }
}

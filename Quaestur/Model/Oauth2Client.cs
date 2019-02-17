using System;
using System.Collections.Generic;

namespace Quaestur
{
    [Flags]
    public enum Oauth2ClientAccess
    {
        None = 0,
        Membership = 1,
        Email = 2,
        Fullname = 4,
        Roles = 8,
    }

    public static class Oauth2ClientAccessExtensions
    {
        public static string Translate(this Oauth2ClientAccess access, Translator translator)
        {
            switch (access)
            {
                case Oauth2ClientAccess.None:
                    return translator.Get("Enum.Oauth2ClientAccess.None", "None value in the OAuth2 client access flag enum", "None");
                case Oauth2ClientAccess.Membership:
                    return translator.Get("Enum.Oauth2ClientAccess.Membership", "Membership value in the OAuth2 client access flag enum", "Membership");
                case Oauth2ClientAccess.Email:
                    return translator.Get("Enum.Oauth2ClientAccess.Email", "E-Mail value in the OAuth2 client access flag enum", "E-Mail");
                case Oauth2ClientAccess.Fullname:
                    return translator.Get("Enum.Oauth2ClientAccess.Fullname", "Full name value in the OAuth2 client access flag enum", "Full name");
                case Oauth2ClientAccess.Roles:
                    return translator.Get("Enum.Oauth2ClientAccess.Roles", "Roles value in the OAuth2 client access flag enum", "Roles");
                default:
                    throw new NotSupportedException();
            }
        }
    }

    public class Oauth2Client : DatabaseObject
    {
		public MultiLanguageStringField Name { get; private set; }
        public StringField Secret { get; private set; }
        public StringField RedirectUri { get; private set; }
        public Field<bool> RequireTwoFactor { get; private set; }
        public EnumField<Oauth2ClientAccess> Access { get; private set; }

        public Oauth2Client() : this(Guid.Empty)
        {
        }

        public Oauth2Client(Guid id) : base(id)
        {
            Name = new MultiLanguageStringField(this, "name");
            Secret = new StringField(this, "secret", 256);
            RedirectUri = new StringField(this, "redirecturi", 256, AllowStringType.UnsecureText);
            RequireTwoFactor = new Field<bool>(this, "requiretwofactor", false);
            Access = new EnumField<Oauth2ClientAccess>(this, "access", Oauth2ClientAccess.None, Oauth2ClientAccessExtensions.Translate);
        }

        public override string ToString()
        {
            return Name.Value.AnyValue;
        }

        public override void Delete(IDatabase database)
        {
            foreach (var authorization in database.Query<Oauth2Authorization>(DC.Equal("clientid", Id.Value)))
            {
                authorization.Delete(database);
            }

            foreach (var session in database.Query<Oauth2Session>(DC.Equal("clientid", Id.Value)))
            {
                session.Delete(database); 
            }

            database.Delete(this);
        }

        public override string GetText(Translator translator)
        {
            return Name.Value[translator.Language];
        }
    }
}

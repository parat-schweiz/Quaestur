using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BaseLibrary;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using Nancy;
using Nancy.Helpers;
using Nancy.ModelBinding;
using Nancy.Responses.Negotiation;
using Nancy.Security;
using Newtonsoft.Json.Linq;
using SiteLibrary;

namespace Quaestur
{
    public class Oauth2AuthViewModel : MasterViewModel
    {
        public string Id;
        public string Data;
        public string Message;
        public string Scope;
        public string PhraseButtonAuthorize;
        public string PhraseButtonReject;

        public Oauth2AuthViewModel(IDatabase database, Translator translator, Session session, Oauth2Client client, string state, string nonce)
            : base(database, translator,
                   translator.Get("OAuth2.Title", "Title of the oauth2 page", "Authorization"),
                   session)
        {
            PhraseButtonAuthorize = translator.Get("OAuth2.Button.Authorize", "Authorize button on the OAuth2 page", "Authorize");
            PhraseButtonReject = translator.Get("OAuth2.Button.Reject", "Reject button on the OAuth2 page", "Reject");

            Id = client.Id.Value.ToString();
            Data = "state=" + HttpUtility.UrlEncode(state) + "&nonce=" + HttpUtility.UrlEncode(nonce ?? string.Empty);
            Message = translator.Get("OAuth2.Message", "Messge on the OAuth2 page", "{0} requests authorization to authenticate you and access your {1}. Do you wish to allow this?", 
                client.Name.Value[translator.Language], GetAccessString(translator, client));
        }

        private string GetAccessString(Translator translator, Oauth2Client client)
        {
            var list = new List<string>();
            list.Add(translator.Get("OAuth2.Message.Access.Username", "Username access in OAuth2 authorization message", "username"));

            foreach (Oauth2ClientAccess access in Enum.GetValues(typeof(Oauth2ClientAccess)))
            {
                if ((int)access > 0 &&
                    client.Access.Value.HasFlag(access))
                { 
                    switch (access)
                    {
                        case Oauth2ClientAccess.Membership:
                            list.Add(translator.Get("OAuth2.Message.Access.Membership", "Membership access in OAuth2 authorization message", "membership and voting rights"));
                            break;
                        case Oauth2ClientAccess.Email:
                            list.Add(translator.Get("OAuth2.Message.Access.Email", "Email access in OAuth2 authorization message", "e-mail address"));
                            break;
                        case Oauth2ClientAccess.Fullname:
                            list.Add(translator.Get("OAuth2.Message.Access.Fullname", "Fullname access in OAuth2 authorization message", "full name"));
                            break;
                        case Oauth2ClientAccess.Roles:
                            list.Add(translator.Get("OAuth2.Message.Access.Roles", "Roles access in OAuth2 authorization message", "assigned roles"));
                            break;
                    }
                }
            }

            return string.Join(", ", list);
        }
    }

    public class Oauth2AuthModule : QuaesturModule
    {
        public Negotiator OAuth2Error(string key, string hint, string technical)
        {
            return View["View/info.sshtml", new InfoViewModel(Database, Translator,
                Translate("Oauth2.Error.Title", "Errot title in OAuth2", "Error"),
                Translate(key, hint, technical),
                Translate("Oauth2.Error.BackLink", "Back link on oauth2 error page", "Back"),
                "/")];
        }

        public Oauth2AuthModule()
        {
            this.RequireCompleteLogin();

            base.Get("/oauth2/authorize/", parameters =>
            {
                string responseType = Request.Query["response_type"];
                if (responseType != "code")
                {
                    Global.Log.Notice("OAuth2: Response type {0} is not supported", responseType);
                    return OAuth2Error("Oauth2.Error.Text.ResponseType",
                                       "Unsupported response type text in OAuth2",
                                       "Response type is not supported");
                }

                string clientIdString = Request.Query["client_id"];
                if (!string.IsNullOrEmpty(clientIdString) &&
                    Guid.TryParse(clientIdString, out Guid clientId))
                {
                    var client = Database.Query<Oauth2Client>(clientId);

                    if (client == null)
                    {
                        Global.Log.Notice("OAuth2: Unknown client ID {0}", clientIdString);
                        return OAuth2Error("Oauth2.Error.Text.UnknownClientId",
                                           "Unkonwn client ID in OAuth2",
                                           "Unknown client ID");
                    }

                    if (client.RequireTwoFactor &&
                        !CurrentSession.TwoFactorAuth)
                    {
                        return OAuth2Error("Oauth2.Error.Text.TwoFactorRequired",
                            "Two-factor required text in OAuth2",
                            "Two-factor login must be activated to use this service.");
                    }

                    string redirectUri = Request.Query["redirect_uri"];
                    if (client.RedirectUri.Value != redirectUri)
                    {
                        Global.Log.Notice("OAuth2: Invalid redirect URI {0}", redirectUri);
                        return OAuth2Error("Oauth2.Error.Text.InvalidRedirectUri",
                                           "Invalid redirect URI in OAuth2",
                                           "Invalid redirect URI");
                    }

                    string state = Request.Query["state"] ?? string.Empty;
                    string nonce = Request.Query["nonce"] ?? string.Empty;
                    bool hasAuthorization = false;

                    using (var transaction = Database.BeginTransaction())
                    {
                        var authorization = Database.Query<Oauth2Authorization>(
                        DC.Equal("userid", CurrentSession.User.Id.Value)
                        .And(DC.Equal("clientid", client.Id.Value)))
                        .SingleOrDefault();

                        if (authorization != null &&
                            DateTime.UtcNow > authorization.Expiry)
                        {
                            authorization.Delete(Database);
                            authorization = null;
                        }

                        transaction.Commit();
                        hasAuthorization = authorization != null;
                    }

                    if (hasAuthorization)
                    {
                        Oauth2Session session = CreateSession(client, nonce);

                        string uri = CreateRedirectUrl(client, session, state);

                        return Response.AsRedirect(uri);
                    }
                    else
                    {
                        return View["View/oauth2auth.sshtml", new Oauth2AuthViewModel(Database, Translator, CurrentSession, client, state, nonce)];
                    }
                }
                else
                {
                    Global.Log.Notice("OAuth2: Bad client ID {0}", clientIdString);
                    return OAuth2Error("OAuth2.Error.Text.BadClientId",
                                       "Bad client ID in OAuth2",
                                       "Bad client ID");
                }
            });
            Post("/oauth2/callback/{id}", parameters =>
            {
                string idString = parameters.id;
                var client = Database.Query<Oauth2Client>(idString);

                if (client == null)
                {
                    Global.Log.Notice("OAuth2: Client not found on callback");
                    return string.Empty;
                }

                if (client.RequireTwoFactor &&
                    (!CurrentSession.TwoFactorAuth))
                {
                    Global.Log.Notice("OAuth2: Callback from client requiring 2FA without 2FA enabled");
                    return string.Empty;
                }

                using (var transaction = Database.BeginTransaction())
                {
                    var authorization = Database.Query<Oauth2Authorization>(
                        DC.Equal("userid", CurrentSession.User.Id.Value)
                        .And(DC.Equal("clientid", client.Id.Value)))
                        .SingleOrDefault();

                    if (authorization != null &&
                        DateTime.UtcNow > authorization.Expiry)
                    {
                        authorization.Delete(Database);
                        authorization = null;
                    }

                    if (authorization == null)
                    {
                        authorization = new Oauth2Authorization(Guid.NewGuid());
                        authorization.Client.Value = client;
                        authorization.User.Value = CurrentSession.User;
                        authorization.Moment.Value = DateTime.UtcNow;
                        authorization.Expiry.Value = DateTime.UtcNow.AddYears(2);
                        Database.Save(authorization);
                    }

                    transaction.Commit();
                }

                string state = Context.Request.Form.state ?? string.Empty;
                string nonce = Context.Request.Form.nonce ?? string.Empty;

                if (string.IsNullOrEmpty(state))
                {
                    Global.Log.Notice("OAuth2: Could not retrieve state from callback body");
                    return string.Empty;
                }

                Oauth2Session session = CreateSession(client, nonce);

                string uri = CreateRedirectUrl(client, session, state);

                return uri;
            });
        }

        private static string CreateRedirectUrl(Oauth2Client client, Oauth2Session session, string state)
        {
            var uri = new StringBuilder();
            uri.Append(client.RedirectUri.Value);

            if (client.RedirectUri.Value.Contains("?"))
            {
                uri.Append("&code=");
            }
            else
            {
                uri.Append("?code=");
            }

            uri.Append(session.AuthCode.Value);

            if (!string.IsNullOrEmpty(state))
            {
                uri.Append("&state=");
                uri.Append(state);
            }

            return uri.ToString();
        }

        private Oauth2Session CreateSession(Oauth2Client client, string nonce)
        {
            var session = new Oauth2Session(Guid.NewGuid());
            session.Client.Value = client;
            session.User.Value = CurrentSession.User;
            session.AuthCode.Value = Rng.Get(16).ToHexString();
            session.Token.Value = session.Id.Value.ToString() + "." + Rng.Get(16).ToHexString();
            session.Moment.Value = DateTime.UtcNow;
            session.Expiry.Value = DateTime.UtcNow.AddSeconds(client.SessionExpirySeconds.Value);
            session.Nonce.Value = nonce ?? string.Empty;
            Database.Save(session);

            Journal(session.User.Value,
                "Oauth2.Authenticated",
                "Authenticated client with OAuth2",
                "Client {0} authenticated using OAuth2",
                t => session.Client.Value.Name.Value[t.Language]);

            return session;
        }
    }

    public class Oauth2TokenPost
    {
        public string grant_type;
        public string code;
        public string redirect_uri;
        public string client_id;
        public string client_secret;
        public string state;
    }

    public class Oauth2SigningKey
    {
        private static Oauth2SigningKey _instance = null;

        public static Oauth2SigningKey Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Oauth2SigningKey();
                }

                return _instance;
            }
        }

        public Oauth2SigningKey()
        {
            Rsa = RSA.Create(2048);
            Id = Rng.Get(16).ToHexString();
        }

        public RSA Rsa { get; private set; }

        public string Id { get; private set; }

        public RS256Algorithm Algo(bool withPrivateKey)
        {
            if (withPrivateKey)
            {
                return new RS256Algorithm(Rsa, Rsa);
            }
            else
            {
                return new RS256Algorithm(Rsa);
            }
        }
    }

    public class Oauth2TokenModule : QuaesturModule
    {
        private void ExpireSessions()
        {
            foreach (var session in Database
                .Query<Oauth2Session>()
                .Where(s => DateTime.UtcNow > s.Expiry.Value))
            {
                session.Delete(Database);
            }
        }

        private Oauth2Session FindSession(string token)
        {
            var parts = token.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2 &&
                Guid.TryParse(parts[0], out Guid sessionId))
            {
                var session = Database.Query<Oauth2Session>(sessionId);

                if (session != null &&
                    session.Token.Value == token)
                {
                    return session; 
                }
            }

            return null;
        }

        private Oauth2Session FindSession()
        {
            string authorization = Request.Headers.Authorization;

            if (!string.IsNullOrEmpty(authorization) &&
                authorization.StartsWith("Bearer ", StringComparison.Ordinal))
            {
                var token = authorization.Substring("Bearer ".Length);
                return FindSession(token);
            }

            return null;
        }

        private Oauth2Client Authenticate(Oauth2TokenPost post)
        {
            var header = Request.Headers["Authorization"].FirstOrDefault();

            if (!string.IsNullOrEmpty(header) &&
                header.StartsWith("Basic ", StringComparison.InvariantCulture))
            {
                var text = Encoding.UTF8.GetString(Convert.FromBase64String(header.Substring(6)));
                var parts = text.Split(new string[] { ":" }, StringSplitOptions.None);
                if (parts.Length == 2 &&
                    Guid.TryParse(parts[0], out Guid clientId))
                {
                    var client = Database
                        .Query<Oauth2Client>(DC.Equal("id", clientId))
                        .SingleOrDefault();

                    if (client != null &&
                        client.Secret.Value == parts[1] &&
                        client.RedirectUri.Value == post.redirect_uri)
                    {
                        return client;
                    }
                }
            }
            else if (!string.IsNullOrEmpty(post.client_id) &&
                Guid.TryParse(post.client_id, out Guid clientId))
            {
                var client = Database
                    .Query<Oauth2Client>(DC.Equal("id", clientId))
                    .SingleOrDefault();

                if (client != null &&
                    client.Secret.Value == post.client_secret &&
                    client.RedirectUri.Value == post.redirect_uri)
                {
                    return client;
                }
            }

            return null;
        }

        public Oauth2TokenModule()
        {
            Get("/.well-known/openid-configuration", parameters =>
            {
                var response = new JObject(
                    new JProperty("issuer", Global.Config.WebSiteAddress),
                    new JProperty("authorization_endpoint", string.Format("{0}/oauth2/authorize", Global.Config.WebSiteAddress)),
                    new JProperty("token_endpoint", string.Format("{0}/oauth2/token", Global.Config.WebSiteAddress)),
                    new JProperty("jwks_uri", string.Format("{0}/oauth2/jwk.json", Global.Config.WebSiteAddress)),
                    new JProperty("response_types_supported", new JArray("code")),
                    new JProperty("subject_types_supported", new JArray("public")),
                    new JProperty("id_token_signing_alg_values_supported", new JArray("RS256")),
                    new JProperty("userinfo_endpoint", string.Format("{0}/api/v1/user/profile", Global.Config.WebSiteAddress)));
                return Response.AsText(response.ToString(), "application/json");
            });
            Get("/oauth2/jwk.json", parameters =>
            {
                var encoder = new JwtBase64UrlEncoder();
                var rsa = Oauth2SigningKey.Instance.Rsa.ExportParameters(false);
                var response = new JObject(
                    new JProperty("keys", new JArray(
                        new JObject(
                            new JProperty("kid", Oauth2SigningKey.Instance.Id),
                            new JProperty("kty", "RSA"),
                            new JProperty("alg", "RS256"),
                            new JProperty("use", "sig"),
                            new JProperty("n", encoder.Encode(rsa.Modulus)),
                            new JProperty("e", encoder.Encode(rsa.Exponent))))));
                return Response.AsText(response.ToString(), "application/json");
            });
            Post("/oauth2/token", parameters =>
            {
                ExpireSessions();
                var post = this.Bind<Oauth2TokenPost>();
                var client = Authenticate(post);

                if (client != null &&
                    post.grant_type == "authorization_code")
                {
                    var session = Database
                        .Query<Oauth2Session>(DC.Equal("clientid", client.Id.Value))
                        .SingleOrDefault(s => s.AuthCode.Value == post.code);

                    if (session != null)
                    {
                        int expiry = (int)Math.Floor(DateTime.UtcNow.AddHours(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                        int issueTime = (int)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                        int authTime = (int)Math.Floor(session.Moment.Value.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

                        var builder = new JWT.Builder.JwtBuilder()
                            .WithAlgorithm(Oauth2SigningKey.Instance.Algo(true))
                            .AddHeader(HeaderName.KeyId, Oauth2SigningKey.Instance.Id)
                            .AddClaim("iss", Global.Config.WebSiteAddress)
                            .AddClaim("sub", session.User.Value.Id.Value.ToString())
                            .AddClaim("aud", session.Client.Value.Id.Value.ToString())
                            .AddClaim("name", session.User.Value.UserName.Value)
                            .AddClaim("exp", expiry)
                            .AddClaim("iat", issueTime)
                            .AddClaim("auth_time", authTime);

                        if (!string.IsNullOrEmpty(session.Nonce.Value))
                        {
                            builder = builder
                                .AddClaim("nonce", session.Nonce.Value);
                        }

                        if (session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Email))
                        {
                            builder = builder
                                .AddClaim("email", session.User.Value.PrimaryMailAddress);
                        }

                        if (session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Fullname))
                        {
                            builder = builder
                                .AddClaim("fullname", session.User.Value.FullName)
                                .AddClaim("firstname", session.User.Value.FirstName.Value)
                                .AddClaim("lastname", session.User.Value.LastName.Value);
                        }

                        var idToken = builder.Encode();
                        var response = new JObject(
                            new JProperty("access_token", session.Token.Value),
                            new JProperty("token_type", "Bearer"),
                            new JProperty("expires_in", 3600),
                            new JProperty("id_token", idToken.ToString()));
                        Global.Log.Notice("OAuth2 token issued for user {0}", session.User.Value.ShortHand);
                        return Response.AsText(response.ToString(), "application/json");
                    }
                }

                Global.Log.Notice("Invalid OAuth2 token request");
                var error = new JObject(
                    new JProperty("error", "invalid_request"));
                return Response.AsText(error.ToString(), "application/json");
            });
            Get("/api/v1/user/auid/", parameters =>
            {
                var session = FindSession();

                if (session != null)
                {
                    var response = new JObject(
                        new JProperty("auid", session.User.Value.Id.Value.ToString()));
                    return Response.AsText(response.ToString(), "application/json");
                }

                var error = new JObject(
                    new JProperty("error", "invalid_request"));
                return Response.AsText(error.ToString(), "application/json");
            });
            Get("/api/v1/user/profile/", parameters =>
            {
                var session = FindSession();

                if (session != null)
                {
                    var response = new JObject(
                        new JProperty("username", session.User.Value.UserName.Value),
                        new JProperty("iss", Global.Config.WebSiteAddress),
                        new JProperty("sub", session.User.Value.Id.Value.ToString()),
                        new JProperty("aud", session.Client.Value.Id.Value.ToString()),
                        new JProperty("name", session.User.Value.UserName.Value),
                        new JProperty("nickname", session.User.Value.UserName.Value),
                        new JProperty("preferred_username", session.User.Value.UserName.Value));

                    if (session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Email))
                    {
                        response.Add(new JProperty("mail", session.User.Value.PrimaryMailAddress));
                        response.Add(new JProperty("email", session.User.Value.PrimaryMailAddress));
                    }

                    if (session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Fullname))
                    {
                        response.Add(new JProperty("fullname", session.User.Value.FullName));
                        response.Add(new JProperty("firstname", session.User.Value.FirstName.Value));
                        response.Add(new JProperty("lastname", session.User.Value.LastName.Value));
                        response.Add(new JProperty("given_name", session.User.Value.FirstName.Value));
                        response.Add(new JProperty("family_name", session.User.Value.LastName.Value));
                    }
                    else
                    {
                        response.Add(new JProperty("fullname", session.User.Value.UserName.Value));
                        response.Add(new JProperty("firstname", session.User.Value.UserName.Value));
                        response.Add(new JProperty("lastname", session.User.Value.UserName.Value));
                        response.Add(new JProperty("given_name", session.User.Value.UserName.Value));
                        response.Add(new JProperty("family_name", session.User.Value.UserName.Value));
                    }

                    return Response.AsText(response.ToString(), "application/json");
                }

                var error = new JObject(
                    new JProperty("error", "invalid_request"));
                return Response.AsText(error.ToString(), "application/json");
            });
            Get("/api/v1/roles/", parameters =>
            {
                var session = FindSession();

                if (session != null)
                {
                    var list = new List<JObject>();

                    foreach (var role in Database.Query<Role>())
                    {
                        list.Add(
                            new JObject(
                                new JProperty("id", role.Id.Value),
                                new JProperty("name", 
                                    (role.Group.Value.Organization.Value.Name.Value + " / " +
                                    role.Group.Value.Name.Value + " / " + role.Name.Value).ToJson())));
                    }

                    var response = new JArray(list);

                    return Response.AsText(response.ToString(), "application/json");
                }

                var error = new JObject(
                    new JProperty("error", "invalid_request"));
                return Response.AsText(error.ToString(), "application/json");
            });
            Get("/api/v1/user/membership/", parameters =>
            {
                var session = FindSession();

                if (session != null &&
                    session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Membership))
                {
                    var response = new JObject(
                        new JProperty("type", GetUserType(session.User.Value)),
                        new JProperty("verified", false),
                        new JProperty("nested_groups", GetUserGroups(session.User.Value)),
                        new JProperty("all_nested_groups", GetUserGroups(session.User.Value)));
                    return Response.AsText(response.ToString(), "application/json");
                }

                return string.Empty;
            });
            Get("/api/v1/user/roles/", parameters =>
            {
                var session = FindSession();

                if (session != null &&
                    session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Roles))
                {
                    var response = new JArray(session.User.Value.RoleAssignments
                        .Select(ra => ra.Role.Value.Id.ToString()));
                    return Response.AsText(response.ToString(), "application/json");
                }

                return string.Empty;
            });
        }

        private JArray GetUserGroups(Person person)
        {
            return new JArray(person.Memberships.Select(m => m.Organization.Value.Name.Value[Language.English]));
        }

        private string GetUserType(Person person)
        {
            foreach (var membership in person.Memberships)
            {
                if (!membership.HasVotingRight.Value.HasValue)
                {
                    membership.UpdateVotingRight(Database); 
                }
            }

            if (person.Memberships.Any(m => m.HasVotingRight.Value.Value))
            {
                return "eligible member";
            }
            else if (person.Memberships.Any(m => m.Type.Value.Rights.Value.HasFlag(MembershipRight.Voting)))
            {
                return "plain member";
            }
            else
            {
                return "guest"; 
            }
        }
    }
}

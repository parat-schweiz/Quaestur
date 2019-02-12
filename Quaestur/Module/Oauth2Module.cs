using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Responses;
using Nancy.Security;
using Nancy.Responses.Negotiation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Quaestur
{
    public class Oauth2AuthViewModel : MasterViewModel
    {
        public string Id;
        public string State;
        public string Message;
        public string Scope;
        public string PhraseButtonAuthorize;
        public string PhraseButtonReject;

        public Oauth2AuthViewModel(Translator translator, Session session, Oauth2Client client, string state)
            : base(translator,
                   translator.Get("OAuth2.Title", "Title of the oauth2 page", "Authorization"),
                   session)
        {
            PhraseButtonAuthorize = translator.Get("OAuth2.Button.Authorize", "Authorize button on the OAuth2 page", "Authorize");
            PhraseButtonReject = translator.Get("OAuth2.Button.Reject", "Reject button on the OAuth2 page", "Reject");

            Id = client.Id.Value.ToString();
            State = state;
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
            return View["View/info.sshtml", new InfoViewModel(Translator,
                Translate("Oauth2.Error.Title", "Errot title in OAuth2", "Error"),
                Translate(key, hint, technical),
                Translate("Oauth2.Error.BackLink", "Back link on oauth2 error page", "Back"),
                "/")];
        }

        public Oauth2AuthModule()
        {
            this.RequiresAuthentication();

            Get["/oauth2/authorize/"] = parameters =>
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
                        CurrentSession.CompleteAuth)
                    {
                        CurrentSession.ReturnUrl = "/oauth2/authorize/" + Request.Url.Query;
                        return Response.AsRedirect("/twofactor/auth"); 
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
                        Oauth2Session session = CreateSession(client);

                        var uri = string.IsNullOrEmpty(state) ?
                            string.Format("{0}?code={1}",
                                client.RedirectUri.Value,
                                session.AuthCode.Value) :
                            string.Format("{0}?code={1}&state={2}",
                                client.RedirectUri.Value,
                                session.AuthCode.Value,
                                state);

                        return Response.AsRedirect(uri);
                    }
                    else
                    {
                        return View["View/oauth2auth.sshtml", new Oauth2AuthViewModel(Translator, CurrentSession, client, state)];
                    }
                }
                else
                {
                    Global.Log.Notice("OAuth2: Bad client ID {0}", clientIdString);
                    return OAuth2Error("OAuth2.Error.Text.BadClientId",
                                       "Bad client ID in OAuth2",
                                       "Bad client ID");
                }
            };
            Post["/oauth2/callback/{id}"] = parameters =>
            {
                string idString = parameters.id;
                var client = Database.Query<Oauth2Client>(idString);

                if (client != null)
                {
                    if (client.RequireTwoFactor &&
                        CurrentSession.CompleteAuth)
                    {
                        return null;
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
                            authorization.Expiry.Value = DateTime.UtcNow.AddDays(180);
                            Database.Save(authorization);
                        }

                        transaction.Commit();
                    }

                    var state = ReadBody();

                    Oauth2Session session = CreateSession(client);

                    var uri = string.IsNullOrEmpty(state) ?
                        string.Format("{0}?code={1}",
                            client.RedirectUri.Value,
                            session.AuthCode.Value) :
                        string.Format("{0}?code={1}&state={2}",
                            client.RedirectUri.Value,
                            session.AuthCode.Value,
                            state);

                    return uri;
                }

                return null;
            };
        }

        private Oauth2Session CreateSession(Oauth2Client client)
        {
            var session = new Oauth2Session(Guid.NewGuid());
            session.Client.Value = client;
            session.User.Value = CurrentSession.User;
            session.AuthCode.Value = Rng.Get(16).ToHexString();
            session.Token.Value = session.Id.Value.ToString() + "." + Rng.Get(16).ToHexString();
            session.Moment.Value = DateTime.UtcNow;
            session.Expiry.Value = DateTime.UtcNow.AddHours(1);
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

    public class Oauth2TokenModule : QuaesturModule
    {
        private void ExpireSessions()
        {
            foreach (var session in Database
                .Query<Oauth2Session>()
                .Where(s => DateTime.UtcNow > s.Expiry.Value))
            {
                Database.Delete(session);
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

        public Oauth2TokenModule()
        {
            Post["/oauth2/token"] = parameters =>
            {
                ExpireSessions();
                var post = this.Bind<Oauth2TokenPost>();

                if (post.grant_type == "authorization_code" &&
                    !string.IsNullOrEmpty(post.client_id) &&
                    Guid.TryParse(post.client_id, out Guid clientId))
                {
                    var session = Database
                        .Query<Oauth2Session>(DC.Equal("clientid", clientId))
                        .SingleOrDefault(s => s.AuthCode.Value == post.code);

                    if (session != null &&
                        session.Client.Value.RedirectUri.Value == post.redirect_uri &&
                        session.Client.Value.Secret.Value == post.client_secret)
                    {
                        int expiry = (int)Math.Floor(DateTime.UtcNow.AddHours(1).Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                        int issueTime = (int)Math.Floor(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                        int authTime = (int)Math.Floor(session.Moment.Value.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);

                        var idToken = new JWT.Builder.JwtBuilder()
                            .WithAlgorithm(new JWT.Algorithms.HMACSHA256Algorithm())
                            .WithSecret(session.Client.Value.Secret.Value)
                            .AddClaim("iss", Global.Config.WebSiteAddress)
                            .AddClaim("sub", session.User.Value.Id.Value.ToString())
                            .AddClaim("aud", session.Client.Value.Id.Value.ToString())
                            .AddClaim("exp", expiry)
                            .AddClaim("iat", issueTime)
                            .AddClaim("auth_time", authTime)
                            .Build();
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
            };
            Get["/api/v1/user/auid/"] = parameters =>
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
            };
            Get["/api/v1/user/profile/"] = parameters =>
            {
                var session = FindSession();

                if (session != null)
                {
                    var response = new JObject(
                        new JProperty("username", session.User.Value.UserName.Value));

                    if (session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Email))
                    {
                        response.Add(new JProperty("mail", session.User.Value.PrimaryMailAddress));
                    }

                    if (session.Client.Value.Access.Value.HasFlag(Oauth2ClientAccess.Fullname))
                    {
                        response.Add(new JProperty("fullname", session.User.Value.FullName));
                        response.Add(new JProperty("firstname", session.User.Value.FirstName));
                        response.Add(new JProperty("lastname", session.User.Value.LastName));
                    }

                    return Response.AsText(response.ToString(), "application/json");
                }

                var error = new JObject(
                    new JProperty("error", "invalid_request"));
                return Response.AsText(error.ToString(), "application/json");
            };
            Get["/api/v1/user/membership/"] = parameters =>
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

                return null;
            };
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

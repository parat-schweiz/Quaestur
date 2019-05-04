using Nancy;
using Nancy.Hosting.Self;
using Nancy.ModelBinding;
using Nancy.Authentication.Forms;
using Nancy.Security;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BaseLibrary;
using SiteLibrary;

namespace Publicus
{
    public class LoginModule : PublicusModule
    {
        private const string StateTag = "oauth2state";

        private string ValidateReturnUrl(string returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) &&
                returnUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return returnUrl;
            }
            else
            {
                return string.Empty;
            }
        }

        private byte[] Mac(DateTime time, string returnUrl)
        {
            using (var serializer = new Serializer())
            {
                serializer.WritePrefixed(StateTag);
                serializer.Write(time);
                serializer.WritePrefixed(returnUrl);

                using (var hmac = new HMACSHA256())
                {
                    hmac.Key = Global.Config.LinkKey;
                    return hmac.ComputeHash(serializer.Data);
                }
            }
        }

        private string CreateState(string returnUrl)
        {
            var time = DateTime.UtcNow;
            var stateString = string.Format("{0};{1};{2}", time.Ticks, returnUrl, Mac(time, returnUrl).ToHexString());
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(stateString));
        }

        private string VerifyState(string state)
        {
            try
            {
                var stateString = Encoding.UTF8.GetString(Convert.FromBase64String(state));
                var parts = stateString.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3) return null;
                var macBytes = parts[2].TryParseHexBytes();
                var returnUrl = ValidateReturnUrl(parts[1]);

                if (long.TryParse(parts[0], out long ticks) &&
                    (!string.IsNullOrEmpty(returnUrl)) &&
                    (macBytes != null))
                {
                    var time = new DateTime(ticks, DateTimeKind.Utc);

                    if (Mac(time, returnUrl).AreEqual(macBytes) &&
                        (DateTime.UtcNow > time) &&
                        (DateTime.UtcNow.Subtract(time).TotalMinutes < 15d))
                    {
                        return returnUrl;
                    }
                }

                return null;
            }
            catch
            {
                return null; 
            }
        }

        private string HttpPost(string url, string requestData)
        {
            var content = new StringContent(requestData);
            content.Headers.Remove("Content-Type");
            content.Headers.Add("Content-Type", "application/json");

            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(url);
            request.Content = content;

            var client = new HttpClient();
            var waitResponse = client.SendAsync(request);
            waitResponse.Wait();
            var response = waitResponse.Result;

            var waitRead = response.Content.ReadAsByteArrayAsync();
            waitRead.Wait();
            var responseText = Encoding.UTF8.GetString(waitRead.Result);
            return responseText;
        }

        private string HttpGetAuthorized(string url, string token)
        {
            var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.RequestUri = new Uri(url);
            request.Headers.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);

            var client = new HttpClient();
            var waitResponse = client.SendAsync(request);
            waitResponse.Wait();
            var response = waitResponse.Result;

            var waitRead = response.Content.ReadAsByteArrayAsync();
            waitRead.Wait();
            var responseText = Encoding.UTF8.GetString(waitRead.Result);
            return responseText;
        }

        private enum ApiUrl
        { 
            UserAuid = 1,
            UserProfile = 2,
            UserRoles = 3,
            AllRoles = 4,
        }

        private string GetApiUrl(ApiUrl url)
        {
            switch (url)
            {
                case ApiUrl.UserAuid:
                    return Global.Config.Oauth2.OAuth2ApiUrl + "user/auid/";
                case ApiUrl.UserProfile:
                    return Global.Config.Oauth2.OAuth2ApiUrl + "user/profile/";
                case ApiUrl.UserRoles:
                    return Global.Config.Oauth2.OAuth2ApiUrl + "user/roles/";
                case ApiUrl.AllRoles:
                    return Global.Config.Oauth2.OAuth2ApiUrl + "roles/";
                default:
                    throw new NotSupportedException();
            }
        }

        private void UpdateMasterRoles(string token)
        {
            var responseData = HttpGetAuthorized(GetApiUrl(ApiUrl.AllRoles), token);
            var responseArray = JArray.Parse(responseData);
            var masterRoles = Database.Query<MasterRole>().ToList();
            var usedMasterRoles = new List<MasterRole>();

            foreach (var roleObject in responseArray.Values<JObject>())
            {
                var id = Guid.Parse(roleObject.Property("id").Value.Value<string>());
                var name = new MultiLanguageString(roleObject.Property("name").Value.Value<JArray>());

                var masterRole = masterRoles.FirstOrDefault(mr => mr.Id.Value.Equals(id));

                if (masterRole == null)
                {
                    masterRole = new MasterRole(id);
                    masterRole.Name.Value = name;
                    Database.Save(masterRole);
                }
                else if (masterRole.Name.Value != name)
                {
                    masterRole.Name.Value = name;
                    Database.Save(masterRole);
                }

                usedMasterRoles.Add(masterRole);
            }

            foreach (var masterRole in masterRoles)
            {
                if (!usedMasterRoles.Contains(masterRole))
                {
                    masterRole.Delete(Database); 
                }
            }
        }

        private void CreateDefaultStructure()
        {
            using (var transaction = Database.BeginTransaction())
            {
                if (!Database.Query<Feed>().Any())
                {
                    var feed = new Feed(Guid.NewGuid());
                    feed.Name.Value[Language.English] = "Default Feed";
                    Database.Save(feed);

                    var group = new Group(Guid.NewGuid());
                    group.Name.Value[Language.English] = "Default Group";
                    group.Feed.Value = feed;
                    Database.Save(group);

                    var role = new Role(Guid.NewGuid());
                    role.Name.Value[Language.English] = "Default Role";
                    role.Group.Value = group;
                    Database.Save(role);

                    foreach (var masterRole in Database.Query<MasterRole>())
                    {
                        var roleAssingment = new RoleAssignment(Guid.NewGuid());
                        roleAssingment.Role.Value = role;
                        roleAssingment.MasterRole.Value = masterRole;
                        Database.Save(roleAssingment);
                    }

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
                }

                transaction.Commit();
            }
        }

        private void AddPermission(Role role, SubjectAccess subject, PartAccess part, AccessRight right)
        {
            var permission = new Permission(Guid.NewGuid());
            permission.Role.Value = role;
            permission.Subject.Value = subject;
            permission.Part.Value = part;
            permission.Right.Value = right;
            Database.Save(permission);
        }

        public LoginModule()
        {
            Get["/login"] = parameters =>
            {
                var returnUrl = ValidateReturnUrl(Request.Query["returnUrl"]);
                string oauthUrl = 
                    string.Format("{0}?response_type=code&client_id={1}&state={2}&redirect_uri={3}",
                        Global.Config.Oauth2.OAuth2AuthorizationUrl,
                        Global.Config.Oauth2.OAuth2ClientId,
                        CreateState(returnUrl),
                        Nancy.Helpers.HttpUtility.UrlEncode(Global.Config.WebSiteAddress + "/login/redirect"));
               return Response.AsRedirect(oauthUrl);
            };
            Get["/login/redirect"] = parameters =>
            {
                string state = Request.Query["state"] ?? string.Empty;
                string code = Request.Query["code"] ?? string.Empty;
                string returnUrl = VerifyState(state);

                if (string.IsNullOrEmpty(code) ||
                    string.IsNullOrEmpty(returnUrl))
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Login.Redirect.Invalid.Title", "Title of the message when login redirect is invalid", "Invalid request"),
                        Translate("Login.Redirect.Invalid.Message", "Text of the message when login redirect is invalid", "Request is invalid."),
                        Translate("Login.Redirect.Invalid.BackLink", "Link text of the message when login redirect is invalid", "Back"),
                        "/")];
                }

                var requestObject = new JObject(
                    new JProperty("grant_type", "authorization_code"),
                    new JProperty("code", code),
                    new JProperty("redirect_uri", Global.Config.WebSiteAddress + "/login/redirect"),
                    new JProperty("client_id", Global.Config.Oauth2.OAuth2ClientId),
                    new JProperty("client_secret", Global.Config.Oauth2.OAuth2ClientSecret),
                    new JProperty("state", state));

                try
                {
                    var authResponseText = HttpPost(Global.Config.Oauth2.OAuth2TokenUrl, requestObject.ToString());
                    var authResponseObject = JObject.Parse(authResponseText);
                    var accessToken = authResponseObject.Property("access_token").Value.Value<string>();
                    var tokenType = authResponseObject.Property("token_type").Value.Value<string>();

                    if (tokenType != "Bearer")
                    {
                        return View["View/info.sshtml", new InfoViewModel(Translator,
                            Translate("Login.Redirect.Invalid.Title", "Title of the message when login redirect is invalid", "Invalid request"),
                            Translate("Login.Redirect.Invalid.Message", "Text of the message when login redirect is invalid", "Request is invalid."),
                            Translate("Login.Redirect.Invalid.BackLink", "Link text of the message when login redirect is invalid", "Back"),
                            "/")];
                    }

                    UpdateMasterRoles(accessToken);
                    CreateDefaultStructure();

                    var auidResponseText = HttpGetAuthorized(GetApiUrl(ApiUrl.UserAuid), accessToken);
                    var auidResponseObject = JObject.Parse(auidResponseText);
                    var auid = Guid.Parse(auidResponseObject.Property("auid").Value.Value<string>());

                    var profileResponseText = HttpGetAuthorized(GetApiUrl(ApiUrl.UserProfile), accessToken);
                    var profileResponseObject = JObject.Parse(profileResponseText);
                    var username = profileResponseObject.Property("username").Value.Value<string>();

                    var rolesResponseText = HttpGetAuthorized(GetApiUrl(ApiUrl.UserRoles), accessToken);
                    var rolesResponseArray = JArray.Parse(rolesResponseText);
                    var masterRolesIds = rolesResponseArray.Values<string>()
                        .Select(Guid.Parse).ToList();
                    var masterRoles = Database.Query<MasterRole>()
                        .Where(mr => masterRolesIds.Contains(mr.Id.Value))
                        .ToList();

                    var user = Database.Query<User>(auid);

                    if (user == null)
                    {   
                        user = new User(Guid.NewGuid());
                        user.UserName.Value = username;
                        Database.Save(user);
                    }

                    var session = Global.Sessions.Add(user, masterRoles);

                    return this.LoginAndRedirect(session.Id, DateTime.Now.AddDays(1), returnUrl);
                }
                catch
                {
                    return View["View/info.sshtml", new InfoViewModel(Translator,
                        Translate("Login.Redirect.Invalid.Title", "Title of the message when login redirect is invalid", "Invalid request"),
                        Translate("Login.Redirect.Invalid.Message", "Text of the message when login redirect is invalid", "Request is invalid."),
                        Translate("Login.Redirect.Invalid.BackLink", "Link text of the message when login redirect is invalid", "Back"),
                        "/")];
                }
            };
            Get["/logout"] = parameters =>
            {
                if (CurrentSession != null)
                {
                    Global.Sessions.Remove(CurrentSession);
                }

                return Response.AsRedirect("/");
            };
        }
    }
}

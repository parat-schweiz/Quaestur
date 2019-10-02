using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using Nancy;
using Nancy.ModelBinding;
using Nancy.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using BaseLibrary;
using MimeKit;
using SiteLibrary;

namespace Quaestur
{
    public class MatrixAuthModule : QuaesturModule
    {
        public MatrixAuthModule()
        {
            Post("/_matrix-internal/identity/v1/check_credentials", parameters =>
            {
                var input = JObject.Parse(ReadBody());
                var user = input.Value<JObject>("user");
                var id = user.Value<string>("id");
                var password = user.Value<string>("password");

                var match = Regex.Match(id, @"^@([a-zA-Z0-9_\-\.]+):([a-zA-Z0-9_\-\.]+)$");

                if (!match.Success &&
                    match.Groups.Count < 3)
                {
                    Global.Log.Notice("Invalid id at matrix login: {0}", id);
                    var error = new JObject(
                        new JProperty("auth",
                            new JObject(
                                new JProperty("success", false))));
                    return Response.AsText(error.ToString(), "application/json");
                }

                var userName = match.Groups[1].Value.ToLower();
                var domain = match.Groups[2].Value;

                if (!Global.Config.MatrixDomains.Contains(domain))
                {
                    Global.Log.Notice("Invalid domain at matrix login: {0}", domain);
                    var error = new JObject(
                        new JProperty("auth",
                            new JObject(
                                new JProperty("success", false))));
                    return Response.AsText(error.ToString(), "application/json");
                }

                Global.Throttle.Check(userName, false);
                var result = UserController.Login(Database, userName, password);

                switch (result.Item2)
                {
                    case LoginResult.WrongLogin:
                        Global.Log.Notice("Wrong matrix login with username {0}", userName);
                        Global.Throttle.Fail(userName, false);
                        break;
                    case LoginResult.Locked:
                        Global.Log.Notice("Matrx login denied due to locked user {0}", result.Item1.UserName);
                        break;
                    case LoginResult.Success:
                        break;
                    default:
                        throw new NotSupportedException();
                }


                if (result.Item2 != LoginResult.Success)
                {
                    var error = new JObject(
                        new JProperty("auth",
                            new JObject(
                                new JProperty("success", false))));
                    return Response.AsText(error.ToString(), "application/json");
                }

                var person = result.Item1;
                Journal(Translate(
                    "Password.Journal.MatrixAuth.Process",
                    "Journal entry subject on matrix authentication",
                    "Matrix login Process"),
                    person,
                    "Password.Journal.MatrixAuth.Success",
                    "Journal entry when matrix authentication with password succeeded",
                    "Matrix login with password succeeded");

                var succes = new JObject(
                    new JProperty("auth",
                        new JObject(
                            new JProperty("success", true),
                            new JProperty("mxid", id),
                            new JProperty("profile",
                                new JObject(
                                    new JProperty("display_name", person.UserName.Value),
                                    new JProperty("three_pids", new JArray()))))));
                return Response.AsText(succes.ToString(), "application/json");
            });
        }
    }
}

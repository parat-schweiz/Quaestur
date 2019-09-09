using System;
using Nancy;
using Nancy.Helpers;
using Nancy.Responses;

namespace Quaestur
{
    public static class RequireCompleteAuthenticationModule
    {
        public static void RequireCompleteAuthentication(this NancyModule module)
        {
            module.Before.AddItemToEndOfPipeline(RequireCompleteAuthentication);
        }

        private static Response RequireCompleteAuthentication(NancyContext context)
        {
            Response response = null;

            if ((context.CurrentUser == null) ||
                (!context.CurrentUser.HasClaim(c => c.Type == Session.AuthenticationClaim && c.Value == Session.AuthenticationClaimComplete)))
            {
                var returnUrl = "/login?returnUrl=" + HttpUtility.UrlEncode(context.Request.Url.Path);
                response = new RedirectResponse(returnUrl, RedirectResponse.RedirectType.Temporary);
            }

            return response;
        }
    }
}

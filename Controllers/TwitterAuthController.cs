using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Tweetinvi;
using Tweetinvi.Auth;
using Tweetinvi.Parameters;

namespace TickerTracker.Controllers
{
    public class TwitterAuthController : Controller
    {
        private static readonly IAuthenticationRequestStore _RequestStore = new LocalAuthenticationRequestStore();

        public async Task<IActionResult> Redirect()
        {
            var appClient = new TwitterClient(
                    Models.Util.getEnv("TWITTER_CONSUMER_KEY"),
                    Models.Util.getEnv("TWITTER_CONSUMER_SECRET"));
            var authenticationRequestId = Guid.NewGuid().ToString();
            var redirectPath = Models.Util.Url("/auth/callback", Request);

            // Add the user identifier as a query parameters that will be received by `ValidateTwitterAuth`
            var redirectURL = _RequestStore.AppendAuthenticationRequestIdToCallbackUrl(redirectPath, authenticationRequestId);
            // Initialize the authentication process
            var authenticationRequestToken = await appClient.Auth.RequestAuthenticationUrlAsync(redirectURL);
            // Store the token information in the store
            await _RequestStore.AddAuthenticationTokenAsync(authenticationRequestId, authenticationRequestToken);

            // Redirect the user to Twitter
            return new RedirectResult(authenticationRequestToken.AuthorizationURL);
        }

        public async Task<ActionResult> Callback()
        {
            var appClient = new TwitterClient(
                    Models.Util.getEnv("TWITTER_CONSUMER_KEY"),
                    Models.Util.getEnv("TWITTER_CONSUMER_SECRET"));

            Tweetinvi.Models.IAuthenticatedUser twitterUser;
            Tweetinvi.Models.ITwitterCredentials userCreds;

            try
            {
                // Extract the information from the redirection url
                var requestParameters = await RequestCredentialsParameters.FromCallbackUrlAsync(Request.QueryString.Value, _RequestStore);
                // Request Twitter to generate the credentials.
                userCreds = await appClient.Auth.RequestCredentialsAsync(requestParameters);

                // Congratulations the user is now authenticated!
                var userClient = new TwitterClient(userCreds);
                twitterUser = await userClient.Users.GetAuthenticatedUserAsync();
            } catch( Exception )
            {
                Response.StatusCode = 500;
                return View("~/Views/HttpError/_500.cshtml");
            }

            var User = new Models.User {
                Id = twitterUser.Id,
                Handle = twitterUser.ScreenName,
                Name = twitterUser.Name,
                Avatar = twitterUser.ProfileImageUrl,
                Token = userCreds.AccessToken,
                Secret = userCreds.AccessTokenSecret,
                SessionId = Models.Util.genToken(32),
            };

            try
            {
                await User.Save();

                // save session
                Response.Cookies.Append("sid", User.SessionId,
                    new CookieOptions
                    {
                        Path = "/",
                        HttpOnly = true,
                        IsEssential = true,
                        Expires = DateTimeOffset.Now.AddDays(7), // magic numbers ftw
                    }
                );
            } catch ( Exception ) {}

            return Redirect(Request.Scheme + "://" + Request.Host.Value + "/");
        }
    }
}

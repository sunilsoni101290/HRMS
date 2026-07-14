using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    using APP.Controllers;
    using APP.Helpers;
    using APP.Models.Auth;
    using APP.Services.Interfaces;
    using Microsoft.AspNetCore.Http;

    public class JwtAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            var accessToken = session.GetString("AccessToken");

            // Nothing in Session at all - this happens whenever the
            // server-side session has expired or was never established in
            // this browser session (e.g. the browser was closed and
            // reopened, an app pool recycle wiped the in-memory session
            // store, etc.). Before bouncing to Login, try rebuilding the
            // ENTIRE session straight from the mirrored cookies set at
            // login (CookieHelper.SessionKeys) - no API call needed, so
            // this is instant.
            if (string.IsNullOrEmpty(accessToken))
            {
                RestoreSessionFromCookies(context);

                accessToken = session.GetString("AccessToken");

                if (string.IsNullOrEmpty(accessToken))
                {
                    context.Result = new RedirectToActionResult("Login", "Auth", null);
                    return;
                }
            }

            var refreshToken = session.GetString("RefreshToken");

            // Check expiry
            bool isExpired = JwtTokenHelper.IsTokenExpired(accessToken);

            // Access token valid
            if (!isExpired)
                return;

            // Access token expired (this also covers the case where it was
            // just restored from a cookie that's gone stale, e.g. after a
            // long time away) - the cookies alone can't fix an expired
            // access token, only a real refresh-token call can.
            if (string.IsNullOrEmpty(refreshToken))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            try
            {
                // Resolve API service
                var apiService = context.HttpContext.RequestServices.GetService<IApiService>();

                // Call refresh API
                var response = await apiService.PostAsync<AuthResponse>("auth/refresh-token",
                            new RefreshTokenRequestDto
                            {
                                RefreshToken = refreshToken
                            });

                // Refresh failed - the refresh token itself is genuinely no
                // longer valid (fully expired past its sliding window, or
                // revoked by an explicit Logout elsewhere).
                if (response == null)
                {
                    context.Result = new RedirectToActionResult("Login", "Auth", null);
                    return;
                }

                // Save new tokens
                session.SetString(
                    "AccessToken",
                    response.AccessToken);

                session.SetString(
                    "RefreshToken",
                    response.RefreshToken);

                // Keep the cookie mirror in sync too - AccessToken changes
                // on every refresh, so without this the next cookie-based
                // restore (RestoreSessionFromCookies above) would hand back
                // a stale/expired AccessToken every time. Re-mirrors ALL
                // fields (not just the tokens) so this also refreshes the
                // cookies' own 30-day expiry on every active use, the same
                // way the server-side refresh token itself slides forward.
                AuthController.MirrorSessionToCookies(context.HttpContext);
            }
            catch
            {
                context.Result =
                    new RedirectToActionResult(
                        "Login",
                        "Auth",
                        null);
            }
        }

        // Copies every mirrored auth cookie (see CookieHelper.SessionKeys)
        // straight into Session - no API round trip. If the AccessToken
        // cookie itself is missing (never logged in on this browser, or an
        // explicit Logout already cleared it), Session simply stays empty
        // and the caller falls through to the normal "redirect to Login"
        // path above.
        private static void RestoreSessionFromCookies(AuthorizationFilterContext context)
        {
            var request = context.HttpContext.Request;
            var session = context.HttpContext.Session;

            foreach (var key in CookieHelper.SessionKeys)
            {
                var value = CookieHelper.GetCookie(request, key);

                if (!string.IsNullOrEmpty(value))
                {
                    session.SetString(key, value);
                }
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    using APP.Helpers;
    using APP.Models.Auth;
    using APP.Services.Interfaces;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    public class JwtAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            var accessToken =  session.GetString("AccessToken");

            var refreshToken = session.GetString("RefreshToken");

            // No token in Session at all - this happens whenever the
            // server-side session has expired or was never established in
            // this browser session (e.g. the browser was closed and
            // re-opened). Before bouncing to Login, check for a "Remember
            // Me" refresh token cookie set at login time - if it's still
            // valid, silently re-authenticate instead of forcing the user
            // to log in again.
            if (string.IsNullOrEmpty(accessToken))
            {
                var rememberMeToken = context.HttpContext.Request.Cookies["RememberMeToken"];

                if (!string.IsNullOrEmpty(rememberMeToken) &&
                    await TryRestoreSessionFromRememberMeToken(context, rememberMeToken))
                {
                    return;
                }

                context.Result = new RedirectToActionResult("Login","Auth",null);
                return;
            }

            // Check expiry
            bool isExpired = JwtTokenHelper.IsTokenExpired(accessToken);

            // Access token valid
            if (!isExpired)
                return;

            // Access token expired
            if (string.IsNullOrEmpty(refreshToken))
            {
                context.Result = new RedirectToActionResult("Login","Auth",null);
                return;
            }

            try
            {
                // Resolve API service
                var apiService =context.HttpContext.RequestServices.GetService<IApiService>();

                // Call refresh API
                var response =await apiService.PostAsync<AuthResponse>("auth/refresh-token",
                            new RefreshTokenRequestDto
                            {
                                RefreshToken = refreshToken
                            });

                // Refresh failed
                if (response == null)
                {
                    context.Result =new RedirectToActionResult("Login","Auth",null);
                    return;
                }

                // Save new tokens
                session.SetString(
                    "AccessToken",
                    response.AccessToken);

                session.SetString(
                    "RefreshToken",
                    response.RefreshToken);
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

        // Re-establishes the full Session (all the profile fields the app
        // reads via SessionHelper, not just the tokens) from a "Remember
        // Me" refresh token cookie. Returns false - leaving the caller to
        // redirect to Login - if the token has been revoked/expired
        // server-side or the call otherwise fails.
        private static async Task<bool> TryRestoreSessionFromRememberMeToken(
            AuthorizationFilterContext context,
            string rememberMeToken)
        {
            try
            {
                var apiService = context.HttpContext.RequestServices.GetService<IApiService>();

                var response = await apiService.PostAsync<AuthResponse>(
                    "auth/refresh-token",
                    new RefreshTokenRequestDto { RefreshToken = rememberMeToken });

                if (response == null)
                    return false;

                var session = context.HttpContext.Session;

                session.SetString("AccessToken", response.AccessToken ?? "");
                session.SetString("RefreshToken", response.RefreshToken ?? "");
                session.SetString("FullName", response.FullName ?? "");
                session.SetString("UserId", response.UserId ?? "");
                session.SetString("EmployeeId", response.EmployeeId ?? "");
                session.SetString("TenantId", response.TenantId ?? "");
                session.SetString("Designation", response.Designation ?? "");
                session.SetString("CompanyName", response.CompanyName ?? "");
                session.SetString("CompanyId", response.CompanyId ?? "");
                session.SetString("BranchId", response.BranchId ?? "");
                session.SetString("RoleName", response.RoleName ?? "");

                // The API reuses the same refresh token rather than rotating
                // it, so this just slides the cookie's own expiry forward
                // on each silent re-auth rather than letting it count down
                // to the original login time.
                context.HttpContext.Response.Cookies.Append(
                    "RememberMeToken",
                    response.RefreshToken ?? "",
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(7),
                        HttpOnly = true,
                        Secure = context.HttpContext.Request.IsHttps,
                        SameSite = SameSiteMode.Lax
                    });

                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    //public class JwtAuthorizeAttribute : Attribute, IAuthorizationFilter
    //{
    //    public void OnAuthorization(
    //        AuthorizationFilterContext context)
    //    {
    //        var token = context.HttpContext
    //            .Session
    //            .GetString("AccessToken");

    //        if (string.IsNullOrEmpty(token))
    //        {
    //            context.Result =
    //                new RedirectToActionResult(
    //                    "Login",
    //                    "Auth",
    //                    null);
    //        }
    //    }
    //}

}

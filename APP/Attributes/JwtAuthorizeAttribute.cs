using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    using APP.Helpers;
    using APP.Models.Auth;
    using APP.Services.Interfaces;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;

    public class JwtAuthorizeAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            var accessToken =  session.GetString("AccessToken");

            var refreshToken = session.GetString("RefreshToken");

            // No token
            if (string.IsNullOrEmpty(accessToken))
            {
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

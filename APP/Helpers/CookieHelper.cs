using Microsoft.AspNetCore.Http;

namespace APP.Helpers
{
    // Mirrors the auth-related Session fields into persistent, HttpOnly
    // cookies so the whole session can be rebuilt WITHOUT an API round
    // trip whenever server-side Session is empty (browser closed and
    // reopened, app pool recycle, in-memory session store eviction, etc.).
    // This is what makes "stay logged in for a long time until explicit
    // Logout" actually hold even when the server-side Session itself gets
    // wiped for reasons that have nothing to do with the user's login
    // being genuinely invalid.
    //
    // AccessToken/RefreshToken still get refreshed via the API as usual
    // once JwtAuthorizeAttribute notices the restored AccessToken is
    // expired - these cookies are only the FAST local restore path, not a
    // replacement for the real refresh-token flow.
    public static class CookieHelper
    {
        // Every field mirrored between Session and cookies at login,
        // restore, and refresh time. Keep this list in sync with
        // AuthController.Login's HttpContext.Session.SetString calls.
        public static readonly string[] SessionKeys =
        {
            "AccessToken",
            "RefreshToken",
            "FullName",
            "UserId",
            "EmployeeId",
            "TenantId",
            "Designation",
            "CompanyName",
            "CompanyId",
            "BranchId",
            "RoleName"
        };

        public static void SetCookie(HttpResponse response, string key, string? value, int days = 30)
        {
            response.Cookies.Append(key, value ?? "", new CookieOptions
            {
                HttpOnly = true,

                // Secure must track the actual request scheme - hardcoding
                // true breaks every cookie during local HTTP development
                // (the browser silently refuses to store a Secure cookie
                // over plain HTTP), and hardcoding false would ship an
                // insecure cookie in production. HttpResponse.HttpContext
                // gives us the same request that's being responded to.
                Secure = response.HttpContext.Request.IsHttps,

                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(days),
                IsEssential = true
            });
        }

        public static string? GetCookie(HttpRequest request, string key)
        {
            return request.Cookies[key];
        }

        public static void DeleteCookie(HttpResponse response, string key)
        {
            response.Cookies.Delete(key);
        }
    }
}

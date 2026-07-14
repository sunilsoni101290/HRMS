namespace Application.DTOs.Auth
{
    /// <summary>
    /// Body shape for POST /api/auth/refresh-token and POST /api/auth/logout.
    /// Both callers (APP's JwtAuthorizeAttribute and ApiService) send the
    /// refresh token as JSON: { "RefreshToken": "..." }. The controller
    /// actions used to declare a bare `string refreshToken` parameter with
    /// no [FromBody], which ASP.NET Core binds from the query string by
    /// default for simple types - so the token sent in the body was never
    /// actually read, every refresh/logout call silently received null, and
    /// the "silently restore my session" fallback in JwtAuthorizeAttribute
    /// always failed, sending the user to Login on every request once their
    /// access token was no longer in Session (e.g. after it naturally
    /// expired, or the session was otherwise lost) - surfacing as
    /// "logged out on every page refresh".
    /// </summary>
    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; }
    }
}

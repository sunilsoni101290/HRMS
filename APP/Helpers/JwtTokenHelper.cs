using System.IdentityModel.Tokens.Jwt;

namespace APP.Helpers
{
    public static class JwtTokenHelper
    {
        // Proactive refresh buffer: treat the token as "expired" this long
        // BEFORE its real exp claim, so JwtAuthorizeAttribute refreshes it
        // ahead of time instead of racing the API's own validation cutoff.
        // Must stay comfortably smaller than the access token's own
        // lifetime (Jwt:ExpiryMinutes, currently 60) and roughly matches
        // the API's JWT ClockSkew tolerance - see API/Program.cs.
        private static readonly TimeSpan RefreshBuffer = TimeSpan.FromMinutes(2);

        public static bool IsTokenExpired(string token)
        {
            if (string.IsNullOrEmpty(token))
                return true;

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(token);

            return jwtToken.ValidTo < DateTime.UtcNow.Add(RefreshBuffer);
        }
    }
}

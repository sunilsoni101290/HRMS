using System.IdentityModel.Tokens.Jwt;

namespace APP.Helpers
{
    public static class JwtTokenHelper
    {
        public static bool IsTokenExpired(string token)
        {
            if (string.IsNullOrEmpty(token))
                return true;

            var handler = new JwtSecurityTokenHandler();

            var jwtToken = handler.ReadJwtToken(token);

            return jwtToken.ValidTo < DateTime.UtcNow;
        }
    }
}

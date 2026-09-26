using System;
using System.Security.Cryptography;
using System.Text;

namespace Domain.Helper
{
    // Storage-free payslip verification token - no new database column,
    // nothing persisted. The token is "<payrollId>.<signature>", where
    // <signature> is an HMAC-SHA256 of <payrollId> keyed by the app's
    // existing Jwt:Key secret (API/appsettings.json "Jwt:Key" - the same
    // secret already used to sign login JWTs, reused here rather than
    // introducing a second secret to manage). Anyone holding a valid token
    // (e.g. printed as a QR code on a payslip) can prove the payslip is
    // genuine without the server ever having stored a verification record;
    // a tampered PayrollId or a token for a payslip that has since been
    // deleted both fail validation.
    public static class PayslipVerificationHelper
    {
        public static string GenerateToken(string payrollId, string secret)
        {
            if (string.IsNullOrWhiteSpace(payrollId))
                throw new ArgumentException("PayrollId is required.", nameof(payrollId));

            var signature = Sign(payrollId, secret);
            return payrollId + "." + signature;
        }

        // Splits "<payrollId>.<signature>", recomputes the signature for
        // that payrollId, and compares in fixed time. Returns the
        // recovered payrollId only when the signature matches.
        public static bool TryValidate(string? token, string secret, out string payrollId)
        {
            payrollId = string.Empty;

            if (string.IsNullOrWhiteSpace(token))
                return false;

            var separatorIndex = token.LastIndexOf('.');
            if (separatorIndex <= 0 || separatorIndex == token.Length - 1)
                return false;

            var candidateId = token[..separatorIndex];
            var candidateSignature = token[(separatorIndex + 1)..];
            var expectedSignature = Sign(candidateId, secret);

            var candidateBytes = Encoding.UTF8.GetBytes(candidateSignature);
            var expectedBytes = Encoding.UTF8.GetBytes(expectedSignature);

            if (!CryptographicOperations.FixedTimeEquals(candidateBytes, expectedBytes))
                return false;

            payrollId = candidateId;
            return true;
        }

        private static string Sign(string payrollId, string secret)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret ?? string.Empty));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payrollId));

            // URL-safe Base64 (no '+', '/', or padding) so the token drops
            // straight into a query string / QR payload with no encoding.
            return Convert.ToBase64String(hash)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }
    }
}

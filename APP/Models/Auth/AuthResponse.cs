namespace APP.Models.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public int ExpiresIn { get; set; }

        public string UserId { get; set; }
        public string? EmployeeId { get; set; }
        public string TenantId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public string Designation { get; set; }
        public string CompanyName { get; set; }
        public string CompanyId { get; set; }
        public string BranchId { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; }
    }
}

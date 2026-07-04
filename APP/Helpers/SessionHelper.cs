namespace APP.Helpers
{
    public static class SessionHelper
    {
        public static string GetActiveTenantId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("TenantId");

        public static string GetActiveUserId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("UserId");
        public static string GetActiveCompanyId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("CompanyId");
        public static string GetActiveBranchId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("BranchId");

        public static string GetActiveRoleName => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("RoleName");

        // Admins / HR / Managers land on the admin Dashboard; everyone else on the
        // Employee self-service dashboard. Unknown/empty defaults to admin (safe).
        public static bool IsAdminRole(string? roleName = null)
        {
            var r = (roleName ?? GetActiveRoleName ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(r)) return true;
            return r.Contains("admin") || r.Contains("manager");
        }
    }
}

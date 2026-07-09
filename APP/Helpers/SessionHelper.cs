namespace APP.Helpers
{
    public static class SessionHelper
    {
        public static string GetActiveTenantId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("TenantId");

        public static string GetActiveUserId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("UserId");

        // Employee record linked to the logged-in account (empty for accounts
        // not tied to an employee, e.g. a pure admin login). Used to auto-resolve
        // "who am I" for self-service actions like attendance punching instead
        // of letting the user pick an employee from a dropdown.
        public static string GetActiveEmployeeId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("EmployeeId");

        public static string GetActiveFullName => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("FullName");
        public static string GetActiveCompanyId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("CompanyId");
        public static string GetActiveBranchId => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("BranchId");

        public static string GetActiveRoleName => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("RoleName");

        public static string GetActiveDesignation => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("Designation");

        public static string GetActiveCompanyName => new HttpContextAccessor()
            .HttpContext?.Session?.GetString("CompanyName");

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

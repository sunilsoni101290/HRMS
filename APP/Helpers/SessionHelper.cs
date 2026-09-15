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
        // Employee self-service dashboard.
        //
        // Unknown/empty role name defaults to NOT admin. This used to default to
        // true ("safe" per the old comment), but that was actually a fail-open
        // bug: a session with a missing/unreadable RoleName would silently be
        // treated as a full admin, which is the opposite of safe now that
        // self-service users are restricted to a small allowlist of pages
        // (see EssRestrictionAttribute) - an unrecognized role must fail closed
        // (most restricted), not open (most privileged).
        public static bool IsAdminRole(string? roleName = null)
        {
            var r = (roleName ?? GetActiveRoleName ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(r)) return false;
            // "configurator" covers the System Configurator role - an
            // application-management account that must never be funneled
            // into the ESS restriction filter alongside plain employees.
            return r.Contains("admin") || r.Contains("manager") || r.Contains("configurator");
        }

        // Narrower than IsAdminRole (which also passes any "manager" or
        // "configurator" role) - used to gate the User Management "Set
        // Password" action to Admin/Super Admin and HR Manager/HR
        // Executive only, per explicit requirement. Same loose
        // substring-match convention as IsAdminRole/
        // SystemConfiguratorOnlyAttribute - this is only the "don't even
        // show/allow the action" UI convenience gate, not a data-driven
        // Permission check (there is no granular Permission/RolePermission
        // entry for this specific action in this codebase yet).
        public static bool IsAdminOrHrRole(string? roleName = null)
        {
            var r = (roleName ?? GetActiveRoleName ?? "").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(r)) return false;
            return r.Contains("admin") || r.Contains("hr");
        }
    }
}

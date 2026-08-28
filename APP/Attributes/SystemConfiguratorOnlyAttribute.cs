using APP.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    // Gate for the Error Log screen (requirement #4: "accessible only to
    // users having the System Configurator role/permission"). Strictly
    // System Configurator only, per explicit instruction - Admin/Super
    // Admin is NOT included here even though DbSeeder's fullAccessRoles
    // loop would otherwise grant it every Permission automatically (see
    // the ERROR_LOG carve-out in AppFeatureSeeder.ReconcilePermissionsAsync,
    // which skips granting this specific feature's permissions to Admin).
    // The real, data-driven check is still
    // IErrorLogService.EnsurePermissionAsync on the API side; this attribute
    // is only the "don't even show the page" convenience gate.
    public class SystemConfiguratorOnlyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private static readonly string[] AllowedRoles = { "system configurator" };

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            var accessToken = session.GetString("AccessToken");

            // Not authenticated at all - let JwtAuthorizeAttribute (which
            // always runs first, Order 0) handle the redirect to Login;
            // nothing to check here yet.
            if (string.IsNullOrEmpty(accessToken))
                return Task.CompletedTask;

            var roleName = (SessionHelper.GetActiveRoleName ?? "").Trim().ToLowerInvariant();

            if (AllowedRoles.Contains(roleName))
                return Task.CompletedTask;

            // Logged in, but not System Configurator/Admin - send them back
            // to their own dashboard rather than a bare 403, same UX as
            // EssRestrictionAttribute's redirect for a blocked admin page.
            var dashboardController = SessionHelper.IsAdminRole() ? "Dashboard" : "EmployeeDashboard";
            context.Result = new RedirectToActionResult("Index", dashboardController, null);

            return Task.CompletedTask;
        }
    }
}

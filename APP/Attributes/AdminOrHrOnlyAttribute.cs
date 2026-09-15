using APP.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    // Gate for actions that must be restricted to Admin/Super Admin or HR
    // Manager/HR Executive only - currently just
    // UserController.SetPassword ("Set/View Password" on the User
    // Management Index page). Same shape/convention as
    // SystemConfiguratorOnlyAttribute - this is only the "don't even allow
    // the action" convenience gate; there is no granular Permission/
    // RolePermission entry for this specific action in this codebase yet,
    // so there's no deeper data-driven check to layer underneath (unlike
    // SystemConfiguratorOnlyAttribute's EnsurePermissionAsync backstop for
    // ERROR_LOG). See SessionHelper.IsAdminOrHrRole for the role-name
    // matching rule itself.
    public class AdminOrHrOnlyAttribute : Attribute, IAsyncAuthorizationFilter
    {
        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            var accessToken = session.GetString("AccessToken");

            // Not authenticated at all - let JwtAuthorizeAttribute (which
            // always runs first, Order 0) handle the redirect to Login;
            // nothing to check here yet.
            if (string.IsNullOrEmpty(accessToken))
                return Task.CompletedTask;

            if (SessionHelper.IsAdminOrHrRole())
                return Task.CompletedTask;

            // Logged in, but not Admin/HR - send them back to their own
            // dashboard rather than a bare 403, same UX as
            // SystemConfiguratorOnlyAttribute/EssRestrictionAttribute.
            var dashboardController = SessionHelper.IsAdminRole() ? "Dashboard" : "EmployeeDashboard";
            context.Result = new RedirectToActionResult("Index", dashboardController, null);

            return Task.CompletedTask;
        }
    }
}

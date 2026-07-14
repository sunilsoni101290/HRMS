using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    // Global safety net: ApiService throws UnauthorizedAccessException only
    // when a call to the API comes back 401 AND a silent token refresh
    // (see ApiService.SendWithAutoRefreshAsync) has already been tried and
    // failed - meaning the refresh token itself is genuinely no longer
    // valid (fully expired past its window, or revoked by an explicit
    // Logout elsewhere). At that point there's nothing left to silently
    // recover, so this catches it wherever it surfaces (any controller
    // action, without needing a try/catch in every one of them) and sends
    // the user to Login with a clear message instead of the generic
    // /Home/Error page a raw unhandled exception would otherwise hit.
    public class ApiSessionExpiredFilter : IActionFilter, IOrderedFilter
    {
        // Runs as a normal action filter (default Order 0) - this only
        // needs to catch exceptions thrown by the action itself, so it
        // doesn't need explicit ordering relative to JwtAuthorizeAttribute
        // or EssRestrictionAttribute (both authorization filters, a
        // separate pipeline stage that always runs first regardless).
        public int Order => 0;

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception is UnauthorizedAccessException)
            {
                context.ExceptionHandled = true;

                context.HttpContext.Session.Clear();

                context.Result = new RedirectToActionResult(
                    "Login",
                    "Auth",
                    new { });

                // TempData isn't available directly on ActionExecutedContext,
                // so stash the message via the session-backed TempData
                // provider through the controller if present - falling back
                // silently is fine, the redirect to Login is what matters.
                if (context.Controller is Controller controller)
                {
                    controller.TempData["GlobalError"] =
                        "Your session has expired. Please login again.";
                }
            }
        }
    }
}

using APP.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APP.Attributes
{
    // Global gate: a self-service (non-admin) user must only ever be able to
    // reach the employee self-service (ESS) area of the site, never an
    // admin/HR page, regardless of how they got the URL (typed it, bookmarked
    // it, clicked an old link, etc.) - hiding buttons/menu items alone is not
    // enough, since the underlying controller actions are still directly
    // reachable by URL.
    //
    // This is registered once, globally, in Program.cs
    // (options.Filters.Add<EssRestrictionAttribute>()) rather than decorating
    // every controller, so nothing can be reached by simply forgetting to add
    // a guard to a new controller - it is deny-by-default for two categories:
    //
    //  1. Controllers that are entirely admin/HR (master data, org setup,
    //     recruitment, payroll administration, user/role management, etc.) -
    //     blocked wholesale.
    //  2. Specific admin-only actions inside "mixed" controllers that also
    //     serve a legitimate self-service purpose (e.g. LeaveApplication has
    //     both the org-wide Index/Pending/Approved lists AND the self-service
    //     Create/EmployeeLeaves/MyApprovals actions) - only the admin-only
    //     actions are blocked, the rest of the controller stays reachable.
    //
    // Admin/HR users are never restricted by this filter.
    public class EssRestrictionAttribute : IAsyncAuthorizationFilter, IOrderedFilter
    {
        // This filter is registered globally, while JwtAuthorizeAttribute is
        // applied per-controller. ASP.NET Core would otherwise run the
        // global filter BEFORE the controller-scoped one - which matters
        // because JwtAuthorizeAttribute can silently re-populate an expired
        // Session (role included) from a "Remember Me" cookie. Without this
        // explicit Order, this filter could make its allow/deny decision
        // using a stale, not-yet-restored session on that first request.
        // A higher Order value here means "run later" - after any
        // default-Order (0) filter such as JwtAuthorizeAttribute.
        public int Order => 1000;


        // Entirely admin/HR - every action in these controllers is blocked
        // for a self-service user.
        private static readonly HashSet<string> AdminOnlyControllers = new(StringComparer.OrdinalIgnoreCase)
        {
            "AppFeatures",
            "AssetAllocation",
            "AssetCategory",
            "Asset",
            "AttendanceLog",
            "BiometricDevice",
            "Branch",
            "CandidateApplication",
            "Candidate",
            "City",
            "Company",
            "Country",
            "Dashboard",
            "Department",
            "Designation",
            "EmployeeBiometricMapping",
            "EmployeeShiftMapping",
            "FinancialYear",
            "HolidayGroup",
            "InterviewSchedule",
            "JobOpening",
            "LeaveApprovalHistory",
            "LeaveBalance",
            "LeaveType",
            "Location",
            "LoginHistory",
            "RecruitmentDashboard",
            "Role",
            "Permission",
            "SalaryComponent",
            "SalaryStructure",
            "Shift",
            "State",
            "Tenant",
            "User",
            "WeekOff",
        };

        // Mixed controllers - reachable by self-service by default, EXCEPT
        // the specific admin-only actions listed here.
        private static readonly Dictionary<string, HashSet<string>> AdminOnlyActionsByController =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Employee"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Index", "Create", "Delete", "Import", "DownloadImportTemplate",
                    "GetBranchByCompanyId", "GetDesignationByDepartmentId",
                    "ViewPassport", "DownloadPassport"
                },
                ["Attendance"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Index", "GetEmployeeAttendanceStatus"
                },
                ["LeaveApplication"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    // Approve/Reject/SendBack/MyApprovals are deliberately NOT
                    // here - a Reporting Manager or Department Head may be
                    // logged in under the plain self-service role, since org
                    // hierarchy is independent of login role in this system.
                    "Index", "Pending", "Approved", "Rejected", "Cancelled", "Edit"
                },
                ["EmployeeDocument"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Index", "Create", "Edit", "Delete", "Verify", "UnVerify",
                    "Expired", "Expiring"
                },
                ["EmployeeTask"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Index", "Create", "Edit", "Delete"
                },
                ["Payroll"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Index", "Generate", "Details", "Process", "MarkPaid",
                    "Delete", "Dashboard", "Report"
                },
                ["Announcement"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Create", "Edit", "Delete"
                },
                ["Event"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    "Create", "Edit", "Delete"
                },
                ["Notification"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    // Manage/Create/Delete build/administer org-wide broadcast
                    // notifications - these had no server-side admin guard at
                    // all before this filter existed.
                    "Manage", "Create", "Delete"
                },
                ["SupportTicket"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    // Index lists every ticket org-wide; UpdateStatus lets
                    // support close/resolve a ticket. MyTickets/Create/
                    // Details/Reply stay open - ticket raising and viewing
                    // one's own tickets is open to everyone by design.
                    "Index", "UpdateStatus"
                },
                ["Faq"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    // Managing the knowledge base is Admin/HR only. Browse
                    // (published FAQs) stays open to everyone.
                    "Index", "Create", "Edit", "Delete", "ToggleActive"
                },
                ["Auth"] = new(StringComparer.OrdinalIgnoreCase)
                {
                    // User list / user creation / arbitrary user lookup are
                    // admin-only. Login/ForgotPassword are pre-auth and never
                    // reach this filter anyway (see below); Logout and
                    // ChangePassword stay open to everyone.
                    "Index", "Register", "Details"
                },
            };

        public Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;

            // Not authenticated yet - nothing to restrict here, the
            // per-controller [JwtAuthorize] filter (where present) is
            // responsible for bouncing anonymous requests to Login.
            var accessToken = session.GetString("AccessToken");

            if (string.IsNullOrEmpty(accessToken))
                return Task.CompletedTask;

            // Admin/HR/Manager roles are unrestricted.
            if (SessionHelper.IsAdminRole())
                return Task.CompletedTask;

            var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
            var action = context.RouteData.Values["action"]?.ToString() ?? "";

            bool blocked =
                AdminOnlyControllers.Contains(controller) ||
                (AdminOnlyActionsByController.TryGetValue(controller, out var deniedActions) &&
                 deniedActions.Contains(action));

            if (!blocked)
                return Task.CompletedTask;

            // Send them back to their own dashboard rather than a bare
            // Forbid - a self-service user hitting a stale/bookmarked admin
            // link should land somewhere useful, not a dead end.
            context.Result = new RedirectToActionResult("Index", "EmployeeDashboard", null);

            return Task.CompletedTask;
        }
    }
}

using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // Own controller - mirrors the "Dashboard" (admin) vs "EmployeeDashboard"
    // (self-service) split already used in this app; this is the
    // Admin/HR-only KPI dashboard for Attendance specifically. Backed by
    // API/Controllers/AttendanceInsightsController.cs (api/attendanceinsights/dashboard).
    // Admin/HR only - see EssRestrictionAttribute.
    [JwtAuthorize]
    public class AttendanceDashboardController : Controller
    {
        private readonly IApiService _apiService;

        public AttendanceDashboardController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? companyId)
        {
            companyId ??= SessionHelper.GetActiveCompanyId;

            ViewBag.SelectedCompanyId = companyId;

            var url = "attendanceinsights/dashboard";
            if (!string.IsNullOrWhiteSpace(companyId))
                url += $"?companyId={Uri.EscapeDataString(companyId)}";

            var data = await _apiService.GetAsync<AttendanceDashboardDto>(url);

            return View(data ?? new AttendanceDashboardDto());
        }
    }
}

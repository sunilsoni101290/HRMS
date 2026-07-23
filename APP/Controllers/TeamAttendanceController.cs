using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // Kept on its own controller rather than folded into AttendanceController -
    // same "AttendanceRegularization is its own controller" precedent this app
    // already follows for a major Attendance sub-feature. Backed by
    // API/Controllers/AttendanceInsightsController.cs (api/attendanceinsights/team),
    // which scopes the result server-side to the caller's own direct reports
    // (or org-wide for a real HR/Admin caller) - this controller never passes
    // an employeeId/manager id itself.
    //
    // Deliberately NOT added to EssRestrictionAttribute.AdminOnlyControllers -
    // a Reporting Manager may be logged in under the plain self-service role
    // (org hierarchy is independent of login role in this system), same as
    // LeaveApplication.MyApprovals.
    [JwtAuthorize]
    public class TeamAttendanceController : Controller
    {
        private readonly IApiService _apiService;

        public TeamAttendanceController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? date)
        {
            var selectedDate = date ?? DateTime.Today;

            ViewBag.SelectedDate = selectedDate;

            var url = $"attendanceinsights/team?date={selectedDate:yyyy-MM-dd}";

            var data = await _apiService.GetAsync<List<TeamAttendanceMemberDto>>(url);

            return View(data ?? new List<TeamAttendanceMemberDto>());
        }
    }
}

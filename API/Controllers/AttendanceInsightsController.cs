using Application.Interfaces.Attendances;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    // Attendance aggregation/read endpoints (Calendar/Team/Summary/Dashboard)
    // kept on a dedicated controller rather than folded into the existing
    // AttendanceController (which is the plain Attendance master CRUD +
    // punch endpoints), so this diff stays isolated and reviewable.
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AttendanceInsightsController : ControllerBase
    {
        private readonly IAttendanceService _attendanceService;

        public AttendanceInsightsController(IAttendanceService attendanceService)
        {
            _attendanceService = attendanceService;
        }

        // TenantId/UserId are read from the JWT claims exactly like
        // EmployeeBankController / OnboardingController.
        private string? TenantId => User.FindFirst("TenantId")?.Value;
        private string? ActingUserId => User.FindFirst("UserId")?.Value;

        // GET api/attendanceinsights/calendar?employeeId=&month=&year=
        [HttpGet("calendar")]
        public async Task<IActionResult> GetCalendar([FromQuery] string employeeId, [FromQuery] int month, [FromQuery] int year)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
                return BadRequest(new { Message = "employeeId is required." });

            if (month < 1 || month > 12)
                return BadRequest(new { Message = "month must be between 1 and 12." });

            var result = await _attendanceService.GetCalendarAsync(employeeId, month, year, TenantId);
            return Ok(result);
        }

        // GET api/attendanceinsights/team?date=
        [HttpGet("team")]
        public async Task<IActionResult> GetTeam([FromQuery] DateTime date)
        {
            bool isHrOrAdmin = await _attendanceService.IsHrOrAdminForAttendanceAsync(ActingUserId);

            var result = await _attendanceService.GetTeamAttendanceAsync(ActingUserId, date, TenantId, isHrOrAdmin);
            return Ok(result);
        }

        // GET api/attendanceinsights/summary?month=&year=&departmentId=&employeeId=
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary(
            [FromQuery] int month,
            [FromQuery] int year,
            [FromQuery] string? departmentId,
            [FromQuery] string? employeeId)
        {
            if (month < 1 || month > 12)
                return BadRequest(new { Message = "month must be between 1 and 12." });

            var result = await _attendanceService.GetSummaryAsync(TenantId, month, year, departmentId, employeeId);
            return Ok(result);
        }

        // GET api/attendanceinsights/dashboard?companyId=
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard([FromQuery] string? companyId)
        {
            var result = await _attendanceService.GetDashboardAsync(TenantId, companyId);
            return Ok(result);
        }
    }
}

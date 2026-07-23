using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class AttendanceController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tenantId;
        private string _userId;

        // The employee record linked to whoever is logged in. Punch actions
        // always use this - never a value picked from a dropdown or posted
        // from the client - so an employee can only ever punch themselves in/out.
        private readonly string? _employeeId;
        private readonly bool _isAdmin;

        public AttendanceController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _employeeId = SessionHelper.GetActiveEmployeeId;
            _isAdmin = SessionHelper.IsAdminRole();
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.IsAdmin = _isAdmin;
            var data = await _apiService
                .GetAsync<List<AttendanceDto>>($"attendance/get-all-attendance-list");
            return View(data);
        }

        /// <summary>
        /// Self-service attendance history - filters the same punch list the
        /// admin Index screen uses down to only the logged-in user's own
        /// employee record server-side, so an employee can never see anyone
        /// else's attendance from this page. Enhanced with a monthly
        /// summary strip (Present/Absent/Late/HalfDay/Leave counts + total
        /// working hours, from AttendanceInsights/summary scoped to this one
        /// employee) and a link into the new self-service Attendance
        /// Calendar - own dedicated view (MyAttendance.cshtml) so the
        /// original admin Index.cshtml/route stays completely untouched.
        /// </summary>
        public async Task<IActionResult> MyAttendance(int? month, int? year)
        {
            ViewBag.IsAdmin = false;

            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.NoEmployeeProfile = true;
                return View(new List<AttendanceDto>());
            }

            var data = await _apiService
                .GetAsync<List<AttendanceDto>>($"attendance/get-all-attendance-list");

            var mine = (data ?? new List<AttendanceDto>())
                .Where(a => a.EmployeeId == _employeeId)
                .OrderByDescending(a => a.Date)
                .ToList();

            var today = DateTime.Today;
            int y = year ?? today.Year;
            int m = month ?? today.Month;

            ViewBag.SummaryMonth = m;
            ViewBag.SummaryYear = y;
            ViewBag.SummaryMonthName = new DateTime(y, m, 1).ToString("MMMM yyyy");

            try
            {
                var summaryUrl =
                    $"attendanceinsights/summary?month={m}&year={y}" +
                    $"&employeeId={Uri.EscapeDataString(_employeeId)}";

                var summaryRows = await _apiService.GetAsync<List<AttendanceSummaryRowDto>>(summaryUrl);
                ViewBag.MySummary = summaryRows?.FirstOrDefault();
            }
            catch
            {
                // Insights endpoint failing shouldn't take down the whole
                // page - the punch list below still renders without the
                // summary strip.
                ViewBag.MySummary = null;
            }

            ViewBag.ListTitle = "My Attendance";
            return View(mine);
        }

        [HttpGet]
        public async Task<JsonResult> GetEmployeeAttendanceStatus(string employeeId)
        {
            var data = await _apiService
                .GetAsync<AttendanceCurrentStatusDto>(
                    $"attendance/current-status/{employeeId}");

            return Json(data);
        }

        /// <summary>
        /// Same as GetEmployeeAttendanceStatus, but always resolves the
        /// employee from the logged-in session - the punch page calls this
        /// instead, so nobody can query (or punch) on another employee's
        /// behalf by tampering with a posted/querystring EmployeeId.
        /// </summary>
        [HttpGet]
        public async Task<JsonResult> GetMyAttendanceStatus()
        {
            if (string.IsNullOrEmpty(_employeeId))
            {
                return Json(new { NoEmployeeProfile = true });
            }

            var data = await _apiService
                .GetAsync<AttendanceCurrentStatusDto>(
                    $"attendance/current-status/{_employeeId}");

            return Json(data);
        }

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<AttendanceDto>(
                $"attendance/get-attendance-detail/{id}"
            );

            if (data == null)
                return NotFound();

            // A self-service user can only ever open their own attendance
            // records from "My Attendance" - never a colleague's, even by
            // guessing/tampering with the id in the URL.
            if (!_isAdmin && data.EmployeeId != _employeeId)
                return Forbid();

            return View(data);
        }

        #region Calendar

        /// <summary>
        /// Month-grid view of one employee's attendance, calling
        /// AttendanceInsights/calendar - same placement precedent as
        /// LeaveApplication.Calendar (an action added directly on the
        /// existing feature controller rather than a new one, since it's
        /// just another read view over the same "attendance" concept).
        /// Self-service: always defaults to (and, for a non-admin, is
        /// locked to) the logged-in user's own employee record. Admin/HR
        /// may pass employeeId to view anyone else's calendar via the
        /// dropdown rendered in the view.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Calendar(int? year, int? month, string? employeeId)
        {
            var today = DateTime.Today;

            int y = year ?? today.Year;
            int m = month ?? today.Month;

            while (m < 1) { m += 12; y -= 1; }
            while (m > 12) { m -= 12; y += 1; }

            // Never trust a client-supplied employeeId for a self-service
            // user - always view their own calendar, exactly like
            // Details/EmployeeLeaves elsewhere in this app. Only admin/HR
            // may look up another employee's calendar this way.
            string? targetEmployeeId = _isAdmin
                ? (string.IsNullOrWhiteSpace(employeeId) ? _employeeId : employeeId)
                : _employeeId;

            ViewBag.IsAdmin = _isAdmin;
            ViewBag.SelectedEmployeeId = targetEmployeeId;

            if (_isAdmin)
            {
                var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee");
                ViewBag.EmployeeList = new SelectList(employees, "Value", "Text", targetEmployeeId);
            }

            if (string.IsNullOrEmpty(targetEmployeeId))
            {
                ViewBag.NoEmployeeProfile = true;
                return View(new List<AttendanceCalendarDayDto>());
            }

            ViewBag.NoEmployeeProfile = false;
            ViewBag.Year = y;
            ViewBag.Month = m;
            ViewBag.MonthName = new DateTime(y, m, 1).ToString("MMMM yyyy");

            var url =
                $"attendanceinsights/calendar?employeeId={Uri.EscapeDataString(targetEmployeeId)}" +
                $"&month={m}&year={y}";

            var data = await _apiService.GetAsync<List<AttendanceCalendarDayDto>>(url);

            return View(data ?? new List<AttendanceCalendarDayDto>());
        }

        #endregion

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            // No employee dropdown - the page always punches the
            // logged-in user's own linked employee record.
            ViewBag.NoEmployeeProfile = string.IsNullOrEmpty(_employeeId);
            ViewBag.EmployeeDisplayName = SessionHelper.GetActiveFullName;

            return View(new PunchRequestDto { EmployeeId = _employeeId });
        }

        [HttpPost]
        public async Task<IActionResult> SavePunch(PunchRequestDto dto,string PunchType)
        {
            // Always punch as the logged-in user's own employee record -
            // ignore whatever EmployeeId (if any) came from the client.
            if (string.IsNullOrEmpty(_employeeId))
            {
                TempData["GlobalError"] = "Your login isn't linked to an employee profile, so you can't punch attendance.";
                return RedirectToAction(nameof(Create));
            }

            dto ??= new PunchRequestDto();
            dto.EmployeeId = _employeeId;

            if (!string.IsNullOrEmpty(PunchType))
            {
                string userAgent = Request.Headers["User-Agent"].ToString();

                var device = GetDeviceInfo(userAgent);

                dto.DeviceType= device.DeviceType;
                dto.OS = device.OS;
                dto.Browser = device.Browser;
                dto.Version = device.Version;

                if (PunchType == "In")
                {
                    dto.CreatedBy = _userId;
                    await _apiService.PostAsync<dynamic>($"attendance/punch-in", dto);
                    TempData["SuccessMessage"] = "Punch In recorded successfully.";

                    return View("Create", dto);
                }                
                if (PunchType == "BreakOut")
                {
                    dto.CreatedBy = _userId;
                    dto.ModifiedBy = _userId;
                    dto.ModifiedOn = DateTime.Now;

                    await _apiService.PostAsync<dynamic>($"attendance/break-out", dto);
                    TempData["SuccessMessage"] = "Break Out recorded successfully.";
                    return View("Create", dto);
                }
                if (PunchType == "BreakIn")
                {
                    dto.CreatedBy = _userId;
                    dto.ModifiedBy = _userId;
                    dto.ModifiedOn = DateTime.Now;

                    await _apiService.PostAsync<dynamic>($"attendance/break-in", dto);
                    TempData["SuccessMessage"] = "Break In recorded successfully.";
                    return View("Create", dto);
                }
                if (PunchType == "Out")
                {
                    dto.CreatedBy = _userId;
                    dto.ModifiedBy = _userId;
                    dto.ModifiedOn = DateTime.Now;

                    await _apiService.PostAsync<dynamic>($"attendance/punch-out", dto);
                    TempData["SuccessMessage"] = "Punch Out recorded successfully.";
                    return View("Create", dto);
                }

            }
            return RedirectToAction("Index");
        }

        #region Get Device Id
        public static DeviceInfo GetDeviceInfo(string userAgent)
        {
            var info = new DeviceInfo();

            // Browser
            if (userAgent.Contains("Edg"))
                info.Browser = "Edge";
            else if (userAgent.Contains("Chrome"))
                info.Browser = "Chrome";
            else if (userAgent.Contains("Firefox"))
                info.Browser = "Firefox";
            else if (userAgent.Contains("Safari"))
                info.Browser = "Safari";
            else
                info.Browser = "Unknown";

            // OS
            if (userAgent.Contains("Windows"))
                info.OS = "Windows";
            else if (userAgent.Contains("Android"))
                info.OS = "Android";
            else if (userAgent.Contains("iPhone"))
                info.OS = "iPhone";
            else if (userAgent.Contains("Mac"))
                info.OS = "Mac";
            else
                info.OS = "Unknown";

            // Device Type
            if (userAgent.Contains("Mobile"))
                info.DeviceType = "Mobile";
            else
                info.DeviceType = "Desktop";

            // Browser Version
            var match = System.Text.RegularExpressions.Regex.Match(userAgent, @"(Edg|Chrome|Firefox)/(\d+)");

            if (match.Success)
                info.Version = match.Groups[2].Value;

            return info;
        }

        #endregion
    }
}

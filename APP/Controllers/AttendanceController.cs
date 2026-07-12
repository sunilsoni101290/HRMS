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
        /// Self-service attendance history - reuses the same Index view/list
        /// endpoint as the admin screen, but filters down to only the
        /// logged-in user's own employee record server-side, so an employee
        /// can never see anyone else's attendance from this page.
        /// </summary>
        public async Task<IActionResult> MyAttendance()
        {
            ViewBag.IsAdmin = false;

            if (string.IsNullOrEmpty(_employeeId))
            {
                ViewBag.NoEmployeeProfile = true;
                return View("Index", new List<AttendanceDto>());
            }

            var data = await _apiService
                .GetAsync<List<AttendanceDto>>($"attendance/get-all-attendance-list");

            var mine = (data ?? new List<AttendanceDto>())
                .Where(a => a.EmployeeId == _employeeId)
                .OrderByDescending(a => a.Date)
                .ToList();

            ViewBag.ListTitle = "My Attendance";
            return View("Index", mine);
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

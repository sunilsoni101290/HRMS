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
        public AttendanceController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
           
            var data = await _apiService
                .GetAsync<List<AttendanceDto>>($"attendance/get-all-attendance-list");
            return View(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetEmployeeAttendanceStatus(string employeeId)
        {
            var data = await _apiService
                .GetAsync<AttendanceCurrentStatusDto>(
                    $"attendance/current-status/{employeeId}");

            return Json(data);
        }

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<AttendanceDto>(
                $"attendance/get-attendance-detail/{id}"
            );

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new PunchRequestDto());
        }

        [HttpPost]
        public async Task<IActionResult> SavePunch(PunchRequestDto dto,string PunchType)
        {
            if (dto!=null && !string.IsNullOrEmpty(PunchType))
            {
                string userAgent = Request.Headers["User-Agent"].ToString();

                var device = GetDeviceInfo(userAgent);

                dto.DeviceType= device.DeviceType;
                dto.OS = device.OS;
                dto.Browser = device.Browser;
                dto.Version = device.Version;

                await LoadDropdowns();

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

        #region Load Dropdowns
        private async Task LoadDropdowns()
        {
            // Employee
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/employee");

            ViewBag.EmployeeList = new SelectList(
                employees,
                "Value",
                "Text");

            ViewBag.EmployeeNames = employees.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion

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

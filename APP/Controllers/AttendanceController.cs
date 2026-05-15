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
                .GetAsync<List<AttendanceLogDto>>($"attendance/get-all-attendance-logs");

            await LoadDropdowns();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new PunchRequestDto() { DeviceId = GetDeviceId() });
        }

        [HttpPost]
        public async Task<IActionResult> SavePunch(PunchRequestDto dto,string PunchType)
        {
            if (ModelState.IsValid)
            {
                dto.DeviceId = GetDeviceId();
                await LoadDropdowns();

                if (PunchType == "In")
                {
                    await _apiService.PostAsync<dynamic>($"attendance/punch-in", dto);
                    TempData["SuccessMessage"] = "Punch In recorded successfully.";
                    return View(dto);
                }
                else if (PunchType == "Out")
                {
                    await _apiService.PostAsync<dynamic>($"attendance/punch-out", dto);
                    TempData["SuccessMessage"] = "Punch Out recorded successfully.";
                    return View(dto);
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

        private string GetDeviceId()
        {
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "UnknownIP";

            string userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

            string browser = HttpContext.Request.Headers["sec-ch-ua"].ToString();

            string deviceId = $"{ipAddress}-{userAgent}-{browser}";

            return deviceId;
        }

        #endregion
    }
}

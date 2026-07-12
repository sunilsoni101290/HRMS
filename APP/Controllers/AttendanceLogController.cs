using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class AttendanceLogController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tenantId;
        private string _userId;
        public AttendanceLogController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
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

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<AttendanceLogDto>(
                $"attendance/attendanceLog-detail/{id}"
            );

            await LoadDropdowns();

            return View(data);
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
    }
}

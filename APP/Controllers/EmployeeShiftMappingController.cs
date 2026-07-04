using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class EmployeeShiftMappingController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        public EmployeeShiftMappingController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
               .GetAsync<List<EmployeeShiftMappingDto>>("EmployeeShiftMapping");

            await LoadDropdowns();

            return View(data);
        }


        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new EmployeeShiftMappingDto() { EffectiveFrom = DateTime.Now, EffectiveTo=DateTime.Now.AddYears(1)});
        }

        [HttpPost]
        public async Task<IActionResult> Create(EmployeeShiftMappingDto dto)
        {
            if (dto!=null)
            {
                dto.CreatedBy= _userId;
                await LoadDropdowns();
                await _apiService.PostAsync<dynamic>("EmployeeShiftMapping", dto);

                TempData["Success"] = "Record created successfully.";

                return RedirectToAction(nameof(Index));
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<EmployeeShiftMappingDto>($"EmployeeShiftMapping/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, EmployeeShiftMappingDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.UpdatedOn = DateTime.UtcNow;
                dto.ModifiedBy = _userId;
                dto.CreatedBy = _userId;

                await LoadDropdowns();
                await _apiService
                    .PutAsync<dynamic>($"EmployeeShiftMapping/{id}", dto);

                TempData["Success"] = "Record updated successfully.";

                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        #region Details

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<EmployeeShiftMappingDto>($"EmployeeShiftMapping/{id}");
            if (data == null)
                return NotFound();

            return View(data);
        }

        #endregion

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

            // Employee
            var shifts = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/shift");

            ViewBag.ShiftList = new SelectList(
                shifts,
                "Value",
                "Text");

            ViewBag.ShiftNames = employees.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion
    }
}

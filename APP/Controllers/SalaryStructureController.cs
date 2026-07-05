using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Salary Structure Controller

    [JwtAuthorize]
    public class SalaryStructureController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public SalaryStructureController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<SalaryStructureListDto>>("salary-structure");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new SalaryStructureDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(SalaryStructureDto dto)
        {
            if (dto != null && dto.Details != null && dto.Details.Any(d => !string.IsNullOrEmpty(d.SalaryComponentId)))
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("salary-structure", dto);

                TempData["Success"] = "Salary structure saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Please add at least one salary component.";
            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryStructureDto>($"salary-structure/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<SalaryStructureDto>($"salary-structure/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, SalaryStructureDto dto)
        {
            if (dto != null && dto.Details != null && dto.Details.Any(d => !string.IsNullOrEmpty(d.SalaryComponentId)))
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"salary-structure/{id}", dto);

                TempData["Success"] = "Salary structure updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Please add at least one salary component.";
            await LoadDropdowns();
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"salary-structure/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");
            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            var components = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/salary-component");
            ViewBag.ComponentList = new SelectList(components, "Value", "Text");
        }

        #endregion
    }

    #endregion
}

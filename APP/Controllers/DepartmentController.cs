using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Department Controller
    [JwtAuthorize]
    public class DepartmentController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        public DepartmentController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<DepartmentListDto>>("department");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
             await LoadDropdowns();
            return View(new DepartmentDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(DepartmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy= _userId;
                await LoadDropdowns();
                await _apiService.PostAsync<dynamic>("department", dto);
                AlertHelper.Success(TempData, "Record saved successfully.");
                return View(dto);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<DepartmentDto>($"department/{id}");
            await LoadDropdowns();
            return View("Create",data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, DepartmentDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await LoadDropdowns();
                await _apiService
                    .PutAsync<dynamic>($"department/{id}", dto);
                AlertHelper.Success(TempData, "Record updated successfully.");
                return View("Create",dto);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"department/{id}");

            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            // Company
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/department");

            ViewBag.DepartmentList = new SelectList(
                departments,
                "Value",
                "Text");

            // Branch
            var branchs = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/branch");

            ViewBag.BranchList = new SelectList(
                branchs,
                "Value",
                "Text");
        }

        #endregion
    }

    #endregion
}

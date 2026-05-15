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

            await LoadDropdowns();

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
                TempData["Success"] = "Record created successfully.";
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
                TempData["Success"] = "Record updated successfully.";
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

        private async Task LoadDropdowns(string?deptId=null)
        {
            // Company
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");

            ViewBag.CompanyNames = companies.ToDictionary(x => x.Value, x => x.Text);

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/parent-department?tenantId={_tenantId}&departmentId={deptId}");

            ViewBag.ParentDepartmentList = new SelectList(
                departments,
                "Value",
                "Text");

            ViewBag.ParentDepartments = departments.ToDictionary(x => x.Value, x => x.Text);

            // Branch
            var branches = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/branch");

            ViewBag.BranchList = new SelectList(
                branches,
                "Value",
                "Text");

            ViewBag.BranchNames = branches.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion
    }

    #endregion
}

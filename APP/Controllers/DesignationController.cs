using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APP.Controllers
{
    #region Designation Controller

    [JwtAuthorize]
    public class DesignationController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public DesignationController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<DesignationListDto>>("designation");

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/department");

            ViewBag.DepartmentNames = departments.ToDictionary(x => x.Value, x => x.Text);

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new DesignationDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(DesignationDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await LoadDropdowns();
                await _apiService.PostAsync<dynamic>("designation", dto);

                TempData["Success"] = "Record saved successfully.";
                return View(dto);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<DesignationDto>($"designation/{id}");
            await LoadDropdowns(data.ParentDesignationId, data.CompanyId);
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<DesignationDto>($"designation/{id}");
            await LoadDropdowns(data.ParentDesignationId,data.CompanyId);
            return View("Create",data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, DesignationDto dto)
        {
            if (!ModelState.IsValid)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await LoadDropdowns(dto.ParentDesignationId, dto.CompanyId);
                await _apiService
                .PutAsync<dynamic>($"designation/{id}", dto);

                TempData["Success"] = "Record updated successfully.";

                return View("Create",dto);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"designation/{id}");

            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? designationId = null, string? companyId = null)
        {
            // Department
            var parentDesignations = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/parent-designation?tenantId={_tenantId}&designationId={designationId}");

            ViewBag.ParentDesignationList = new SelectList(
                parentDesignations,
                "Value",
                "Text");

            ViewBag.ParentDesignations = parentDesignations.ToDictionary(x => x.Value, x => x.Text);

            // Department
            var departments = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/department");

            ViewBag.DepartmentList = new SelectList(departments,"Value","Text");

            ViewBag.ParentDepartments = departments.ToDictionary(x => x.Value, x => x.Text);

            // =========================
            // Company Dropdown
            // =========================
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");

            ViewBag.CompanyNames = companies.ToDictionary(x => x.Value, x => x.Text);

            // =========================
            // Brances Dropdown
            // =========================
            List<DropdownDto> branches = new();

            if (!string.IsNullOrEmpty(companyId))
            {
                branches = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/branch/{companyId}"
                    );
            }

            ViewBag.BranchList = branches.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            ViewBag.BranchNames = branches.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/branch/{companyId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }
    }

    #endregion
}

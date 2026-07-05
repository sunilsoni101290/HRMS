using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Asset Controller

    [JwtAuthorize]
    public class AssetController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public AssetController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<AssetListDto>>("asset");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new AssetDto { Status = "Available" });
        }

        [HttpPost]
        public async Task<IActionResult> Create(AssetDto dto)
        {
            if (dto!=null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("asset", dto);

                TempData["Success"] = "Record saved successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.CompanyId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<AssetDto>($"asset/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<AssetDto>($"asset/{id}");
            await LoadDropdowns(data.CompanyId);
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, AssetDto dto)
        {
            if (ModelState.IsValid)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"asset/{id}", dto);

                TempData["Success"] = "Record updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.CompanyId);
            return View("Create", dto);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"asset/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? companyId = null)
        {
            var categories = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/asset-category");

            ViewBag.CategoryList = new SelectList(categories, "Value", "Text");

            var companies = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/company");

            ViewBag.CompanyList = new SelectList(companies, "Value", "Text");

            List<DropdownDto> branches = new();

            if (!string.IsNullOrEmpty(companyId))
            {
                branches = await _apiService
                    .GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}");
            }

            ViewBag.BranchList = branches.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            ViewBag.StatusList = new SelectList(new[]
            {
                "Available", "Allocated", "UnderMaintenance", "Scrap"
            });
        }

        #endregion

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var branches = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}");

            var result = branches.Select(x => new { value = x.Value, text = x.Text });
            return Json(result);
        }
    }

    #endregion
}

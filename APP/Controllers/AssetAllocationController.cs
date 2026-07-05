using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region Asset Allocation Controller

    [JwtAuthorize]
    public class AssetAllocationController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public AssetAllocationController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<AssetAllocationListDto>>("asset-allocation");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new AssetAllocationDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(AssetAllocationDto dto)
        {
            if (dto!=null)
            {
                dto.TenantId = _tenantId;
                dto.CreatedBy = _userId;
                dto.AllocationStatus = 1; // Allocated

                await _apiService.PostAsync<dynamic>("asset-allocation", dto);

                TempData["Success"] = "Asset allocated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.AssetId);
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<AssetAllocationDto>($"asset-allocation/{id}");
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<AssetAllocationDto>($"asset-allocation/{id}");
            await LoadDropdowns(data.AssetId);
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, AssetAllocationDto dto)
        {
            if (ModelState.IsValid)
            {
                dto.TenantId = _tenantId;
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"asset-allocation/{id}", dto);

                TempData["Success"] = "Allocation updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns(dto.AssetId);
            return View("Create", dto);
        }

        [HttpPost]
        public async Task<IActionResult> Return(string id)
        {
            var dto = new AssetAllocationDto
            {
                Id = id,
                ReturnedOn = DateTime.UtcNow,
                ModifiedBy = _userId,
                CreatedBy = _userId
            };

            await _apiService.PutAsync<dynamic>($"asset-allocation/return/{id}", dto);

            TempData["Success"] = "Asset returned successfully.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"asset-allocation/{id}");
            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns(string? assetId = null)
        {
            var assets = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/available-asset?assetId={assetId}");

            ViewBag.AssetList = new SelectList(assets, "Value", "Text");

            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");
        }

        #endregion
    }

    #endregion
}

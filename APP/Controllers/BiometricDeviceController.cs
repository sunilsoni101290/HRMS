using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    public class BiometricDeviceController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tenantId;
        private string _userId;
        public BiometricDeviceController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<BiometricDeviceDto>>($"BiometricDevice");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new BiometricDeviceDto());
        }

        [HttpPost]
        public async Task<ActionResult> Create(BiometricDeviceDto model)
        {
            //if (!ModelState.IsValid)
            //    return View(model);

            try
            {
                model.TenantId = _tenantId;
                model.CreatedBy = _userId;

                var response =
                    await _apiService.PostAsync<BiometricDeviceDto, ApiResponse<BiometricDeviceDto>>
                    (
                        $"BiometricDevice",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "",
                    response.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }

            await LoadDropdowns();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<BiometricDeviceDto>($"BiometricDevice/{id}");

            if (data == null)
                return NotFound();

            await LoadDropdowns(data.CompanyId);

            return View("Create", data);
        }

        [HttpPut]
        public async Task<ActionResult> Edit(BiometricDeviceDto model)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                model.TenantId = _tenantId;
                model.CreatedBy = _userId;
                model.ModifiedBy = _userId;
                model.ModifiedOn = DateTime.UtcNow;

                await LoadDropdowns(model.CompanyId);

                var response =
                    await _apiService.PutAsync<BiometricDeviceDto, ApiResponse<BiometricDeviceDto>>
                    (
                        $"BiometricDevice",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "",
                    response.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<BiometricDeviceDto>(
                $"BiometricDevice/{id}"
            );

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> TestConnection(string id)
        {
            var data = await _apiService.GetAsync<bool>(
                $"BiometricDevice/test/{id}"
            );

            if (data)
            {
                AlertHelper.Success(TempData, "Test Connection success.");
                return RedirectToAction(nameof(Index));
            }

            return View(data);
        }

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var branches = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/branch/{companyId}"
                );

            var result = branches.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }

        #region LoadDropdowns

        private async Task LoadDropdowns(string? companyId = null)
        {
            // Company
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text"
            );

            // Branch
            List<DropdownDto> branches = new();

            if (!string.IsNullOrEmpty(companyId))
            {
                branches = await _apiService
                    .GetAsync<List<DropdownDto>>
                    ($"dropdown/branch/{companyId}");
            }

            ViewBag.BranchList = new SelectList(
                branches,
                "Value",
                "Text"
            );
        }

        #endregion
    }
}

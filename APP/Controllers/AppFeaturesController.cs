using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class AppFeaturesController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public AppFeaturesController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }
        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<AppFeatureDto>>("AppFeatures");
            await LoadDropdowns();
            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new AppFeatureDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(AppFeatureDto dto)
        {
            if (dto != null)
            {
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>("AppFeatures", dto);

                TempData["Success"] = "Feature created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<AppFeatureDto>($"AppFeatures/{id}");
            await LoadDropdowns();
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AppFeatureDto dto)
        {
            if (dto != null && !string.IsNullOrEmpty(dto.Id) && ModelState.IsValid)
            {
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>("AppFeatures", dto);

                TempData["Success"] = "Feature updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<AppFeatureDto>($"AppFeatures/{id}");
            if (data == null)
                return NotFound();

            return View(data);
        }
        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"AppFeatures/{id}");

            return RedirectToAction(nameof(Index));
        }

        #region Load Dropdowns

        private async Task LoadDropdowns()
        {
            
            // Parent Features
            var featuers = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/parent-appfeature");

            ViewBag.ParentFeaturesList = new SelectList(featuers, "Value", "Text");

            ViewBag.ParentFeatures = featuers.ToDictionary(x => x.Value, x => x.Text);
        }

        #endregion
    }
}

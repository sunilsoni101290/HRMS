using APP.Attributes;
using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    // Feature Management CRUD screens below are System Configurator ONLY
    // (requirement: lock down Feature Management same as Error Log /
    // Database Management). [SystemConfiguratorOnly] is the "don't even
    // show the page" convenience gate; the real, data-driven check is
    // still IAppFeatureService.EnsurePermissionAsync on the API side, so
    // actingUserId is appended to every call below. GetMyFavorites/
    // ToggleFavorite are deliberately NOT restricted - every logged-in
    // user needs those for their own sidebar Quick Access.
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

        [SystemConfiguratorOnly]
        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<AppFeatureDto>>($"AppFeatures?actingUserId={Uri.EscapeDataString(_userId ?? "")}");
            await LoadDropdowns();
            return View(data);
        }

        [SystemConfiguratorOnly]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new AppFeatureDto());
        }

        [SystemConfiguratorOnly]
        [HttpPost]
        public async Task<IActionResult> Create(AppFeatureDto dto)
        {
            if (dto != null)
            {
                dto.CreatedBy = _userId;

                await _apiService.PostAsync<dynamic>($"AppFeatures?actingUserId={Uri.EscapeDataString(_userId ?? "")}", dto);

                TempData["Success"] = "Feature created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View(dto);
        }

        [SystemConfiguratorOnly]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<AppFeatureDto>($"AppFeatures/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}");
            if (data == null)
                return NotFound();

            await LoadDropdowns();
            return View("Create", data);
        }

        [SystemConfiguratorOnly]
        [HttpPost]
        public async Task<IActionResult> Edit(AppFeatureDto dto)
        {
            if (dto != null && !string.IsNullOrEmpty(dto.Id) && ModelState.IsValid)
            {
                dto.ModifiedBy = _userId;
                dto.ModifiedOn = DateTime.UtcNow;

                await _apiService.PutAsync<dynamic>($"AppFeatures?actingUserId={Uri.EscapeDataString(_userId ?? "")}", dto);

                TempData["Success"] = "Feature updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdowns();
            return View("Create", dto);
        }

        [SystemConfiguratorOnly]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService
                .GetAsync<AppFeatureDto>($"AppFeatures/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}");
            if (data == null)
                return NotFound();

            return View(data);
        }

        [SystemConfiguratorOnly]
        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"AppFeatures/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}");

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

        #region Menu Bar Redesign - Favorites / Quick Access

        // Called by the sidebar's JS on every page load (_LayoutMain.cshtml)
        // to render the "Quick Access" section. JSON, not a view - this is
        // an AJAX-only endpoint, reachable from any page regardless of
        // which controller that page belongs to.
        [HttpGet]
        public async Task<IActionResult> GetMyFavorites()
        {
            if (string.IsNullOrEmpty(_userId))
                return Json(new List<AppFeatureDto>());

            try
            {
                var data = await _apiService
                    .GetAsync<List<AppFeatureDto>>($"AppFeatures/favorites/user/{_userId}");
                return Json(data ?? new List<AppFeatureDto>());
            }
            catch
            {
                return Json(new List<AppFeatureDto>());
            }
        }

        // Star-icon toggle next to each menu item - one endpoint, current
        // pinned state decides add vs remove so the frontend does not need
        // to track which call to make.
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(string appFeatureId, bool pin)
        {
            if (string.IsNullOrEmpty(_userId) || string.IsNullOrEmpty(appFeatureId))
                return Json(new { success = false, message = "Invalid request." });

            try
            {
                if (pin)
                {
                    await _apiService.PostAsync<dynamic>("AppFeatures/favorites", new FavoriteMenuRequestDto
                    {
                        UserId = _userId,
                        AppFeatureId = appFeatureId,
                        TenantId = _tenantId
                    });
                }
                else
                {
                    await _apiService.DeleteAsync($"AppFeatures/favorites/{_userId}/{appFeatureId}");
                }

                return Json(new { success = true });
            }
            catch
            {
                return Json(new { success = false, message = "Could not update Quick Access. Please try again." });
            }
        }

        #endregion
    }
}

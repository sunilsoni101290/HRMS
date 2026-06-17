using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region City Controller
    [JwtAuthorize]
    public class CityController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public CityController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        // =====================================================
        // INDEX
        // =====================================================
        public async Task<IActionResult> Index()
        {
            var data = await _apiService.GetAsync<List<CityDto>>(
                "city"
            );

            return View(data);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();

            return View(new CityDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CityDto dto)
        {
            try
            {
                if (dto!=null)
                {
                    await LoadDropdowns();


                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<CityDto>(
                        "city",
                        dto
                    );
                    AlertHelper.Success(TempData, "City created successfully.");
                    return View(dto);
                }

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDropdowns();

                return View(dto);
            }
            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // EDIT - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<CityDto>(
                $"city/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDropdowns(data.CountryId);

            return View("Create",data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CityDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.Id) && dto!=null)
                {
                    await LoadDropdowns();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;
                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;

                    await _apiService.PutAsync<dynamic>(
                        $"city/{dto.Id}",
                        dto
                    );

                    AlertHelper.Success(TempData, "City updated successfully.");

                    return View("Create", dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDropdowns();

                return View("Create",dto);
            }
            return RedirectToAction(nameof(Index));

        }

        // =====================================================
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<CityDto>(
                $"city/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            return View(data);
        }

        // =====================================================
        // DELETE
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return NotFound();
                }

                await _apiService.DeleteAsync(
                    $"city/{id}"
                );

                TempData["Success"] = "City deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<JsonResult> GetStatesByCountry(string countryId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/state/{countryId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }

        // =====================================================
        // LOAD STATE DROPDOWN
        // =====================================================
        private async Task LoadDropdowns(string? selectedCountryId = null)
        {
            // =========================
            // Country Dropdown
            // =========================
            var countries = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/country");

            ViewBag.CountryList = countries.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            // =========================
            // State Dropdown
            // =========================
            List<DropdownDto> states = new();

            if (!string.IsNullOrEmpty(selectedCountryId))
            {
                states = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/state/{selectedCountryId}"
                    );
            }

            ViewBag.StateList = states.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();
        }
    }

    #endregion
}

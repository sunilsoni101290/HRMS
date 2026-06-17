using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class BranchController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public BranchController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data =  await _apiService.GetAsync<List<BranchDto>>(
                "branch"
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

            return View(new BranchDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BranchDto dto)
        {
            try
            {
                if (dto != null)
                {
                    await LoadDropdowns();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<BranchDto>(
                        "branch",
                        dto
                    );

                    TempData["Success"] = "Branch created successfully.";

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

            var data = await _apiService.GetAsync<BranchDto>(
                $"branch/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDropdowns(data.CountryId, data.StateId);

            return View("Create", data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(BranchDto dto)
        {
            try
            {
                if (dto != null && !string.IsNullOrEmpty(dto.Id))
                {
                    await LoadDropdowns(dto.CountryId, dto.StateId);

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;
                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;

                    await _apiService.PutAsync<dynamic>($"branch/{dto.Id}", dto);

                    TempData["Success"] = "Branch updated successfully.";
                    return View("Create", dto);
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
        // DETAILS
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<BranchDto>(
                $"branch/{id}"
            );

            return View(data);
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

        [HttpGet]
        public async Task<JsonResult> GetCityByState(string stateId)
        {
            var states = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/city/{stateId}"
                );

            var result = states.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }


        #region Private method
        private async Task LoadDropdowns(string? selectedCountryId = null, string? selectedStatedId = null)
        {

            // =========================
            // Company Dropdown
            // =========================
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text");


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


            // ==============================
            // CITY
            // ==============================
            List<DropdownDto> cities = new();
            if (!string.IsNullOrEmpty(selectedStatedId))
            {
                cities = await _apiService
                    .GetAsync<List<DropdownDto>>(
                        $"dropdown/city/{selectedStatedId}"
                    );
            }

            ViewBag.CityList = cities.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            
        }
        #endregion
    }
}

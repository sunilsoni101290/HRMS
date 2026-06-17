using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    #region Country Controller

    [JwtAuthorize]
    public class CountryController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;
        public CountryController(IApiService apiService)
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
            var data = await _apiService.GetAsync<List<CountryDto>>("country");

            return View(data);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CountryDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CountryDto dto)
        {
            try
            {
                if (dto!=null)
                {

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    var response = await _apiService.PostAsync<CountryDto>("Country", dto);

                    TempData["Success"] = "Country created successfully.";
                }

            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
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

            var data = await _apiService.GetAsync<CountryDto>($"Country/{id}");

            if (data == null)
            {
                return NotFound();
            }

            return View("Create",data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CountryDto dto)
        {
            try
            {
                if (dto != null)
                {

                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;

                    await _apiService.PutAsync<dynamic>($"Country/{dto.Id}", dto);

                    TempData["Success"] = "Record updated successfully.";

                    return View("Create", dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
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
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var data = await _apiService.GetAsync<CountryDto>($"Country/{id}");

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

                await _apiService.DeleteAsync($"Country/{id}");

                TempData["Success"] = "Country deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }

    #endregion
}

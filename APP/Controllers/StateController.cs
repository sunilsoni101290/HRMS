using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    #region State Controller
    [JwtAuthorize]

    public class StateController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public StateController(IApiService apiService)
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
            var data = await _apiService.GetAsync<List<StateDto>>("state");

            return View(data);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadCountryDropdown();

            return View(new StateDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StateDto dto)
        {
            try
            {
                if (dto != null)
                {
                    await LoadCountryDropdown();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<StateDto>(
                        "state",
                        dto
                    );

                    TempData["Success"] = "State created successfully.";

                    return View("Create",dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadCountryDropdown();

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

            var data = await _apiService.GetAsync<StateDto>(
                $"state/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadCountryDropdown();

           return View("Create", data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(StateDto dto)
        {
            try
            {
                if (dto!=null && !string.IsNullOrEmpty(dto.Id))
                {
                    await LoadCountryDropdown();

                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;

                    await _apiService.PutAsync<dynamic>(
                        $"state/{dto.Id}",
                        dto
                    );

                    TempData["Success"] = "State updated successfully.";
                    return View("Create", dto);
                }
                
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadCountryDropdown();

                return View("Create", dto);
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

            var data = await _apiService.GetAsync<StateDto>(
                $"state/{id}"
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
                    $"state/{id}"
                );

                TempData["Success"] = "State deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // LOAD COUNTRY DROPDOWN
        // =====================================================
        private async Task LoadCountryDropdown()
        {
            var countries = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/country");

            ViewBag.CountryList = countries.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();
        }
    }

    #endregion
}

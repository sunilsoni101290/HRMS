using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class WeekOffController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public WeekOffController(IApiService apiService)
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
            var data = await _apiService.GetAsync<List<WeekOffDto>>(
                "weekoff"
            );

            return View(data);
        }

        // =====================================================
        // CREATE - GET
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDayDropdown();

            return View(new WeekOffDto());
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WeekOffDto dto)
        {
            try
            {
                if (dto != null )
                {
                    await LoadDayDropdown();

                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PostAsync<WeekOffDto>(
                        "weekoff",
                        dto
                    );

                    TempData["Success"] = "Week Off created successfully.";
                    return View(dto);
                }
                
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDayDropdown();

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

            var data = await _apiService.GetAsync<WeekOffDto>(
                $"weekoff/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDayDropdown();

            return View("Create",data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(WeekOffDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await LoadDayDropdown();


                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;
                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;

                    await _apiService.PutAsync<dynamic>(
                        $"weekoff/{dto.Id}",
                        dto
                    );

                    TempData["Success"] = "Week Off updated successfully.";
                    return View("Create", dto);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;

                await LoadDayDropdown();

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

            var data = await _apiService.GetAsync<WeekOffDto>(
                $"weekoff/{id}"
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
                    $"weekoff/{id}"
                );

                TempData["Success"] = "Week Off deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // LOAD DAY DROPDOWN
        // =====================================================
        private async Task LoadDayDropdown()
        {
            ViewBag.DayList = Enum.GetValues(typeof(DayOfWeek))
                .Cast<DayOfWeek>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                }).ToList();

            await Task.CompletedTask;
        }
    }
}

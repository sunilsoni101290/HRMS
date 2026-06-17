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
    public class FinancialYearController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public FinancialYearController(IApiService apiService)
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
            var data = await _apiService.GetAsync<List<FinancialYearDto>>(
                "financialyear"
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

            var today = DateTime.Today;

            int startYear = today.Month >= 4 ? today.Year : today.Year - 1;

            DateTime fyStart = new DateTime(startYear, 4, 1);
            DateTime fyEnd = new DateTime(startYear + 1, 3, 31);

            return View(new FinancialYearDto
            {
                StartDate = fyStart,
                EndDate = fyEnd
            });
        }

        // =====================================================
        // CREATE - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FinancialYearDto dto)
        {
            try
            {
                if (dto!=null)
                {
                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await LoadDropdowns();

                    if (dto.EndDate < dto.StartDate)
                    {
                        ModelState.AddModelError(
                            nameof(dto.EndDate),
                            "End Date must be greater than Start Date."
                        );

                        await LoadDropdowns();
                        return View(dto);
                    }

                    dto.CreatedBy = User.Identity?.Name ?? "Admin";

                    await _apiService.PostAsync<FinancialYearDto>(
                        "financialyear",
                        dto
                    );

                    AlertHelper.Success(TempData, "Financial Year created successfully.");
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

            var data = await _apiService.GetAsync<FinancialYearDto>(
                $"financialyear/{id}"
            );

            if (data == null)
            {
                return NotFound();
            }

            await LoadDropdowns();

            return View("Create",data);
        }

        // =====================================================
        // EDIT - POST
        // =====================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FinancialYearDto dto)
        {
            try
            {
                if (!string.IsNullOrEmpty(dto.Id) &&  dto!=null)
                {
                    await LoadDropdowns();


                    if (dto.EndDate < dto.StartDate)
                    {
                        ModelState.AddModelError(
                            nameof(dto.EndDate),
                            "End Date must be greater than Start Date."
                        );

                        await LoadDropdowns();
                        return View(dto);
                    }

                    dto.ModifiedOn = DateTime.UtcNow;
                    dto.ModifiedBy = _userId;
                    dto.CreatedBy = _userId;
                    dto.TenantId = _tenantId;

                    await _apiService.PutAsync<dynamic>(
                        $"financialyear/{dto.Id}",
                        dto
                    );

                    AlertHelper.Success(TempData, "Financial Year updated successfully.");
                    return View("Create",dto);
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

            var data = await _apiService.GetAsync<FinancialYearDto>(
                $"financialyear/{id}"
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
                    $"financialyear/{id}"
                );

                AlertHelper.Success(TempData, "Financial Year deleted successfully.");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // =====================================================
        // LOAD DROPDOWNS
        // =====================================================
        private async Task LoadDropdowns()
        {
            // ==========================
            // Company Dropdown
            // ==========================
            var companies = await _apiService.GetAsync<List<DropdownDto>>(
                "dropdown/company"
            );

            ViewBag.CompanyList = companies.Select(x => new SelectListItem
            {
                Value = x.Value,
                Text = x.Text
            }).ToList();

            // ==========================
            // Status Dropdown
            // ==========================
            ViewBag.StatusList = Enum.GetValues(typeof(FinancialYearStatus))
                .Cast<FinancialYearStatus>()
                .Select(x => new SelectListItem
                {
                    Value = ((int)x).ToString(),
                    Text = x.ToString()
                }).ToList();
        }
    }
}

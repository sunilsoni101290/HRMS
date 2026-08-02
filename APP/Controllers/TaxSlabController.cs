using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // Admin-only master-data CRUD for income-tax slabs - see
    // API/Controllers/TaxSlabController.cs (api/taxslab). Named exactly
    // "TaxSlab" to match AppFeatureConstants.TAX_SLAB_CONTROLLER.
    [JwtAuthorize]
    public class TaxSlabController : Controller
    {
        private readonly IApiService _apiService;

        public TaxSlabController(IApiService apiService)
        {
            _apiService = apiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? financialYearId, int? regime)
        {
            ViewBag.FinancialYearId = financialYearId;
            ViewBag.Regime = regime;

            await BindFinancialYearDropdown(financialYearId);
            ViewBag.RegimeList = EnumHelper.GetEnumList<TaxRegime>();

            var url = $"taxslab?financialYearId={Uri.EscapeDataString(financialYearId ?? string.Empty)}" +
                      (regime.HasValue ? $"&regime={regime.Value}" : string.Empty);

            try
            {
                var data = await _apiService.GetAsync<List<TaxSlabDto>>(url);
                return View(data ?? new List<TaxSlabDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Tax Slabs.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadFormViewDataAsync();
            return View(new TaxSlabDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaxSlabDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadFormViewDataAsync();
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<TaxSlabDto, TaxSlabDto>("taxslab", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Tax Slab created successfully."
                    : "Unable to create Tax Slab.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await LoadFormViewDataAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<TaxSlabDto>($"taxslab/{id}");

            if (data == null)
                return NotFound();

            await LoadFormViewDataAsync();
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, TaxSlabDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadFormViewDataAsync();
                return View(model);
            }

            try
            {
                var result = await _apiService.PutAsync<TaxSlabDto>($"taxslab/{id}", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Tax Slab updated successfully."
                    : "Unable to update Tax Slab.";

                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await LoadFormViewDataAsync();
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var result = await _apiService.DeleteAsync($"taxslab/{id}");
                TempData[result ? "Success" : "GlobalError"] = result
                    ? "Tax Slab deleted successfully."
                    : "Unable to delete Tax Slab.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadFormViewDataAsync()
        {
            await BindFinancialYearDropdown(null);
            ViewBag.RegimeList = EnumHelper.GetEnumList<TaxRegime>();
        }

        // No "dropdown/financialyear" endpoint exists in
        // DropdownListController - FinancialYear predates the generic
        // dropdown catalogue, so this fetches the master list directly
        // from api/financialyear (FinancialYearController.GetAll) instead.
        private async Task BindFinancialYearDropdown(string? selectedId)
        {
            var years = await _apiService.GetAsync<List<FinancialYearDto>>("financialyear")
                ?? new List<FinancialYearDto>();

            var options = years
                .OrderByDescending(x => x.StartDate)
                .Select(x => new { Value = x.Id, Text = x.Name })
                .ToList();

            ViewBag.FinancialYearList = new SelectList(options, "Value", "Text", selectedId);
        }

        private string GetErrorMessage(string raw)
        {
            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }

                return "Unable to process this Tax Slab request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Tax Slab request." : raw;
            }
        }
    }
}

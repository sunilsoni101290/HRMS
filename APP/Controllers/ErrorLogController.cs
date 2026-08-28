using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>
    /// Error Log management screen (AppFeatureConstants.ERROR_LOG) -
    /// System Configurator/Admin only, see SystemConfiguratorOnlyAttribute.
    /// Read + Resolve only; error rows themselves are only ever written by
    /// API/Middleware/ExceptionMiddleware or IErrorLogService.LogAsync, never
    /// through this controller (no Create/Delete actions here by design -
    /// requirement #7: never physically delete an error record).
    /// </summary>
    [JwtAuthorize]
    [SystemConfiguratorOnly]
    public class ErrorLogController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _userId;

        public ErrorLogController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
        }

        [HttpGet]
        public async Task<IActionResult> Index(ErrorLogFilterDto filter)
        {
            filter ??= new ErrorLogFilterDto();

            var url = BuildFilterQuery(filter) + $"&actingUserId={Uri.EscapeDataString(_userId ?? "")}";

            var response = await _apiService
                .GetAsync<ApiResponse<PagedResult<ErrorLogDto>>>(url);

            var result = response?.Data ?? new PagedResult<ErrorLogDto>
            {
                Data = new List<ErrorLogDto>(),
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalRecords = 0
            };

            ViewBag.Filter = filter;

            return View(result);
        }

        // Details rendered as a modal fetched via AJAX from the Index page
        // (requirement #6) - GET so it can also be opened directly by URL.
        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var response = await _apiService
                .GetAsync<ApiResponse<ErrorLogDto>>(
                    $"ErrorLog/{id}?actingUserId={Uri.EscapeDataString(_userId ?? "")}");

            if (response?.Data == null)
                return NotFound();

            return PartialView("_Details", response.Data);
        }

        [HttpPost]
        public async Task<JsonResult> Resolve([FromBody] ResolveErrorLogDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<ResolveErrorLogDto, ApiResponse<object>>(
                    $"ErrorLog/resolve?actingUserId={Uri.EscapeDataString(_userId ?? "")}",
                    model);

                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Unable to reach the ERP API." });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private static string BuildFilterQuery(ErrorLogFilterDto filter)
        {
            var qs = new List<string>();

            void Add(string key, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    qs.Add($"{key}={Uri.EscapeDataString(value)}");
            }

            if (filter.DateFrom.HasValue) Add("DateFrom", filter.DateFrom.Value.ToString("yyyy-MM-dd"));
            if (filter.DateTo.HasValue) Add("DateTo", filter.DateTo.Value.ToString("yyyy-MM-dd"));
            Add("ModuleName", filter.ModuleName);
            Add("FeatureName", filter.FeatureName);
            Add("UserName", filter.UserName);
            Add("ErrorMessage", filter.ErrorMessage);
            Add("Controller", filter.Controller);
            Add("Action", filter.Action);
            if (filter.IsResolved.HasValue) qs.Add($"IsResolved={filter.IsResolved.Value}");

            qs.Add($"PageNumber={(filter.PageNumber < 1 ? 1 : filter.PageNumber)}");
            qs.Add($"PageSize={(filter.PageSize < 1 ? 20 : filter.PageSize)}");

            return "ErrorLog?" + string.Join("&", qs);
        }

        // Same shape as ApiService.HandleResponse's error body - mirrors the
        // GetErrorMessage helper used across the rest of the APP controllers
        // (e.g. EmployeeBankController/BiometricSimulatorController).
        private static string GetErrorMessage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "The ERP API returned an error.";

            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["message"] != null)
                    return obj["message"]!.ToString();

                return raw;
            }
            catch
            {
                return raw;
            }
        }
    }
}

using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>
    /// eSSL eTimeTrackLite1 direct-SQL attendance integration - admin
    /// screen (Settings/Test Connection/Save/Sync Now, Sync Logs, Unmapped
    /// Employees). Admin/HR-only via EssRestrictionAttribute's
    /// AdminOnlyControllers list (same gating style as BiometricDevice/
    /// EmployeeBiometricMapping).
    /// </summary>
    [JwtAuthorize]
    public class EsslAttendanceController : Controller
    {
        private readonly IApiService _apiService;

        public EsslAttendanceController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public async Task<IActionResult> Index()
        {
            // Card 1 (status) and Card 2 (configuration) are two separate
            // API calls/DTOs by design (requirement #14 - keep status and
            // configuration visibly, logically separate) - fetched together
            // here purely for one page load.
            var statusResponse = await _apiService.GetAsync<ApiResponse<EsslSyncSettingsDto>>("EsslAttendance/settings");
            var configResponse = await _apiService.GetAsync<ApiResponse<EsslDatabaseConfigViewDto>>("EsslAttendance/configuration");

            var model = new EsslSettingsPageViewModel
            {
                Status = statusResponse?.Data ?? new EsslSyncSettingsDto(),
                Config = configResponse?.Data ?? new EsslDatabaseConfigViewDto()
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> SyncLogs(EsslSyncHistoryFilterDto filter)
        {
            filter ??= new EsslSyncHistoryFilterDto();

            var qs = new List<string>
            {
                $"PageNumber={(filter.PageNumber < 1 ? 1 : filter.PageNumber)}",
                $"PageSize={(filter.PageSize < 1 ? 20 : filter.PageSize)}"
            };

            if (filter.DateFrom.HasValue) qs.Add($"DateFrom={filter.DateFrom.Value:yyyy-MM-dd}");
            if (filter.DateTo.HasValue) qs.Add($"DateTo={filter.DateTo.Value:yyyy-MM-dd}");

            var response = await _apiService.GetAsync<ApiResponse<PagedResult<EsslSyncHistoryDto>>>(
                "EsslAttendance/sync-history?" + string.Join("&", qs));

            var result = response?.Data ?? new PagedResult<EsslSyncHistoryDto>
            {
                Data = new List<EsslSyncHistoryDto>(),
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalRecords = 0
            };

            ViewBag.Filter = filter;

            return PartialView("_SyncLogs", result);
        }

        [HttpGet]
        public async Task<IActionResult> UnmappedEmployees()
        {
            var response = await _apiService.GetAsync<ApiResponse<List<EsslUnmappedEmployeeDto>>>(
                "EsslAttendance/unmapped-employees");

            return PartialView("_UnmappedEmployees", response?.Data ?? new List<EsslUnmappedEmployeeDto>());
        }

        // Re-fetches Card 2's current saved values - backs the Cancel
        // button's "restore the currently persisted values" behavior
        // without a full page reload.
        [HttpGet]
        public async Task<JsonResult> GetConfiguration()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<EsslDatabaseConfigViewDto>>("EsslAttendance/configuration");
                return Json(new { success = true, data = response?.Data });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        [HttpPost]
        public async Task<JsonResult> TestConnection([FromBody] EsslDatabaseConfigDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<EsslDatabaseConfigDto, ApiResponse<object>>(
                    "EsslAttendance/test-connection", model ?? new EsslDatabaseConfigDto());

                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Unable to reach the ERP API." });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        [HttpPost]
        public async Task<JsonResult> SaveConfiguration([FromBody] EsslDatabaseConfigDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<EsslDatabaseConfigDto, ApiResponse<object>>(
                    "EsslAttendance/configuration", model ?? new EsslDatabaseConfigDto());

                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Unable to reach the ERP API." });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        // Backs both "Sync Now" (no dates - incremental) and the Historical
        // Import form (From/To dates).
        [HttpPost]
        public async Task<JsonResult> SyncNow([FromBody] EsslSyncRequestDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<EsslSyncRequestDto, ApiResponse<EsslSyncResultDto>>(
                    "EsslAttendance/sync", model ?? new EsslSyncRequestDto());

                return Json(new
                {
                    success = response?.Success ?? false,
                    message = response?.Message ?? "Unable to reach the ERP API.",
                    data = response?.Data
                });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.Message) });
            }
        }

        // Same shape as the GetErrorMessage helper used across the rest of
        // the APP controllers (e.g. ErrorLogController/EmployeeBiometricMappingController),
        // EXTENDED to also understand the ValidationProblemDetails shape
        // ([ApiController]'s automatic DataAnnotations 400 response -
        // {"errors":{"Field":["message"]}, ...} - see
        // API/Filters/FluentValidationActionFilter.cs's remarks on why this
        // app's API deliberately uses that same shape for both automatic
        // model-binding errors and FluentValidation errors).
        private static string GetErrorMessage(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "The ERP API returned an error.";

            try
            {
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null) return obj["Message"]!.ToString();
                if (obj["message"] != null) return obj["message"]!.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject errors)
                {
                    foreach (var prop in errors.Properties())
                    {
                        if (prop.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]!.ToString();
                    }
                }

                return raw;
            }
            catch
            {
                return raw;
            }
        }
    }
}

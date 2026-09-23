using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>
    /// Standalone "Historical Attendance Sync" admin screen - a COMPLETELY
    /// SEPARATE controller/view from EsslAttendance (never modified, never
    /// referenced here). Lets an admin pick a historical From/To date range
    /// (optionally one Employee), preview what it would touch, and start a
    /// background job that re-processes EXISTING BiometricAttendanceLogs
    /// rows into AttendanceLogs/Attendances - it never fetches anything new
    /// from eSSL/any biometric device. Admin/HR-only via
    /// EssRestrictionAttribute's AdminOnlyControllers list (same gating
    /// style as EsslAttendance/EmployeeBiometricMapping).
    /// </summary>
    [JwtAuthorize]
    public class HistoricalAttendanceSyncController : Controller
    {
        private readonly IApiService _apiService;

        public HistoricalAttendanceSyncController(IApiService apiService)
        {
            _apiService = apiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<JsonResult> Preview([FromBody] HistoricalSyncRequestDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<HistoricalSyncRequestDto, ApiResponse<HistoricalSyncPreviewDto>>(
                    "HistoricalAttendanceSync/preview", model ?? new HistoricalSyncRequestDto());

                return Json(new { success = response?.Success ?? false, message = response?.Message, data = response?.Data });
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
        public async Task<JsonResult> Start([FromBody] HistoricalSyncRequestDto model)
        {
            try
            {
                var response = await _apiService.PostAsync<HistoricalSyncRequestDto, ApiResponse<HistoricalSyncStartResultDto>>(
                    "HistoricalAttendanceSync/start", model ?? new HistoricalSyncRequestDto());

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

        // JSON status poll - backs the UI's Progress/Status card while a
        // job is Queued/Running (see Index.cshtml's pollJobStatus()).
        [HttpGet]
        public async Task<JsonResult> GetStatus(string jobId)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<HistoricalSyncJobDto>>(
                    "HistoricalAttendanceSync/status/" + Uri.EscapeDataString(jobId ?? ""));

                return Json(new { success = response?.Success ?? false, message = response?.Message, data = response?.Data });
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

        [HttpGet]
        public async Task<IActionResult> History(HistoricalSyncJobFilterDto filter)
        {
            filter ??= new HistoricalSyncJobFilterDto();

            var qs = new List<string>
            {
                $"PageNumber={(filter.PageNumber < 1 ? 1 : filter.PageNumber)}",
                $"PageSize={(filter.PageSize < 1 ? 20 : filter.PageSize)}"
            };

            if (filter.DateFrom.HasValue) qs.Add($"DateFrom={filter.DateFrom.Value:yyyy-MM-dd}");
            if (filter.DateTo.HasValue) qs.Add($"DateTo={filter.DateTo.Value:yyyy-MM-dd}");
            if (!string.IsNullOrWhiteSpace(filter.Status)) qs.Add($"Status={Uri.EscapeDataString(filter.Status)}");

            var response = await _apiService.GetAsync<ApiResponse<PagedResult<HistoricalSyncJobDto>>>(
                "HistoricalAttendanceSync/history?" + string.Join("&", qs));

            var result = response?.Data ?? new PagedResult<HistoricalSyncJobDto>
            {
                Data = new List<HistoricalSyncJobDto>(),
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalRecords = 0
            };

            return PartialView("_JobHistory", result);
        }

        // Same shape as the GetErrorMessage helper used across the rest of
        // the APP controllers (see EsslAttendanceController's identical
        // private helper, copied verbatim per this codebase's convention).
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

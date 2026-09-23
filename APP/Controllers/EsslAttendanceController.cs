using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;

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

            // --------------------------------------------------------
            // ADDITIVE - "Attendance Synchronization" combined-page redesign
            // (see EsslSettingsPageViewModel's remarks). Reads the single
            // most recent row from the SAME two already-existing endpoints
            // the old "Sync Logs" tab and the standalone Historical Sync
            // page already call - no new API surface, no engine change.
            // Best-effort: a failure here must never block the page (it
            // only feeds two summary cards), so it degrades to "no summary
            // yet" rather than failing the whole page load.
            // --------------------------------------------------------
            try
            {
                var lastSyncResp = await _apiService.GetAsync<ApiResponse<PagedResult<EsslSyncHistoryDto>>>(
                    "EsslAttendance/sync-history?PageNumber=1&PageSize=1");
                model.LastSyncHistory = lastSyncResp?.Data?.Data?.FirstOrDefault();
            }
            catch { /* summary card just shows "no sync yet" */ }

            try
            {
                var lastJobResp = await _apiService.GetAsync<ApiResponse<PagedResult<HistoricalSyncJobDto>>>(
                    "HistoricalAttendanceSync/history?PageNumber=1&PageSize=1");
                model.LastHistoricalJob = lastJobResp?.Data?.Data?.FirstOrDefault();
            }
            catch { /* summary card just shows "no historical sync yet" */ }

            // Historical Sync (Manual) card's "Employee (Optional)" dropdown -
            // reuses the SAME already-existing BiometricSimulator/mapped-employees
            // endpoint the Biometric Punch Simulator screen already uses for its
            // own Employee dropdown (APP/Controllers/BiometricSimulatorController.cs),
            // since it is exactly "employees with an active biometric mapping" -
            // the same population Historical Sync itself operates over. No new
            // API endpoint added.
            try
            {
                var tenantId = SessionHelper.GetActiveTenantId;
                var employees = await _apiService
                    .GetAsync<List<SimulatorMappedEmployeeDto>>($"BiometricSimulator/mapped-employees?tenantId={Uri.EscapeDataString(tenantId ?? "")}")
                    ?? new List<SimulatorMappedEmployeeDto>();

                ViewBag.EmployeeList = new SelectList(
                    employees.Where(x => x.IsActive).OrderBy(x => x.EmployeeName),
                    "EmployeeId", "EmployeeName");
            }
            catch
            {
                ViewBag.EmployeeList = new SelectList(new List<SimulatorMappedEmployeeDto>(), "EmployeeId", "EmployeeName");
            }

            return View(model);
        }

        // --------------------------------------------------------------
        // ADDITIVE - unified "Processing Log (Last 50 Lines)" for the
        // combined Attendance Synchronization page. There is no per-line
        // processing-log table/service anywhere in this codebase for
        // either sync feature (only per-run summary rows - EsslSyncHistoryDto
        // / HistoricalSyncJobDto) and IErrorLogService is for unhandled
        // application exceptions, not routine sync narration - so rather
        // than inventing a new persisted log table (out of scope for a
        // UI-only change), this synthesizes readable log lines FROM the
        // same per-run summary rows both features already persist and the
        // page already calls (EsslAttendance/sync-history,
        // HistoricalAttendanceSync/history) - one Start line + one
        // Completed/Failed line per run, merged and time-sorted. Purely a
        // thin APP-side composition; no API or engine change.
        // --------------------------------------------------------------
        [HttpGet]
        public async Task<JsonResult> CombinedLog()
        {
            var lines = new List<object>();

            try
            {
                var esslResp = await _apiService.GetAsync<ApiResponse<PagedResult<EsslSyncHistoryDto>>>(
                    "EsslAttendance/sync-history?PageNumber=1&PageSize=15");

                foreach (var h in esslResp?.Data?.Data ?? new List<EsslSyncHistoryDto>())
                {
                    lines.Add(new { time = h.StartTime, message = $"[eSSL Sync] Started - window {(h.FromDate?.ToString("dd-MMM") ?? "incremental")} to {(h.ToDate?.ToString("dd-MMM") ?? "now")}." });

                    if (h.EndTime.HasValue)
                    {
                        var statusWord = h.Status == "Success" ? "completed successfully" : h.Status == "PartialFailure" ? "completed with warnings" : "failed";
                        lines.Add(new
                        {
                            time = h.EndTime.Value,
                            message = $"[eSSL Sync] {statusWord} - Fetched {h.RecordsFetched}, Imported {h.RecordsInserted}, Skipped {h.RecordsSkipped}, Failed {h.RecordsFailed}." +
                                      (string.IsNullOrEmpty(h.ErrorMessage) ? "" : $" {h.ErrorMessage}")
                        });
                    }
                }
            }
            catch { /* one source failing must not blank out the other */ }

            try
            {
                var jobResp = await _apiService.GetAsync<ApiResponse<PagedResult<HistoricalSyncJobDto>>>(
                    "HistoricalAttendanceSync/history?PageNumber=1&PageSize=15");

                foreach (var j in jobResp?.Data?.Data ?? new List<HistoricalSyncJobDto>())
                {
                    var started = j.StartTime ?? j.CreatedOn;
                    lines.Add(new { time = started, message = $"[Historical Sync] Started - {j.FromDate:dd-MMM-yyyy} to {j.ToDate:dd-MMM-yyyy}{(string.IsNullOrEmpty(j.EmployeeName) ? " (all employees)" : $" - {j.EmployeeName}")}." });

                    if (j.EndTime.HasValue)
                    {
                        var statusWord = j.Status == "Completed" ? "completed successfully" : j.Status == "PartiallyCompleted" ? "completed with warnings" : j.Status == "Failed" ? "failed" : j.Status;
                        lines.Add(new
                        {
                            time = j.EndTime.Value,
                            message = $"[Historical Sync] {statusWord} - Processed {j.ProcessedCount}/{j.TotalRecords}, AttendanceLogs {j.AttendanceLogsCreated}, Attendances Created {j.AttendancesCreated}, Updated {j.AttendancesUpdated}, Unmapped {j.UnmappedCount}, Failed {j.FailedCount}." +
                                      (string.IsNullOrEmpty(j.ErrorSummary) ? "" : $" {j.ErrorSummary}")
                        });
                    }
                }
            }
            catch { /* one source failing must not blank out the other */ }

            var ordered = lines
                .Select(l => (dynamic)l)
                .OrderByDescending(l => (DateTime)l.time)
                .Take(50)
                .Select((l, idx) => new { line = idx + 1, time = ((DateTime)l.time).ToString("hh:mm:ss tt"), message = (string)l.message })
                .ToList();

            return Json(new { success = true, data = ordered });
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

        // JSON status poll, backing the live "Total / Synced / Skipped /
        // Failed" progress readout while a Sync Now / Historical Import
        // run is in progress (see Index.cshtml's pollStatus()). Sync Now
        // itself now returns almost immediately (the actual sync runs in
        // the background - see API's EsslAttendanceController.SyncNow),
        // so the browser polls this same status endpoint every couple of
        // seconds instead of waiting on one long request.
        [HttpGet]
        public async Task<JsonResult> GetStatus()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<EsslSyncSettingsDto>>("EsslAttendance/settings");
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

        // Safe manual recovery for a tenant whose sync lock is genuinely
        // stuck - see API's EsslAttendanceController.ResetStuckSync /
        // IEsslAttendanceSyncService.ResetStuckSyncAsync's remarks. The UI
        // only shows/enables this when the status poll's CanForceReset
        // flag is true, but the real safety gate is server-side.
        [HttpPost]
        public async Task<JsonResult> ResetStuckSync()
        {
            try
            {
                var response = await _apiService.PostAsync<ApiResponse<object>>(
                    "EsslAttendance/reset-stuck-sync", new { });

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

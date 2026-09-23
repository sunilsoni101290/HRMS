using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    /// <summary>
    /// Database Management module (Phase A - scaffold, Settings CRUD,
    /// read-only live Database Info, Execution History reader, and the
    /// visual shell for the Database Update / Backup / Restore / Swap tabs
    /// with their not-yet-implemented steps visibly disabled). System
    /// Configurator ONLY - same combination as ErrorLogController:
    /// [SystemConfiguratorOnly] is the "don't even show the page"
    /// convenience gate on this (MVC) side; the API's
    /// IDatabaseManagementService.EnsurePermissionAsync is the real,
    /// data-driven check.
    /// </summary>
    [JwtAuthorize]
    [SystemConfiguratorOnly]
    public class DatabaseManagementController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _userId;
        private readonly string _tenantId;

        public DatabaseManagementController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
            _tenantId = SessionHelper.GetActiveTenantId;
        }

        [HttpGet]
        public async Task<IActionResult> Index(DatabaseOperationHistoryFilterDto filter)
        {
            filter ??= new DatabaseOperationHistoryFilterDto();

            var settings = await GetSettingsAsync();
            var databaseInfo = await GetDatabaseInfoAsync();
            var history = await GetHistoryAsync(filter);

            ViewBag.Settings = settings;
            ViewBag.DatabaseInfo = databaseInfo;
            ViewBag.Filter = filter;

            return View(history);
        }

        // AJAX partial refresh for the Database Info panel + Quick Actions
        // "Refresh" button - a genuine round trip, not a fake spinner (see
        // Phase A scope's "Refresh must be real").
        [HttpGet]
        public async Task<JsonResult> RefreshDatabaseInfo()
        {
            var info = await GetDatabaseInfoAsync();
            return Json(new { success = true, data = info });
        }

        [HttpGet]
        public async Task<JsonResult> GetHistoryPage(DatabaseOperationHistoryFilterDto filter)
        {
            var history = await GetHistoryAsync(filter ?? new DatabaseOperationHistoryFilterDto());
            return Json(new { success = true, data = history });
        }

        // "Open SSMS" Quick Action - per the Phase A scope, this app cannot
        // launch a local process from a web request, so it surfaces
        // server/database name only (never the password/connection
        // string) for the user to copy into SSMS themselves.
        [HttpGet]
        public async Task<JsonResult> GetConnectionInfo()
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<DatabaseConnectionInfoDto>>(
                    $"DatabaseManagement/connection-info?tenantId={Uri.EscapeDataString(_tenantId ?? "")}&actingUserId={Uri.EscapeDataString(_userId ?? "")}");

                return Json(new { success = response?.Success ?? false, data = response?.Data, message = response?.Message });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> SaveSettings(DatabaseManagementSettingsDto model)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(" ", errors) });
            }

            try
            {
                var response = await _apiService.PutAsync<DatabaseManagementSettingsDto, ApiResponse<object>>(
                    $"DatabaseManagement/settings?tenantId={Uri.EscapeDataString(_tenantId ?? "")}&actingUserId={Uri.EscapeDataString(_userId ?? "")}",
                    model);

                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Unable to reach the ERP API." });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
        }

        private async Task<DatabaseManagementSettingsDto> GetSettingsAsync()
        {
            var response = await _apiService.GetAsync<ApiResponse<DatabaseManagementSettingsDto>>(
                $"DatabaseManagement/settings?tenantId={Uri.EscapeDataString(_tenantId ?? "")}&actingUserId={Uri.EscapeDataString(_userId ?? "")}");

            return response?.Data ?? new DatabaseManagementSettingsDto();
        }

        private async Task<DatabaseInfoDto> GetDatabaseInfoAsync()
        {
            var response = await _apiService.GetAsync<ApiResponse<DatabaseInfoDto>>(
                $"DatabaseManagement/database-info?tenantId={Uri.EscapeDataString(_tenantId ?? "")}&actingUserId={Uri.EscapeDataString(_userId ?? "")}");

            return response?.Data ?? new DatabaseInfoDto { Available = false, UnavailableReason = "Unable to reach the ERP API." };
        }

        private async Task<PagedResult<DatabaseOperationHistoryDto>> GetHistoryAsync(DatabaseOperationHistoryFilterDto filter)
        {
            var url = BuildHistoryQuery(filter) + $"&actingUserId={Uri.EscapeDataString(_userId ?? "")}";

            var response = await _apiService.GetAsync<ApiResponse<PagedResult<DatabaseOperationHistoryDto>>>(url);

            return response?.Data ?? new PagedResult<DatabaseOperationHistoryDto>
            {
                Data = new List<DatabaseOperationHistoryDto>(),
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalRecords = 0
            };
        }

        private string BuildHistoryQuery(DatabaseOperationHistoryFilterDto filter)
        {
            var qs = new List<string> { $"tenantId={Uri.EscapeDataString(_tenantId ?? "")}" };

            void Add(string key, string? value)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    qs.Add($"{key}={Uri.EscapeDataString(value)}");
            }

            if (filter.DateFrom.HasValue) Add("DateFrom", filter.DateFrom.Value.ToString("yyyy-MM-dd"));
            if (filter.DateTo.HasValue) Add("DateTo", filter.DateTo.Value.ToString("yyyy-MM-dd"));
            Add("OperationType", filter.OperationType);
            Add("Status", filter.Status);

            qs.Add($"PageNumber={(filter.PageNumber < 1 ? 1 : filter.PageNumber)}");
            qs.Add($"PageSize={(filter.PageSize < 1 ? 20 : filter.PageSize)}");

            return "DatabaseManagement/history?" + string.Join("&", qs);
        }

        // Same shape as ErrorLogController's GetErrorMessage helper.
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

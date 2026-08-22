using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class BiometricDeviceController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _env;
        private string _tenantId;
        private string _userId;
        public BiometricDeviceController(IApiService apiService, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment env)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _httpContextAccessor = httpContextAccessor;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.IsDevelopment = _env.IsDevelopment();

            var data = await _apiService
                .GetAsync<List<BiometricDeviceDto>>($"BiometricDevice")
                ?? new List<BiometricDeviceDto>();

            // Company / Branch names for the filter dropdowns and for
            // rendering readable labels in the table (the list only
            // carries Ids from the API).
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/company")
                ?? new List<DropdownDto>();

            ViewBag.CompanyList = new SelectList(companies, "Value", "Text");
            ViewBag.CompanyNames = companies.ToDictionary(x => x.Value, x => x.Text);

            var branchNames = new Dictionary<string, string>();

            foreach (var companyId in data
                .Select(x => x.CompanyId)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct())
            {
                var branches = await _apiService
                    .GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}")
                    ?? new List<DropdownDto>();

                foreach (var branch in branches)
                    branchNames[branch.Value] = branch.Text;
            }

            ViewBag.BranchNames = branchNames;

            // Agent names for the "Agent" column - the device list only
            // carries AgentId/AgentName is already flattened by the API, but
            // keep a lookup too in case AgentName ever comes back empty.
            var agents = await _apiService
                .GetAsync<List<BiometricAgentDto>>("BiometricAgent")
                ?? new List<BiometricAgentDto>();

            ViewBag.AgentNames = agents.ToDictionary(x => x.Id!, x => x.AgentName);

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new BiometricDeviceDto());
        }

        [HttpPost]
        public async Task<ActionResult> Create(BiometricDeviceDto model)
        {
            //if (!ModelState.IsValid)
            //    return View(model);

            try
            {
                model.TenantId = _tenantId;
                model.CreatedBy = _userId;

                var response =
                    await _apiService.PostAsync<BiometricDeviceDto, ApiResponse<BiometricDeviceDto>>
                    (
                        $"BiometricDevice",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] =
                        $"{response.Message} Device Key: {response.Data?.DeviceKey} " +
                        "(also visible any time on this device's Details page - you'll need it to configure the BiometricAgent / test the ingest API).";

                    return RedirectToAction(nameof(Details), new { id = response.Data?.Id });
                }

                ModelState.AddModelError(
                    "",
                    response.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }

            await LoadDropdowns(model.CompanyId, model.BranchId);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<BiometricDeviceDto>($"BiometricDevice/{id}");

            if (data == null)
                return NotFound();

            data.TenantId = _tenantId;
            data.CreatedBy = _userId;

            await LoadDropdowns(data.CompanyId, data.BranchId);

            return View("Create", data);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(BiometricDeviceDto model)
        {
            if (model==null && string.IsNullOrEmpty(model.Id))
            {
                await LoadDropdowns(model.CompanyId, model.BranchId);
                return View("Create", model);
            }

            try
            {
                model.TenantId = _tenantId;
                model.CreatedBy = _userId;
                model.ModifiedBy = _userId;
                model.ModifiedOn = DateTime.UtcNow;

                var response =
                    await _apiService.PutAsync<BiometricDeviceDto, ApiResponse<BiometricDeviceDto>>
                    (
                        $"BiometricDevice",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(
                    "",
                    response.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    ex.Message);
            }

            await LoadDropdowns(model.CompanyId, model.BranchId);

            return View("Create", model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<BiometricDeviceDto>(
                $"BiometricDevice/{id}"
            );

            if (data == null)
                return NotFound();

            if (!string.IsNullOrEmpty(data.CompanyId))
            {
                var companies = await _apiService
                    .GetAsync<List<DropdownDto>>("dropdown/company")
                    ?? new List<DropdownDto>();

                ViewBag.CompanyName = companies
                    .FirstOrDefault(x => x.Value == data.CompanyId)?.Text;

                if (!string.IsNullOrEmpty(data.BranchId))
                {
                    var branches = await _apiService
                        .GetAsync<List<DropdownDto>>($"dropdown/branch/{data.CompanyId}")
                        ?? new List<DropdownDto>();

                    ViewBag.BranchName = branches
                        .FirstOrDefault(x => x.Value == data.BranchId)?.Text;
                }
            }

            if (!string.IsNullOrEmpty(data.AgentId))
            {
                var agent = await _apiService.GetAsync<BiometricAgentDto>($"BiometricAgent/{data.AgentId}");
                ViewBag.AgentCode = agent?.AgentCode;
            }

            return View(data);
        }

        /// <summary>
        /// Pulls whatever punches the on-site BiometricAgent has pushed for this
        /// device (or, for older pull-based setups, whatever the device's own
        /// ApiUrl exposes) and runs them through AttendanceProcessorService.
        /// This is a manual trigger for HR - the normal path is the agent
        /// pushing to /api/BiometricSync/ingest on its own schedule.
        /// </summary>
        public async Task<IActionResult> SyncNow(string id)
        {
            try
            {
                await _apiService.PostAsync<object>($"BiometricDevice/{id}/sync", new { });

                AlertHelper.Success(TempData, "Device synced and attendance processed.");
            }
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, $"Sync failed: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Health()
        {
            var data = await _apiService
                .GetAsync<List<BiometricDeviceHealthDto>>("BiometricDevice/health")
                ?? new List<BiometricDeviceHealthDto>();

            ViewBag.Summary = await _apiService
                .GetAsync<BiometricDashboardSummaryDto>("BiometricDevice/dashboard-summary")
                ?? new BiometricDashboardSummaryDto();

            ViewBag.SyncLogs = await _apiService
                .GetAsync<List<BiometricSyncLogDto>>("BiometricDevice/sync-logs?take=25")
                ?? new List<BiometricSyncLogDto>();

            return View(data);
        }

        public async Task<IActionResult> SyncAll()
        {
            try
            {
                await _apiService.PostAsync<object>("BiometricSync/sync-all", new { });

                AlertHelper.Success(TempData, "All active devices synced and attendance processed.");
            }
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, $"Sync failed: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync($"BiometricDevice/{id}");

                AlertHelper.Success(TempData, "Biometric device deleted successfully.");
            }
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Non-JS fallback / direct link. This can only ever kick the test
        /// off and inform the user - it cannot wait for the result inline,
        /// because the assigned BiometricAgent is outbound-poll-only (see
        /// BiometricAgent.Worker) and may take up to its configured
        /// PollIntervalSeconds (default 60s) to pick the request up. Prefer
        /// TestConnectionAjax from the UI, which polls for the real outcome.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestConnection(string id)
        {
            try
            {
                var response = await _apiService.PostAsync<object, ApiResponse<DeviceTestConnectionResultDto>>(
                    $"BiometricDevice/{id}/test-connection?tenantId={Uri.EscapeDataString(_tenantId)}&requestedBy={Uri.EscapeDataString(_userId)}",
                    new { });

                if (response.Success)
                    AlertHelper.Success(TempData, "Test connection requested - the biometric agent will attempt the device connection shortly. Check the device's Status badge in a minute, or use the Details page's Test Connection button for a live result.");
                else
                    AlertHelper.Error(TempData, response.Message);
            }
            catch (ApiException ex)
            {
                AlertHelper.Error(TempData, $"Test connection failed: {GetErrorMessage(ex.ResponseContent)}");
            }
            catch (Exception ex)
            {
                // PostAsync<TRequest,TResponse> always throws ApiException on a
                // non-success HTTP response (see ApiService.PostAsync) - this
                // branch only catches genuine transport/deserialization failures.
                AlertHelper.Error(TempData, $"Test connection failed: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// AJAX endpoint used by the Index/Details "Test Connection" buttons.
        /// Kicks off a REAL device-level test (HRMS -> API -> assigned
        /// BiometricAgent -> ESSL SDK -> device) and returns immediately
        /// with the Pending request id; the browser then polls
        /// TestConnectionStatusAjax until the agent reports back. This
        /// can't be synchronous end-to-end - the agent has no reverse
        /// channel, so there is no way to get a same-request answer from it.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> TestConnectionAjax(string id)
        {
            try
            {
                var response = await _apiService.PostAsync<object, ApiResponse<DeviceTestConnectionResultDto>>(
                    $"BiometricDevice/{id}/test-connection?tenantId={Uri.EscapeDataString(_tenantId)}&requestedBy={Uri.EscapeDataString(_userId)}",
                    new { });

                if (response == null)
                    return Json(new { success = false, complete = true, message = "Unable to reach the ERP API." });

                if (!response.Success)
                    // Pre-flight business failure (device inactive, no agent
                    // assigned, agent offline, device not found, ...) - the
                    // API already picked the specific, safe message.
                    return Json(new { success = false, complete = true, message = response.Message });

                return Json(new
                {
                    success = true,
                    complete = false,
                    requestId = response.Data?.RequestId,
                    message = "Test requested - waiting for the biometric agent to respond..."
                });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, complete = true, message = $"Test connection failed: {GetErrorMessage(ex.ResponseContent)}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, complete = true, message = $"Test connection failed: {ex.Message}" });
            }
        }

        /// <summary>Polled by the browser after TestConnectionAjax until the response's `complete` flag is true. Calls GET /api/BiometricDevice/{id}/test-connection/{requestId}.</summary>
        [HttpGet]
        public async Task<JsonResult> TestConnectionStatusAjax(string id, string requestId)
        {
            try
            {
                var response = await _apiService.GetAsync<ApiResponse<DeviceTestConnectionResultDto>>(
                    $"BiometricDevice/{id}/test-connection/{requestId}?tenantId={Uri.EscapeDataString(_tenantId)}");

                var data = response?.Data;

                if (response == null || !response.Success || data == null)
                    return Json(new { success = false, complete = true, message = response?.Message ?? "Unable to check the test connection status." });

                return Json(new
                {
                    success = data.StatusName == "Success",
                    complete = data.IsComplete,
                    stage = data.Stage,
                    message = data.Message,
                    deviceInfo = data.DeviceInfo,
                    deviceCode = data.DeviceCode
                });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, complete = true, message = $"Unable to check the test connection status: {GetErrorMessage(ex.ResponseContent)}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, complete = true, message = $"Unable to check the test connection status: {ex.Message}" });
            }
        }

        // Same shape as ApiService.HandleResponse's error body (ApiResponse<T>
        // serialized as JSON, occasionally prefixed with plain text like
        // "Bad Request (400): {...}") - mirrors the GetErrorMessage helper
        // used across the rest of the APP controllers (e.g. EmployeeBankController).
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

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString() ?? "The ERP API returned an error.";

                return raw;
            }
            catch
            {
                return raw;
            }
        }

        /// <summary>Connection/last-seen/last-sync/agent-liveness snapshot, calling GET /api/BiometricDevice/{id}/status.</summary>
        [HttpGet]
        public async Task<JsonResult> StatusAjax(string id)
        {
            try
            {
                var status = await _apiService.GetAsync<BiometricDeviceStatusDto>($"BiometricDevice/{id}/status");
                return Json(status);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var branches = await _apiService
                .GetAsync<List<DropdownDto>>(
                    $"dropdown/branch/{companyId}"
                );

            var result = branches.Select(x => new
            {
                value = x.Value,
                text = x.Text
            });

            return Json(result);
        }

        #region LoadDropdowns

        private async Task LoadDropdowns(string? companyId = null, string? branchId = null)
        {
            // Company
            var companies = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/company");

            ViewBag.CompanyList = new SelectList(
                companies,
                "Value",
                "Text",
                companyId
            );

            // Branch
            List<DropdownDto> branches = new();

            if (!string.IsNullOrEmpty(companyId))
            {
                branches = await _apiService
                    .GetAsync<List<DropdownDto>>
                    ($"dropdown/branch/{companyId}");
            }

            ViewBag.BranchList = new SelectList(
                branches,
                "Value",
                "Text",
                branchId
            );

            // Biometric Agent
            var agents = await _apiService
                .GetAsync<List<BiometricAgentDto>>("BiometricAgent")
                ?? new List<BiometricAgentDto>();

            ViewBag.AgentList = new SelectList(
                agents.Where(a => a.IsActive),
                "Id",
                "AgentName"
            );
        }

        #endregion
    }
}

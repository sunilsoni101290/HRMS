using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;

namespace APP.Controllers
{
    /// <summary>
    /// Admin CRUD for BiometricAgent (the Windows Service instances that
    /// poll biometric devices). Mirrors BiometricDeviceController's shape -
    /// this feature is deliberately symmetric with device management.
    /// MVC talks only to the API here, never to a device or to SQL directly.
    /// </summary>
    [JwtAuthorize]
    public class BiometricAgentController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IConfiguration _configuration;
        private string _tenantId;
        private string _userId;

        public BiometricAgentController(IApiService apiService, IConfiguration configuration)
        {
            _apiService = apiService;
            _configuration = configuration;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<BiometricAgentDto>>("BiometricAgent")
                ?? new List<BiometricAgentDto>();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadBranchDropdown();
            return View(new BiometricAgentDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create(BiometricAgentDto model)
        {
            try
            {
                model.TenantId = _tenantId;
                model.CreatedBy = _userId;

                var response = await _apiService
                    .PostAsync<BiometricAgentDto, ApiResponse<BiometricAgentDto>>("BiometricAgent", model);

                if (response.Success)
                {
                    TempData["Success"] =
                        $"{response.Message} Agent Key: {response.Data?.AgentKey} " +
                        "(also visible any time on this agent's Details page - you'll need it to configure the BiometricAgent Windows Service).";

                    return RedirectToAction(nameof(Details), new { id = response.Data?.Id });
                }

                ModelState.AddModelError("", response.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            await LoadBranchDropdown(model.BranchId);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<BiometricAgentDto>($"BiometricAgent/{id}");

            if (data == null)
                return NotFound();

            await LoadBranchDropdown(data.BranchId);
            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BiometricAgentDto model)
        {
            if (model==null)
            {
                await LoadBranchDropdown(model.BranchId);
                return View("Create", model);
            }

            try
            {
                model.TenantId = _tenantId;
                model.ModifiedBy = _userId;
                model.CreatedBy = _userId;
                model.ModifiedOn = DateTime.UtcNow;

                var response = await _apiService
                    .PutAsync<BiometricAgentDto, ApiResponse<BiometricAgentDto>>("BiometricAgent", model);

                if (response.Success)
                {
                    TempData["Success"] = response.Message;
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", response.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            await LoadBranchDropdown(model.BranchId);
            return View("Create", model);
        }

        private async Task LoadBranchDropdown(string? branchId = null)
        {
            var branches = await _apiService
                .GetAsync<List<DropdownDto>>($"dropdown/branch-all?tenantId={Uri.EscapeDataString(_tenantId)}")
                ?? new List<DropdownDto>();

            ViewBag.BranchList = new SelectList(branches, "Value", "Text", branchId);
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var data = await _apiService.GetAsync<BiometricAgentDto>($"BiometricAgent/{id}");

            if (data == null)
                return NotFound();

            var devices = await _apiService
                .GetAsync<List<BiometricDeviceDto>>("BiometricDevice")
                ?? new List<BiometricDeviceDto>();

            ViewBag.AssignedDevices = devices.Where(d => d.AgentId == id).ToList();

            return View(data);
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                var ok = await _apiService.DeleteAsync($"BiometricAgent/{id}");

                if (ok)
                    AlertHelper.Success(TempData, "Biometric agent deleted successfully.");
                else
                    AlertHelper.Error(TempData, "Could not delete this agent - it may still have devices assigned to it. Reassign or delete those devices first.");
            }
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Backs the "Download Agent Config" button on Details: rotates the
        /// agent's key via the API (the only way to get AgentKey back, since
        /// Get/Get(id) never return it) and returns a ready-to-use
        /// appsettings.json for the BiometricAgent Windows Service as JSON -
        /// the Details view turns this into a client-side file download via
        /// a Blob, so the key never appears in a URL/query string.
        /// Invalidates any key a currently-running agent instance is using.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> DownloadConfig(string id)
        {
            try
            {
                var response = await _apiService
                    .PostAsync<object, ApiResponse<BiometricAgentDto>>($"BiometricAgent/{id}/regenerate-key", new { });

                if (response?.Data == null)
                    return Json(new { success = false, message = response?.Message ?? "Unable to reach the ERP API." });

                var agent = response.Data;

                // ApiSettings:BaseUrl is this MVC's own API endpoint, e.g.
                // "https://localhost:7222/api/" - the agent's ApiBaseUrl
                // must be the bare host (ApiClient appends "api/..." itself),
                // and Request.Host would wrongly point at the MVC (7070), not
                // the API (7222), so read it from config instead.
                var configuredApiBase = _configuration["ApiSettings:BaseUrl"] ?? "";
                var apiBaseUrl = configuredApiBase.Replace("/api/", "/").Replace("/api", "/");
                if (!apiBaseUrl.EndsWith("/"))
                    apiBaseUrl += "/";

                var configObject = new
                {
                    Logging = new
                    {
                        LogLevel = new { Default = "Information", Microsoft_Hosting_Lifetime = "Information" }
                    },
                    Agent = new
                    {
                        TenantId = agent.TenantId,
                        ApiBaseUrl = apiBaseUrl,
                        AgentCode = agent.AgentCode,
                        AgentKey = agent.AgentKey,
                        PollIntervalSeconds = 60,
                        HeartbeatIntervalSeconds = 60,
                        MaxRetryAttempts = 3,
                        StateFolder = "C:\\ProgramData\\ERP\\BiometricAgent"
                    }
                };

                // The "Logging.Microsoft_Hosting_Lifetime" key above must be
                // "Microsoft.Hosting.Lifetime" in the actual file - C# object
                // property names can't contain dots, so fix it up in the
                // serialized JSON string instead of the anonymous type.
                var json = System.Text.Json.JsonSerializer.Serialize(configObject, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })
                    .Replace("Microsoft_Hosting_Lifetime", "Microsoft.Hosting.Lifetime");

                return Json(new
                {
                    success = true,
                    fileName = $"appsettings.{agent.AgentCode}.json",
                    content = json,
                    agentCode = agent.AgentCode
                });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = ex.ResponseContent });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}

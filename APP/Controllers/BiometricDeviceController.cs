using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    [JwtAuthorize]
    public class BiometricDeviceController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private string _tenantId;
        private string _userId;
        public BiometricDeviceController(IApiService apiService, IHttpContextAccessor httpContextAccessor)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
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

            await LoadDropdowns(data.CompanyId, data.BranchId);

            return View("Create", data);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(BiometricDeviceDto model)
        {
            if (!ModelState.IsValid)
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
                .GetAsync<List<BiometricDeviceHealthDto>>("BiometricDevice/health");

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
        /// Non-JS fallback / direct link. Prefer TestConnectionAjax from the UI -
        /// it keeps the user on the page instead of round-tripping to Index.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> TestConnection(string id)
        {
            try
            {
                var response = await _apiService.PostAsync<object, ApiResponse<bool>>(
                    $"BiometricDevice/{id}/test-connection", new { });

                if (response.Success)
                    AlertHelper.Success(TempData, response.Message);
                else
                    AlertHelper.Error(TempData, response.Message);
            }
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, $"Test connection failed: {ex.Message}");
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// AJAX endpoint used by the Index/Details "Test Connection" buttons so the
        /// result can be shown inline (spinner + badge) without a full page reload.
        /// Calls POST /api/BiometricDevice/{id}/test-connection.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> TestConnectionAjax(string id)
        {
            try
            {
                var response = await _apiService.PostAsync<object, ApiResponse<bool>>(
                    $"BiometricDevice/{id}/test-connection", new { });

                return Json(new
                {
                    success = response.Success,
                    message = response.Message
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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

using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
        private string _tenantId;
        private string _userId;

        public BiometricAgentController(IApiService apiService)
        {
            _apiService = apiService;
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
        public IActionResult Create()
        {
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

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService.GetAsync<BiometricAgentDto>($"BiometricAgent/{id}");

            if (data == null)
                return NotFound();

            return View("Create", data);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(BiometricAgentDto model)
        {
            if (!ModelState.IsValid)
                return View("Create", model);

            try
            {
                model.TenantId = _tenantId;
                model.ModifiedBy = _userId;
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

            return View("Create", model);
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
    }
}

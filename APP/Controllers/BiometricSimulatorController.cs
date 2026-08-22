using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    /// <summary>
    /// Development-only page: lets HR/QA/devs generate biometric punches by
    /// hand (single punch, a full day, or one of the 10 predefined test
    /// scenarios) without a physical eSSL device or a running BiometricAgent
    /// Windows Service. Every action here is a thin wrapper over
    /// API/BiometricSimulatorController, which pushes through the real
    /// ingest pipeline - see BiometricSimulatorService.
    ///
    /// Gated on IWebHostEnvironment.IsDevelopment() so it's never reachable
    /// in Production/Staging regardless of routing/menu wiring; a
    /// "Biometric Simulator" permission is layered on top separately.
    /// </summary>
    [JwtAuthorize]
    public class BiometricSimulatorController : Controller
    {
        private readonly IApiService _apiService;
        private readonly IWebHostEnvironment _env;
        private string _tenantId;

        public BiometricSimulatorController(IApiService apiService, IWebHostEnvironment env)
        {
            _apiService = apiService;
            _env = env;
            _tenantId = SessionHelper.GetActiveTenantId;
        }

        private IActionResult? BlockOutsideDevelopment()
        {
            if (_env.IsDevelopment())
                return null;

            return NotFound("The Biometric Simulator is only available in the Development environment.");
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (BlockOutsideDevelopment() is IActionResult blocked)
                return blocked;

            var employees = await _apiService
                .GetAsync<List<SimulatorMappedEmployeeDto>>($"BiometricSimulator/mapped-employees?tenantId={Uri.EscapeDataString(_tenantId)}")
                ?? new List<SimulatorMappedEmployeeDto>();

            ViewBag.EmployeeList = new SelectList(
                employees.Where(x => x.IsActive),
                "EmployeeId", "EmployeeName");

            var devices = await _apiService
                .GetAsync<List<BiometricDeviceDto>>("BiometricDevice")
                ?? new List<BiometricDeviceDto>();

            ViewBag.DeviceList = new SelectList(
                devices.Where(x => x.IsActive),
                "Id", "DeviceName");

            return View();
        }

        [HttpPost]
        public async Task<JsonResult> SimulatePunch([FromBody] SimulatePunchRequestDto model)
        {
            if (BlockOutsideDevelopment() is IActionResult)
                return Json(new { success = false, message = "Not available." });

            return await PostAndWrap("BiometricSimulator/punch", model);
        }

        [HttpPost]
        public async Task<JsonResult> GenerateFullDay([FromBody] GenerateFullDayRequestDto model)
        {
            if (BlockOutsideDevelopment() is IActionResult)
                return Json(new { success = false, message = "Not available." });

            return await PostAndWrap("BiometricSimulator/full-day", model);
        }

        [HttpPost]
        public async Task<JsonResult> RunScenario([FromBody] RunScenarioRequestDto model)
        {
            if (BlockOutsideDevelopment() is IActionResult)
                return Json(new { success = false, message = "Not available." });

            return await PostAndWrap("BiometricSimulator/scenario", model);
        }

        [HttpPost]
        public async Task<JsonResult> SimulateApiFailure(string deviceId)
        {
            if (BlockOutsideDevelopment() is IActionResult)
                return Json(new { success = false, message = "Not available." });

            return await PostAndWrap($"BiometricSimulator/simulate-api-failure?deviceId={Uri.EscapeDataString(deviceId)}", new { });
        }

        [HttpPost]
        public async Task<JsonResult> SimulateDeviceOffline(string deviceId)
        {
            if (BlockOutsideDevelopment() is IActionResult)
                return Json(new { success = false, message = "Not available." });

            return await PostAndWrap($"BiometricSimulator/simulate-device-offline?deviceId={Uri.EscapeDataString(deviceId)}", new { });
        }

        [HttpPost]
        public async Task<JsonResult> ClearTestData([FromBody] ClearTestDataRequestDto model)
        {
            if (BlockOutsideDevelopment() is IActionResult)
                return Json(new { success = false, message = "Not available." });

            try
            {
                var response = await _apiService.PostAsync<ClearTestDataRequestDto, ApiResponse<object>>(
                    $"BiometricSimulator/clear-test-data?tenantId={Uri.EscapeDataString(_tenantId)}",
                    model);

                return Json(new { success = response?.Success ?? false, message = response?.Message ?? "Unable to reach the ERP API." });
            }
            catch (ApiException ex)
            {
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                // PostAsync<TRequest,TResponse> always throws ApiException on a
                // non-success response (see ApiService.PostAsync) - this branch
                // only catches genuine transport/deserialization failures.
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<JsonResult> PostAndWrap<TReq>(string url, TReq model)
        {
            try
            {
                var separator = url.Contains('?') ? "&" : "?";

                var response = await _apiService.PostAsync<TReq, ApiResponse<SimulatorResultDto>>(
                    $"{url}{separator}tenantId={Uri.EscapeDataString(_tenantId)}",
                    model);

                if (response == null)
                    return Json(new { success = false, message = "Unable to reach the ERP API." });

                return Json(new
                {
                    success = response.Success,
                    message = response.Message,
                    receivedCount = response.Data?.ReceivedCount ?? 0,
                    insertedCount = response.Data?.InsertedCount ?? 0,
                    duplicateCount = response.Data?.DuplicateCount ?? 0
                });
            }
            catch (ApiException ex)
            {
                // ApiService.PostAsync throws this for every non-success HTTP
                // status (400/401/403/404/500/...) with the real API response
                // body in ResponseContent - unwrap it instead of showing the
                // generic "API Error" message so the Simulator surfaces the
                // actual business/validation message from
                // BiometricSimulatorController (API)/BiometricSimulatorService.
                return Json(new { success = false, message = GetErrorMessage(ex.ResponseContent) });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
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
    }
}

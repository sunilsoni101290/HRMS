using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    /// <summary>
    /// Lets an admin link an Employee to the code their biometric device
    /// reports (BiometricAttendanceLog.EmployeeCode / BiometricEmployeeCode)
    /// through a form, instead of writing directly to the database. Required
    /// before any punch - real or test - can turn into an Attendance record.
    /// </summary>
    [JwtAuthorize]
    public class EmployeeBiometricMappingController : Controller
    {
        private readonly IApiService _apiService;
        private string _userId;

        public EmployeeBiometricMappingController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _apiService
                .GetAsync<List<EmployeeBiometricMappingDto>>("EmployeeBiometricMapping");

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new EmployeeBiometricMappingDto());
        }

        [HttpPost]
        public async Task<ActionResult> Create(EmployeeBiometricMappingDto model)
        {
            try
            {
                var response =
                    await _apiService.PostAsync<EmployeeBiometricMappingDto, ApiResponse<EmployeeBiometricMappingDto>>
                    (
                        "EmployeeBiometricMapping",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", response.Message);
            }
            catch (ApiException ex)
            {
                // PostAsync<TRequest,TResponse> always throws ApiException on
                // a non-success response (see ApiService.PostAsync) - unwrap
                // ex.ResponseContent instead of showing the generic
                // "API Error" message, same pattern as EmployeeBankController.
                ModelState.AddModelError("", GetErrorMessage(ex.ResponseContent));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            await LoadDropdowns();

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var data = await _apiService
                .GetAsync<EmployeeBiometricMappingDto>($"EmployeeBiometricMapping/{id}");

            if (data == null)
                return NotFound();

            await LoadDropdowns();

            return View("Create", data);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(EmployeeBiometricMappingDto model)
        {
            try
            {
                var response =
                    await _apiService.PutAsync<EmployeeBiometricMappingDto, ApiResponse<object>>
                    (
                        "EmployeeBiometricMapping",
                        model
                    );

                if (response.Success)
                {
                    TempData["Success"] = response.Message;

                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError("", response.Message);
            }
            catch (Exception ex)
            {
                // PutAsync<TRequest,TResponse> (unlike PostAsync) doesn't
                // wrap a non-success response in ApiException - HandleResponse
                // throws a plain Exception whose Message is
                // "Bad Request (400): {json}", so pull the embedded json
                // back out to get at the real validation/business message.
                ModelState.AddModelError("", GetErrorMessage(ex.Message));
            }

            await LoadDropdowns();

            return View("Create", model);
        }

        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _apiService.DeleteAsync($"EmployeeBiometricMapping/{id}");

                AlertHelper.Success(TempData, "Mapping removed successfully.");
            }
            catch (ApiException ex)
            {
                AlertHelper.Error(TempData, GetErrorMessage(ex.ResponseContent));
            }
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, GetErrorMessage(ex.Message));
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");
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

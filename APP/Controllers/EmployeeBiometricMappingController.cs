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
                ModelState.AddModelError("", ex.Message);
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
            catch (Exception ex)
            {
                AlertHelper.Error(TempData, ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");
        }
    }
}

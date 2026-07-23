using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    #region Employee Bank Controller

    // NOTE: named "EmployeeBank" (not "EmployeeBankDetail") to match the
    // menu-seeding string in Domain/Helper/AppFeatureConstants.cs
    // (EMPLOYEE_BANK_CONTROLLER = "EmployeeBank") and the API route
    // (api/EmployeeBank).
    [JwtAuthorize]
    public class EmployeeBankController : Controller
    {
        private readonly IApiService _apiService;
        private string _tenantId;
        private string _userId;

        public EmployeeBankController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index / Details

        public async Task<IActionResult> Index(string? employeeId, string? search)
        {
            List<EmployeeBankDetailDto>? model;

            if (!string.IsNullOrWhiteSpace(employeeId))
            {
                model = await _apiService
                    .GetAsync<List<EmployeeBankDetailDto>>($"EmployeeBank/employee/{employeeId}");
            }
            else
            {
                var url = string.IsNullOrWhiteSpace(search)
                    ? "EmployeeBank"
                    : $"EmployeeBank?search={Uri.EscapeDataString(search)}";

                model = await _apiService.GetAsync<List<EmployeeBankDetailDto>>(url);
            }

            ViewBag.FilteredEmployeeId = employeeId;
            ViewBag.SearchTerm = search;

            return View(model ?? new List<EmployeeBankDetailDto>());
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var model = await _apiService
                .GetAsync<EmployeeBankDetailDto>($"EmployeeBank/{id}");

            if (model == null) return NotFound();

            return View(model);
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create(string? employeeId)
        {
            await BindDropdowns();

            return View(new EmployeeBankDetailDto
            {
                EmployeeId = employeeId ?? string.Empty,
                IsPrimary = true
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EmployeeBankDetailDto model)
        {
            if (!ModelState.IsValid)
            {
                await BindDropdowns();
                return View(model);
            }

            model.CreatedBy = _userId;
            model.TenantId = _tenantId;

            try
            {
                await _apiService.PostAsync<EmployeeBankDetailDto, EmployeeBankDetailDto>(
                    "EmployeeBank", model);

                TempData["Success"] = "Bank detail added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await BindDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                // PostAsync<TRequest,TResponse> always throws ApiException on
                // a non-success response (see ApiService.PostAsync) - this
                // branch only catches genuine transport/deserialization
                // failures, so ex.Message is shown as-is.
                TempData["GlobalError"] = ex.Message;
                await BindDropdowns();
                return View(model);
            }
        }

        #endregion

        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return RedirectToAction(nameof(Index));

            var model = await _apiService
                .GetAsync<EmployeeBankDetailDto>($"EmployeeBank/{id}");

            if (model == null) return NotFound();

            await BindDropdowns();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EmployeeBankDetailDto model)
        {
            if (!ModelState.IsValid)
            {
                await BindDropdowns();
                return View(model);
            }

            model.TenantId = _tenantId;
            model.ModifiedBy = _userId;
            model.ModifiedOn = DateTime.UtcNow;

            try
            {
                await _apiService.PutAsync<EmployeeBankDetailDto, EmployeeBankDetailDto>(
                    $"EmployeeBank/{id}", model);

                TempData["Success"] = "Bank detail updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                await BindDropdowns();
                return View(model);
            }
            catch (Exception ex)
            {
                // PutAsync (unlike PostAsync<TRequest,TResponse>) doesn't wrap
                // a non-success response in ApiException - HandleResponse
                // throws a plain Exception whose Message is
                // "Bad Request (400): {json}", so pull the embedded json
                // back out to get at the real validation message.
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                await BindDropdowns();
                return View(model);
            }
        }

        #endregion

        #region Delete

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            await _apiService.DeleteAsync($"EmployeeBank/{id}");
            TempData["Success"] = "Bank detail deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region Set Primary

        [HttpPost]
        public async Task<IActionResult> SetPrimary(string id, string? employeeId)
        {
            try
            {
                await _apiService.PutAsync<bool>($"EmployeeBank/{id}/set-primary", new { });
                TempData["Success"] = "Primary bank account updated successfully.";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(Index), new { employeeId });
        }

        #endregion

        #region Helpers

        private async Task BindDropdowns()
        {
            var employees = await _apiService
                .GetAsync<List<DropdownDto>>("dropdown/employee");

            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");

            ViewBag.AccountTypes = Enum.GetValues(typeof(BankAccountType))
                .Cast<BankAccountType>()
                .Select(x => new SelectListItem
                {
                    Text = x.ToString(),
                    Value = ((int)x).ToString()
                })
                .ToList();
        }

        private string GetErrorMessage(string raw)
        {
            try
            {
                // ApiService.HandleResponse sometimes wraps the API's json
                // body in a plain-text prefix, e.g. "Bad Request (400): {...}"
                // - pull the embedded object back out before parsing.
                var jsonStart = raw.IndexOf('{');
                var json = jsonStart >= 0 ? raw.Substring(jsonStart) : raw;

                var obj = Newtonsoft.Json.Linq.JObject.Parse(json);

                if (obj["Message"] != null)
                    return obj["Message"]!.ToString();

                if (obj["Errors"] is Newtonsoft.Json.Linq.JArray errors && errors.Count > 0)
                    return errors[0]?.ToString();

                if (obj["errors"] is Newtonsoft.Json.Linq.JObject validationErrors)
                {
                    foreach (var property in validationErrors.Properties())
                    {
                        if (property.Value is Newtonsoft.Json.Linq.JArray arr && arr.Count > 0)
                            return arr[0]?.ToString();
                    }
                }

                return "Unable to save bank detail.";
            }
            catch
            {
                // Not JSON at all (or a fragment we can't parse) - show the
                // original text rather than hiding it behind a generic
                // message.
                return string.IsNullOrWhiteSpace(raw) ? "Unable to save bank detail." : raw;
            }
        }

        #endregion
    }

    #endregion
}

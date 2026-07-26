using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace APP.Controllers
{
    // NOTE: named exactly "Rejoining" to match the menu-seeding string in
    // Domain/Helper/AppFeatureConstants.cs (REJOINING_CONTROLLER =
    // "Rejoining") and the API route (api/rejoining). Phase 5 (final) of
    // the "Probation & Confirmation" (Employee Lifecycle) module - NO
    // maker-checker workflow, a single-step HR-permission-gated rehire
    // action for a FORMER employee (Employee.RelievingDate != null).
    // HR-only, no self-service equivalent - see
    // APP/Attributes/EssRestrictionAttribute.cs (added to
    // AdminOnlyControllers). Rejoining history is an append-only audit log
    // - no Edit/Delete action exists here by design.
    //
    // Details is folded into Index as an inline expandable row (rather than
    // a separate Details.cshtml) - a rejoin record only has a handful of
    // fields (previous relieving/joining dates, new joining date, reason,
    // processed by/on) that all fit comfortably in the list row itself, so
    // a whole extra round trip/page felt like unnecessary ceremony for this
    // one read-only audit feature. History (below) still gets its own view
    // since it's reached from a different entry point (Employee/Details).
    [JwtAuthorize]
    public class RejoiningController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _tenantId;
        private readonly string _userId;

        public RejoiningController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index (full audit list)

        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var qs = !string.IsNullOrWhiteSpace(search) ? $"?search={Uri.EscapeDataString(search)}" : string.Empty;

            var data = await _apiService.GetAsync<List<RejoiningHistoryDto>>($"rejoining{qs}");

            ViewBag.Search = search;

            return View(data ?? new List<RejoiningHistoryDto>());
        }

        #endregion

        #region Eligible (picker)

        [HttpGet]
        public async Task<IActionResult> Eligible(string? search)
        {
            var qs = !string.IsNullOrWhiteSpace(search) ? $"?search={Uri.EscapeDataString(search)}" : string.Empty;

            var data = await _apiService.GetAsync<List<RejoiningEligibleEmployeeDto>>($"rejoining/eligible{qs}");

            ViewBag.Search = search;

            return View(data ?? new List<RejoiningEligibleEmployeeDto>());
        }

        #endregion

        #region Create

        // employeeId is prefilled from Eligible's "Rejoin" action button (or
        // from Employee/Details' "Rejoin Employee" button for an employee
        // who already has a RelievingDate set).
        [HttpGet]
        public async Task<IActionResult> Create(string employeeId)
        {
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                TempData["GlobalError"] = "No employee was specified to rejoin.";
                return RedirectToAction(nameof(Eligible));
            }

            var employee = await TryGetEmployeeAsync(employeeId);

            if (employee == null)
            {
                TempData["GlobalError"] = "Unable to find this employee.";
                return RedirectToAction(nameof(Eligible));
            }

            ViewBag.Employee = employee;

            var model = new RejoinEmployeeDto
            {
                EmployeeId = employeeId,
                NewJoiningDate = DateTime.Today
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RejoinEmployeeDto model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Employee = await TryGetEmployeeAsync(model.EmployeeId);
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<RejoinEmployeeDto, RejoiningHistoryDto>("rejoining", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to process this rejoin.";
                    return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
                }

                TempData["Success"] = "Employee rejoined successfully.";
                return RedirectToAction("Details", "Employee", new { id = model.EmployeeId });
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
            }
        }

        #endregion

        #region History

        // Read-only list for one employee - linked from Employee/Details'
        // "Rejoin History" reference (an employee who has rejoined before).
        [HttpGet]
        public async Task<IActionResult> History(string employeeId)
        {
            var data = await _apiService.GetAsync<List<RejoiningHistoryDto>>($"rejoining/employee/{employeeId}/history");

            ViewBag.Employee = await TryGetEmployeeAsync(employeeId);
            ViewBag.EmployeeId = employeeId;

            return View(data ?? new List<RejoiningHistoryDto>());
        }

        #endregion

        #region Helpers

        private async Task<EmployeeListDto?> TryGetEmployeeAsync(string employeeId)
        {
            try
            {
                return await _apiService.GetAsync<EmployeeListDto>($"Employee/get-employee-detail/{employeeId}");
            }
            catch
            {
                return null;
            }
        }

        // Same shape as WfhRequestController/EmployeeTransferController's
        // GetErrorMessage.
        private string GetErrorMessage(string raw)
        {
            try
            {
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

                return "Unable to process this rejoin request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this rejoin request." : raw;
            }
        }

        #endregion
    }
}

using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Controllers
{
    // NOTE: named exactly "EmployeeTransfer" to match the menu-seeding
    // string in Domain/Helper/AppFeatureConstants.cs
    // (EMPLOYEE_TRANSFER_CONTROLLER = "EmployeeTransfer") and the API route
    // (api/employeetransfer). Phase 3 of the "Probation & Confirmation"
    // (Employee Lifecycle) module - Maker-Checker workflow (a MAKER
    // proposes new Company/Branch/Department/Designation/ReportingManager
    // values for an Employee, a DIFFERENT person acting as CHECKER must
    // Approve/Reject before the change is applied). This is an HR/Admin-only
    // feature - no self-service equivalent - see
    // APP/Attributes/EssRestrictionAttribute.cs (added to AdminOnlyControllers).
    [JwtAuthorize]
    public class EmployeeTransferController : Controller
    {
        private readonly IApiService _apiService;
        private readonly string _tenantId;
        private readonly string _userId;

        public EmployeeTransferController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index

        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? departmentId, string? search)
        {
            var query = new List<string>();

            if (!string.IsNullOrWhiteSpace(status))
                query.Add($"status={Uri.EscapeDataString(status)}");

            if (!string.IsNullOrWhiteSpace(departmentId))
                query.Add($"departmentId={Uri.EscapeDataString(departmentId)}");

            if (!string.IsNullOrWhiteSpace(search))
                query.Add($"search={Uri.EscapeDataString(search)}");

            var qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;

            var data = await _apiService.GetAsync<List<EmployeeTransferDto>>($"employeetransfer{qs}");

            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department") ?? new();

            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text", departmentId);
            ViewBag.Status = status;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            return View(data ?? new List<EmployeeTransferDto>());
        }

        #endregion

        #region Create

        // employeeId is optional - reached plain (Index's "New Transfer"
        // button) or prefilled from Employee/Details' "Transfer Employee"
        // action button. Once an employee is selected (either prefilled via
        // querystring, or the HR user picks one and the form does a plain
        // GET reload with employeeId in the querystring), the employee's
        // CURRENT Company/Branch/Department/Designation/ReportingManager is
        // loaded as read-only "From" reference text next to each "To"
        // dropdown - see ViewBag.CurrentEmployee below.
        [HttpGet]
        public async Task<IActionResult> Create(string? employeeId)
        {
            await LoadDropdowns();

            EmployeeListDto? currentEmployee = null;

            if (!string.IsNullOrWhiteSpace(employeeId))
            {
                try
                {
                    currentEmployee = await _apiService.GetAsync<EmployeeListDto>($"Employee/get-employee-detail/{employeeId}");
                }
                catch
                {
                    // Unknown/invalid employeeId - fall through with no
                    // "From" reference shown; the Create form still works,
                    // it'll just show the plain employee picker.
                    currentEmployee = null;
                }
            }

            ViewBag.CurrentEmployee = currentEmployee;

            var model = new CreateEmployeeTransferDto
            {
                EmployeeId = employeeId ?? string.Empty,
                EffectiveDate = DateTime.Today
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEmployeeTransferDto model)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdowns();

                ViewBag.CurrentEmployee = !string.IsNullOrWhiteSpace(model.EmployeeId)
                    ? await TryGetEmployeeAsync(model.EmployeeId)
                    : null;

                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateEmployeeTransferDto, EmployeeTransferDto>(
                    "employeetransfer", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit the transfer proposal.";
                    return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
                }

                TempData["Success"] = "Employee Transfer proposal submitted successfully - awaiting checker approval.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
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

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            try
            {
                var data = await _apiService.GetAsync<EmployeeTransferDto>($"employeetransfer/{id}");

                if (data == null)
                    return NotFound();

                ViewBag.CurrentUserId = _userId;

                return View(data);
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                // Mirrors the API's actingUserId != MakerId invariant - a
                // Maker trying to view/act on their own proposal (or anyone
                // else lacking the required permission) gets bounced back
                // with a clear message rather than a raw 403 page. A genuine
                // session-expiry UnauthorizedAccessException (different
                // message) is intentionally NOT caught here - it propagates
                // to the global ApiSessionExpiredFilter, same as
                // ProbationConfirmationController/PipController.
                TempData["GlobalError"] = "You are not authorized to view this Employee Transfer record.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        #endregion

        #region Approve / Reject

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, string? checkerRemarks)
        {
            try
            {
                var result = await _apiService.PutAsync<CheckerActionDto, EmployeeTransferDto>(
                    $"employeetransfer/{id}/approve",
                    new CheckerActionDto { CheckerRemarks = checkerRemarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Employee Transfer approved successfully - the employee's org placement has been updated."
                    : "Unable to approve this Employee Transfer.";
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to approve this Employee Transfer (the Maker cannot also act as Checker).";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(string id, string checkerRemarks)
        {
            try
            {
                var result = await _apiService.PutAsync<CheckerActionDto, EmployeeTransferDto>(
                    $"employeetransfer/{id}/reject",
                    new CheckerActionDto { CheckerRemarks = checkerRemarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Employee Transfer rejected."
                    : "Unable to reject this Employee Transfer.";
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to reject this Employee Transfer (the Maker cannot also act as Checker).";
            }
            catch (ApiException ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        #endregion

        #region History

        // Read-only list for one employee - linked from Employee/Details'
        // "Transfer History" panel.
        [HttpGet]
        public async Task<IActionResult> History(string employeeId)
        {
            var data = await _apiService.GetAsync<List<EmployeeTransferDto>>($"employeetransfer/employee/{employeeId}/history");

            EmployeeListDto? employee = await TryGetEmployeeAsync(employeeId);

            ViewBag.Employee = employee;
            ViewBag.EmployeeId = employeeId;

            return View(data ?? new List<EmployeeTransferDto>());
        }

        #endregion

        #region Cascading Dropdowns (To Branch / To Designation)

        [HttpGet]
        public async Task<JsonResult> GetBranchByCompanyId(string companyId)
        {
            var branches = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/branch/{companyId}") ?? new();

            return Json(branches.Select(x => new { value = x.Value, text = x.Text }));
        }

        [HttpGet]
        public async Task<JsonResult> GetDesignationByDepartmentId(string departmentId)
        {
            var designations = await _apiService.GetAsync<List<DropdownDto>>($"dropdown/designation/{departmentId}") ?? new();

            return Json(designations.Select(x => new { value = x.Value, text = x.Text }));
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

        private async Task LoadDropdowns()
        {
            var employees = await _apiService.GetAsync<List<DropdownDto>>("dropdown/employee") ?? new();
            ViewBag.EmployeeList = new SelectList(employees, "Value", "Text");
            ViewBag.ReportingManagerList = new SelectList(employees, "Value", "Text");

            var companies = await _apiService.GetAsync<List<DropdownDto>>("dropdown/company") ?? new();
            ViewBag.CompanyList = new SelectList(companies, "Value", "Text");

            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department") ?? new();
            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text");

            // Branch/Designation are cascading (scoped to the newly selected
            // Company/Department) - populated client-side via
            // GetBranchByCompanyId/GetDesignationByDepartmentId, so no
            // server-side list is loaded for them up front.
            ViewBag.BranchList = new SelectList(new List<DropdownDto>(), "Value", "Text");
            ViewBag.DesignationList = new SelectList(new List<DropdownDto>(), "Value", "Text");
        }

        // Handles both shapes seen in this codebase: pure JSON (from
        // ApiException.ResponseContent) AND a human-readable prefix in
        // front of the JSON such as "Bad Request (400): {...}" / "Access
        // Denied (403): {...}" (from the plain Exception/
        // UnauthorizedAccessException/ApplicationException that
        // ApiService's HandleResponse throws for the single/2-generic
        // Put/Post overloads) - same approach as WfhRequestController's
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

                return "Unable to process this Employee Transfer request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Employee Transfer request." : raw;
            }
        }

        #endregion
    }
}

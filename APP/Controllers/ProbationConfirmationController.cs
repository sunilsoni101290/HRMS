using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // NOTE: named exactly "ProbationConfirmation" to match the menu-seeding
    // string in Domain/Helper/AppFeatureConstants.cs
    // (PROBATION_CONFIRMATION_CONTROLLER = "ProbationConfirmation") and the
    // API route (api/probationconfirmation). Phase 1 of the "Probation &
    // Confirmation" module - a Maker-Checker (segregation-of-duties)
    // feature: the checker must be a DIFFERENT person than the maker, no
    // override even for HR/Admin (see
    // ProbationConfirmationService.EnsureCheckerIsNotMaker). This is
    // HR-internal (not employee self-service) - anyone holding
    // Create/View/Approve permission on PROBATION_CONFIRMATION (via the
    // DB-driven RolePermission system) may act; the API is the real
    // authority, this controller's own checks are cosmetic UX only.
    [JwtAuthorize]
    public class ProbationConfirmationController : Controller
    {
        private readonly IApiService _apiService;

        // The acting User.Id (NOT an EmployeeId) - MakerId/CheckerId on a
        // ProbationConfirmation record store this same value, so this is
        // what a "is this my own submission" cosmetic check must compare
        // against (see Details.cshtml).
        private readonly string? _tenantId;
        private readonly string? _userId;

        public ProbationConfirmationController(IApiService apiService)
        {
            _apiService = apiService;
            _tenantId = SessionHelper.GetActiveTenantId;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index

        [HttpGet]
        public async Task<IActionResult> Index(string? status, string? departmentId, string? search)
        {
            ViewBag.Status = status;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            await BindDepartmentDropdown(departmentId);
            ViewBag.StatusList = EnumHelper.GetEnumList<ProbationConfirmationStatus>();

            // FIX (defect C1): TenantId/ActingUserId are no longer sent as
            // query params - the API resolves both from the JWT claims on
            // the authenticated request (forwarded automatically by
            // IApiService), never from client-supplied values.
            var url =
                $"probationconfirmation?status={Uri.EscapeDataString(status ?? string.Empty)}" +
                $"&departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<ProbationConfirmationDto>>(url);
                return View(data ?? new List<ProbationConfirmationDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Probation Confirmation records.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        #endregion

        #region Due For Review

        [HttpGet]
        public async Task<IActionResult> DueForReview(string? departmentId, string? search)
        {
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            await BindDepartmentDropdown(departmentId);

            var url =
                $"probationconfirmation/due-for-review?departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<ProbationDueForReviewDto>>(url);
                return View(data ?? new List<ProbationDueForReviewDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view Probation Confirmation records.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        #endregion

        #region Create

        [HttpGet]
        public async Task<IActionResult> Create(string? employeeId)
        {
            await LoadCreateViewDataAsync(employeeId);

            return View(new CreateProbationConfirmationDto
            {
                EmployeeId = employeeId ?? string.Empty
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProbationConfirmationDto model)
        {
            // Extend requires ExtendedProbationEndDate - the API enforces
            // this too (ProbationConfirmationService.CreateAsync), this is
            // just a friendlier round-trip-free check before hitting the
            // API. The Create view also enforces it live via simple
            // show/hide JS on the Recommendation dropdown.
            if (model.Recommendation == (int)ProbationRecommendation.Extend && !model.ExtendedProbationEndDate.HasValue)
            {
                ModelState.AddModelError(nameof(model.ExtendedProbationEndDate),
                    "Extended Probation End Date is required when the recommendation is Extend.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateViewDataAsync(model.EmployeeId);
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreateProbationConfirmationDto, ProbationConfirmationDto>(
                    "probationconfirmation", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to submit Probation Confirmation proposal.";
                    return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
                }

                TempData["Success"] = "Probation Confirmation proposal submitted successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ApiException ex)
            {
                // Includes the Create-permission 403 case (via the
                // ApiException.StatusCode == 403 branch of
                // ApiService.PostAsync<TRequest,TResponse>) - the message
                // embedded in the JSON body is already a clear, specific
                // reason (e.g. "You are not authorized to propose a
                // Probation Confirmation."), so no separate branching is
                // needed here.
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Create), new { employeeId = model.EmployeeId });
            }
        }

        // Populates the Recommendation dropdown plus either the locked
        // read-only employee display (prefilled from DueForReview) or the
        // "eligible employees" picker (direct arrival at Create) - built
        // from the same due-for-review list, since that is this codebase's
        // only concept of "who is currently eligible for a probation
        // decision" (Employees on Probation, not yet exited, without an
        // already-open proposal).
        private async Task LoadCreateViewDataAsync(string? employeeId)
        {
            ViewBag.RecommendationList = EnumHelper.GetEnumList<ProbationRecommendation>();

            var dueList = await _apiService.GetAsync<List<ProbationDueForReviewDto>>("probationconfirmation/due-for-review")
                ?? new List<ProbationDueForReviewDto>();

            if (!string.IsNullOrEmpty(employeeId))
            {
                ViewBag.IsPrefilled = true;
                ViewBag.PrefilledEmployee = dueList.FirstOrDefault(x => x.EmployeeId == employeeId);
            }
            else
            {
                ViewBag.IsPrefilled = false;

                var options = dueList
                    .Select(x => new
                    {
                        Value = x.EmployeeId,
                        Text = $"{x.EmployeeName} ({x.EmployeeCode}) - {x.DepartmentName ?? "-"}"
                    })
                    .ToList();

                ViewBag.EligibleEmployees = new SelectList(options, "Value", "Text");
            }
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            ProbationConfirmationDto? data;

            try
            {
                data = await _apiService.GetAsync<ProbationConfirmationDto>($"probationconfirmation/{id}");
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                // A real 403 (lacking View permission) - NOT a session
                // expiry. Both come back as UnauthorizedAccessException
                // from ApiService.HandleResponse, so this must be caught
                // here and NOT rethrown - otherwise the global
                // ApiSessionExpiredFilter would treat it as a session
                // expiry, clear the session, and bounce to Login, which
                // would be wrong for a plain permission error.
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }

            if (data == null)
                return NotFound();

            ViewBag.CurrentUserId = _userId;

            return View(data);
        }

        #endregion

        #region Workflow

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id, string? remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<ProbationConfirmationDto>(
                    $"probationconfirmation/{id}/approve",
                    new CheckerActionDto { CheckerRemarks = remarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Probation Confirmation approved successfully."
                    : "Unable to approve this Probation Confirmation.";
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                // Surfaces the API's own message verbatim - e.g. "The
                // checker must be a different person than the maker - you
                // cannot approve your own submission." (segregation-of-
                // duties, no override) or a plain missing-Approve-
                // permission message. A genuine session-expiry
                // UnauthorizedAccessException (different message text) is
                // deliberately NOT caught by this filtered clause - it
                // propagates to the global ApiSessionExpiredFilter instead.
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
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
        public async Task<IActionResult> Reject(string id, string remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<ProbationConfirmationDto>(
                    $"probationconfirmation/{id}/reject",
                    new CheckerActionDto { CheckerRemarks = remarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "Probation Confirmation rejected."
                    : "Unable to reject this Probation Confirmation.";
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
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

        #region Helpers

        private async Task BindDepartmentDropdown(string? selectedDepartmentId)
        {
            var departments = await _apiService.GetAsync<List<DropdownDto>>("dropdown/department")
                ?? new List<DropdownDto>();

            ViewBag.DepartmentList = new SelectList(departments, "Value", "Text", selectedDepartmentId);
        }

        // Handles both shapes seen in this codebase: pure JSON (from
        // ApiException.ResponseContent) AND a human-readable prefix in
        // front of the JSON such as "Access Denied (403): {...}" (from the
        // plain UnauthorizedAccessException that ApiService's
        // HandleResponse throws for GetAsync/PutAsync<T> calls) - same
        // approach as WfhRequestController.GetErrorMessage.
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

                return "Unable to process this Probation Confirmation request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this Probation Confirmation request." : raw;
            }
        }

        #endregion
    }
}

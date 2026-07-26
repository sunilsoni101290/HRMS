using APP.Attributes;
using APP.Helpers;
using APP.Models.DTOs;
using APP.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using static APP.Helpers.EnumExtensions;

namespace APP.Controllers
{
    // NOTE: named exactly "Pip" to match the menu-seeding string in
    // Domain/Helper/AppFeatureConstants.cs (PIP_CONTROLLER = "Pip") and the
    // API route (api/pip). Phase 2 of the "Probation & Confirmation"
    // module - Performance Improvement Plan. Creation itself has NO
    // maker-checker gate (system/HR hand-off from an Approved
    // ProbationConfirmation with Recommendation == PlaceOnPIP); the
    // Maker-Checker (segregation-of-duties) gate applies only to the FINAL
    // OUTCOME RESOLUTION (ProposeOutcome then ApproveOutcome/RejectOutcome)
    // - see PipService for the actingUserId != MakerId invariant, no
    // override even for HR/Admin. HR-internal (not employee self-service);
    // the API is the real authority, this controller's own checks are
    // cosmetic UX only.
    [JwtAuthorize]
    public class PipController : Controller
    {
        private readonly IApiService _apiService;

        // The acting User.Id (NOT an EmployeeId) - MakerId/CheckerId on a
        // PipRecord store this same value, so this is what a "is this my
        // own submission" cosmetic check must compare against (see
        // Details.cshtml).
        private readonly string? _userId;

        public PipController(IApiService apiService)
        {
            _apiService = apiService;
            _userId = SessionHelper.GetActiveUserId;
        }

        #region Index

        [HttpGet]
        public async Task<IActionResult> Index(string? finalOutcome, string? departmentId, string? search)
        {
            ViewBag.FinalOutcome = finalOutcome;
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            await BindDepartmentDropdown(departmentId);
            ViewBag.FinalOutcomeList = EnumHelper.GetEnumList<PipFinalOutcome>();

            var url =
                $"pip?finalOutcome={Uri.EscapeDataString(finalOutcome ?? string.Empty)}" +
                $"&departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<PipRecordDto>>(url);
                return View(data ?? new List<PipRecordDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view PIP records.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        #endregion

        #region Active

        // HR tracking dashboard - active/in-progress PIPs only
        // (FinalOutcome == InProgress), backed by GET api/pip/active.
        [HttpGet]
        public async Task<IActionResult> Active(string? departmentId, string? search)
        {
            ViewBag.DepartmentId = departmentId;
            ViewBag.Search = search;

            await BindDepartmentDropdown(departmentId);

            var url =
                $"pip/active?departmentId={Uri.EscapeDataString(departmentId ?? string.Empty)}" +
                $"&search={Uri.EscapeDataString(search ?? string.Empty)}";

            try
            {
                var data = await _apiService.GetAsync<List<PipRecordDto>>(url);
                return View(data ?? new List<PipRecordDto>());
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = "You are not authorized to view PIP records.";
                return RedirectToAction("Index", "Dashboard");
            }
        }

        #endregion

        #region Create

        // Only reachable/intended from an Approved ProbationConfirmation
        // with Recommendation == PlaceOnPIP - see the "Create PIP Record"
        // button on ProbationConfirmation/Details.cshtml. Fetches that
        // ProbationConfirmation's details (via the ProbationConfirmation
        // API, not the Domain/Application layer directly) to prefill/
        // read-only-display EmployeeId/Employee info.
        [HttpGet]
        public async Task<IActionResult> Create(string probationConfirmationId)
        {
            if (string.IsNullOrEmpty(probationConfirmationId))
            {
                TempData["GlobalError"] = "A Probation Confirmation record is required to create a PIP.";
                return RedirectToAction("Index", "ProbationConfirmation");
            }

            ProbationConfirmationDto? confirmation;

            try
            {
                confirmation = await _apiService.GetAsync<ProbationConfirmationDto>(
                    $"probationconfirmation/{probationConfirmationId}");
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction("Index", "ProbationConfirmation");
            }
            catch (KeyNotFoundException)
            {
                confirmation = null;
            }

            if (confirmation == null)
            {
                TempData["GlobalError"] = "Probation Confirmation record not found.";
                return RedirectToAction("Index", "ProbationConfirmation");
            }

            ViewBag.Confirmation = confirmation;

            // Soft, cosmetic eligibility warning only - PipService.CreateAsync
            // is the real authority (Status must be Approved AND
            // Recommendation must be PlaceOnPIP) and will reject the POST
            // with a clear message if these don't hold. Deliberately NOT
            // pre-checking "does a PIP already exist for this Probation
            // Confirmation" here (that would need an extra lookup endpoint
            // that doesn't exist) - PipService.CreateAsync's own duplicate
            // check surfaces a clean error via TempData if one already
            // exists, same as any other Create error.
            ViewBag.IsEligible =
                confirmation.Status == (int)ProbationConfirmationStatus.Approved &&
                confirmation.Recommendation == (int)ProbationRecommendation.PlaceOnPIP;

            return View(new CreatePipRecordDto
            {
                EmployeeId = confirmation.EmployeeId,
                ProbationConfirmationId = confirmation.Id,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddMonths(3)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePipRecordDto model)
        {
            if (model.EndDate <= model.StartDate)
                ModelState.AddModelError(nameof(model.EndDate), "End Date must be after Start Date.");

            if (!ModelState.IsValid)
            {
                await ReloadCreateViewDataAsync(model.ProbationConfirmationId);
                return View(model);
            }

            try
            {
                var result = await _apiService.PostAsync<CreatePipRecordDto, PipRecordDto>("pip", model);

                if (result == null)
                {
                    TempData["GlobalError"] = "Unable to create PIP record.";
                    return RedirectToAction(nameof(Create), new { probationConfirmationId = model.ProbationConfirmationId });
                }

                TempData["Success"] = "PIP record created successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Id });
            }
            catch (ApiException ex)
            {
                // Includes the "PIP already exists for this Probation
                // Confirmation" / "must be Approved + PlaceOnPIP" /
                // Create-permission 403 cases - PipService.CreateAsync's
                // message is already specific enough to show directly.
                TempData["GlobalError"] = GetErrorMessage(ex.ResponseContent);
                return RedirectToAction(nameof(Create), new { probationConfirmationId = model.ProbationConfirmationId });
            }
            catch (Exception ex)
            {
                TempData["GlobalError"] = GetErrorMessage(ex.Message);
                return RedirectToAction(nameof(Create), new { probationConfirmationId = model.ProbationConfirmationId });
            }
        }

        private async Task ReloadCreateViewDataAsync(string probationConfirmationId)
        {
            try
            {
                var confirmation = await _apiService.GetAsync<ProbationConfirmationDto>(
                    $"probationconfirmation/{probationConfirmationId}");

                ViewBag.Confirmation = confirmation;
                ViewBag.IsEligible = confirmation != null &&
                    confirmation.Status == (int)ProbationConfirmationStatus.Approved &&
                    confirmation.Recommendation == (int)ProbationRecommendation.PlaceOnPIP;
            }
            catch
            {
                ViewBag.Confirmation = null;
                ViewBag.IsEligible = false;
            }
        }

        #endregion

        #region Details

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            PipRecordDto? data;

            try
            {
                data = await _apiService.GetAsync<PipRecordDto>($"pip/{id}");
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                // Real 403 (lacking View permission), not a session expiry
                // - see ProbationConfirmationController.Details for why
                // this must be caught here rather than left to bubble to
                // the global ApiSessionExpiredFilter.
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

        // Maker action - proposes the final outcome (Successful/
        // Unsuccessful). Only meaningful while FinalOutcome is still
        // InProgress AND there is no live proposal already pending checker
        // action (MakerId == null on a fresh record, or the previous
        // proposal was Rejected) - see Details.cshtml's canPropose.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposeOutcome(string id, ProposePipOutcomeDto model)
        {
            if (!ModelState.IsValid)
            {
                TempData["GlobalError"] = "A final outcome is required.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var result = await _apiService.PutAsync<ProposePipOutcomeDto, PipRecordDto>(
                    $"pip/{id}/propose-outcome", model);

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "PIP outcome proposed successfully - awaiting checker action."
                    : "Unable to propose this PIP outcome.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOutcome(string id, string? remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<PipRecordDto>(
                    $"pip/{id}/approve-outcome",
                    new CheckerActionDto { CheckerRemarks = remarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "PIP outcome approved successfully."
                    : "Unable to approve this PIP outcome.";
            }
            catch (UnauthorizedAccessException ex) when (ex.Message.StartsWith("Access Denied"))
            {
                // Surfaces the API's own message verbatim - e.g. "The
                // checker must be a different person than the maker - you
                // cannot approve your own submission." A genuine
                // session-expiry UnauthorizedAccessException (different
                // message text) is deliberately NOT caught here - it
                // propagates to the global ApiSessionExpiredFilter.
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
        public async Task<IActionResult> RejectOutcome(string id, string remarks)
        {
            try
            {
                var result = await _apiService.PutAsync<PipRecordDto>(
                    $"pip/{id}/reject-outcome",
                    new CheckerActionDto { CheckerRemarks = remarks });

                TempData[result != null ? "Success" : "GlobalError"] = result != null
                    ? "PIP outcome rejected."
                    : "Unable to reject this PIP outcome.";
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
        // HandleResponse throws for GetAsync/PutAsync calls) - same
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

                return "Unable to process this PIP request.";
            }
            catch
            {
                return string.IsNullOrWhiteSpace(raw) ? "Unable to process this PIP request." : raw;
            }
        }

        #endregion
    }
}
